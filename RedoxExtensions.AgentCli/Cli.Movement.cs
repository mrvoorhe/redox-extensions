using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;

namespace RedoxExtensions.AgentCli;

/// <summary>
/// rectl go: moves the character with the movement keys the keymap binds, the way a player would.  Keys are held as
/// short leases the plugin lets go of by itself (see KeyInput.Press), renewed while rectl wants them held, so a key
/// never stays down for long after rectl stops.
/// </summary>
public partial class Cli
{
    /// <summary>rectl go moves and the keymap actions they press.</summary>
    public static readonly IReadOnlyDictionary<string, string> MoveActions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["forward"] = "MovementForward",
        ["back"] = "MovementBackup",
        ["turnleft"] = "MovementTurnLeft",
        ["turnright"] = "MovementTurnRight",
        ["strafeleft"] = "MovementStrafeLeft",
        ["straferight"] = "MovementStrafeRight",
        ["jump"] = "MovementJump",
        ["stop"] = "MovementStop",
        ["walk"] = "MovementWalkMode",
        ["runlock"] = "MovementRunLock",
    };

    /// <summary>Moves that toggle something with a tap; the rest default to a longer hold.</summary>
    private static readonly HashSet<string> TapMoves = new(StringComparer.OrdinalIgnoreCase) { "stop", "runlock" };

    private const int DefaultMoveHoldMs = 1000;

    /// <summary>How long each key down lasts unless renewed: what the key keeps doing if rectl dies.</summary>
    public const int MoveLeaseMs = 1000;

    /// <summary>How often a held key's lease is renewed, well inside the lease.</summary>
    private const int MoveRenewMs = 400;

    private const int MovePollMs = 100;

    /// <summary>Ends a turn this close to the wanted heading.</summary>
    public const double TurnToleranceDegrees = 5;

    /// <summary>Gives up on --meters when the character moves less than this in <see cref="StuckMs"/>.</summary>
    public const double StuckMeters = 0.5;

    public const int StuckMs = 2000;

    private const int DefaultMoveTimeoutMs = 30000;

    /// <summary>Time to let the character come to rest before reading where it ended up.</summary>
    private const int MoveSettleMs = 400;

    /// <summary>Meters per landblock: landblock coordinates step 192 m.</summary>
    private const double LandblockMeters = 192;

    private volatile bool _moveCancelled;

    private int Go(BridgeInstance instance, Arguments parsed)
    {
        if (parsed.Positionals.Count == 0)
        {
            this._err.WriteLine($"go needs a move: {string.Join(", ", MoveActions.Keys)}, turn <degrees> or face <heading>");
            return ExitUsage;
        }

        var path = parsed.Get("keymap") ?? DefaultKeymapPath();
        if (!File.Exists(path))
        {
            this._err.WriteLine($"No keymap at {path}.  Use --keymap <path>");
            return ExitFailed;
        }
        var lines = File.ReadAllLines(path);

        string KeyFor(string action)
        {
            var key = Keymap.FindKey(lines, action, "MovementCommands");
            if (key == null)
                this._err.WriteLine($"{path} binds no plain keyboard key to {action}");
            return key;
        }

        var timeoutMs = parsed.GetInt("timeout") ?? DefaultMoveTimeoutMs;
        var first = parsed.Positionals[0].ToLowerInvariant();

        if (first is "turn" or "face")
        {
            if (parsed.Positionals.Count != 2 || !double.TryParse(parsed.Positionals[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var degrees)
                || (first == "turn" && Math.Abs(degrees) > 360) || (first == "face" && (degrees < 0 || degrees >= 360)))
            {
                this._err.WriteLine(first == "turn"
                    ? "go turn takes degrees from -360 to 360, positive to the right, e.g. rectl go turn -45"
                    : "go face takes a heading from 0 to 359 (0 north, 90 east), e.g. rectl go face 90");
                return ExitUsage;
            }

            var left = KeyFor(MoveActions["turnleft"]);
            var right = KeyFor(MoveActions["turnright"]);
            if (left == null || right == null)
                return ExitFailed;
            return this.WithMoveCancel(() => this.Turn(instance, first == "face" ? null : degrees, first == "face" ? degrees : null, left, right, timeoutMs));
        }

        if (parsed.Get("meters") is { } metersText)
        {
            if (parsed.Positionals.Count != 1 || first is not ("forward" or "back" or "strafeleft" or "straferight"))
            {
                this._err.WriteLine("--meters goes with one of forward, back, strafeleft or straferight, e.g. rectl go forward --meters 5");
                return ExitUsage;
            }
            if (!double.TryParse(metersText, NumberStyles.Float, CultureInfo.InvariantCulture, out var meters) || meters <= 0 || meters > 500)
            {
                this._err.WriteLine($"--meters takes a distance from 0 to 500, not '{metersText}'");
                return ExitUsage;
            }

            var action = MoveActions[first];
            var key = KeyFor(action);
            if (key == null)
                return ExitFailed;
            return this.WithMoveCancel(() => this.MoveDistance(instance, first, action, key, meters, timeoutMs));
        }

        var presses = new List<(string Move, string Action, string Key, int HoldMs)>();
        foreach (var positional in parsed.Positionals)
        {
            var parts = positional.Split(':');
            if (parts.Length > 2 || !MoveActions.TryGetValue(parts[0], out var action))
            {
                this._err.WriteLine($"Unknown move '{positional}'.  Moves: {string.Join(", ", MoveActions.Keys)}, each optionally :ms, e.g. forward:2000 turnright:400");
                return ExitUsage;
            }

            int holdMs;
            if (parts.Length == 2)
            {
                if (!int.TryParse(parts[1], out holdMs) || holdMs < 0 || holdMs > timeoutMs)
                {
                    this._err.WriteLine($"Hold time in '{positional}' must be 0 to {timeoutMs} ms (--timeout raises the limit)");
                    return ExitUsage;
                }
            }
            else
            {
                holdMs = parsed.GetInt("hold") ?? (TapMoves.Contains(parts[0]) ? DefaultKeyHoldMs : DefaultMoveHoldMs);
            }

            var key = KeyFor(action);
            if (key == null)
                return ExitFailed;
            if (presses.Any(p => p.Key == key))
            {
                this._err.WriteLine($"{parts[0]} is given twice");
                return ExitUsage;
            }
            presses.Add((parts[0].ToLowerInvariant(), action, key, holdMs));
        }

        return this.WithMoveCancel(() => this.HoldMoves(instance, presses));
    }

    /// <summary>Runs a move with Ctrl+C turned into a clean stop (keys let go) rather than killing rectl mid-hold.</summary>
    private int WithMoveCancel(Func<int> move)
    {
        this._moveCancelled = false;
        ConsoleCancelEventHandler handler = (_, e) =>
        {
            e.Cancel = true;
            this._moveCancelled = true;
        };
        Console.CancelKeyPress += handler;
        try
        {
            return move();
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    /// <summary>
    /// Holds every key for its own time, all starting together, renewing the leases of keys held past
    /// <see cref="MoveLeaseMs"/>.  The last lease of each key ends at its hold time, so the plugin lets go on time.
    /// </summary>
    private int HoldMoves(BridgeInstance instance, List<(string Move, string Action, string Key, int HoldMs)> presses)
    {
        using var client = Connect(instance, DefaultWaitTimeoutMs);
        var held = new List<string>();
        try
        {
            var clock = Stopwatch.StartNew();
            var pending = new List<(string Key, int HoldMs)>();
            foreach (var press in presses)
            {
                var response = this.SendKey(client, press.Key, Math.Min(press.HoldMs, MoveLeaseMs));
                if (response != null)
                    return this.Report(response);
                held.Add(press.Key);
                if (press.HoldMs > MoveLeaseMs)
                    pending.Add((press.Key, press.HoldMs));
            }

            while (pending.Count > 0)
            {
                if (this._moveCancelled)
                    return this.Cancelled();
                Thread.Sleep(MoveRenewMs);
                var now = (int)clock.ElapsedMilliseconds;
                for (var i = pending.Count - 1; i >= 0; i--)
                {
                    var remaining = pending[i].HoldMs - now;
                    if (remaining > 0)
                    {
                        var response = this.SendKey(client, pending[i].Key, Math.Min(remaining, MoveLeaseMs));
                        if (response != null)
                            return this.Report(response);
                    }
                    if (remaining <= MoveLeaseMs)
                        pending.RemoveAt(i);
                }
            }

            var longest = presses.Max(p => p.HoldMs);
            var left = longest - (int)clock.ElapsedMilliseconds;
            if (left > 0)
                Thread.Sleep(left);
            Thread.Sleep(KeyReleaseSettleMs);
            held.Clear();

            this.WriteJson(new JsonObject
            {
                ["presses"] = new JsonArray(presses.Select(p => (JsonNode)new JsonObject
                {
                    ["move"] = p.Move,
                    ["action"] = p.Action,
                    ["key"] = p.Key,
                    ["holdMs"] = p.HoldMs,
                }).ToArray()),
            });
            return ExitOk;
        }
        finally
        {
            ReleaseKeys(client, held);
        }
    }

    /// <summary>
    /// Turns by <paramref name="byDegrees"/> (positive right) or to <paramref name="toHeading"/>, holding a turn key
    /// and watching the heading.  Lets go a little early, by how far the character turns in a poll, then nudges
    /// back and forth with short taps until it's within <see cref="TurnToleranceDegrees"/>.
    /// </summary>
    private int Turn(BridgeInstance instance, double? byDegrees, double? toHeading, string leftKey, string rightKey, int timeoutMs)
    {
        using var client = Connect(instance, DefaultWaitTimeoutMs);
        var held = new List<string>();
        try
        {
            if (!this.TryReadPose(client, out var start, out var failed))
                return failed;

            var wanted = byDegrees ?? SignedAngle(toHeading.Value - start.Heading);
            var turned = 0.0;
            var last = start.Heading;
            var clock = Stopwatch.StartNew();
            var rate = 0.0;  // degrees a millisecond, measured while turning

            void Track(double heading)
            {
                turned += SignedAngle(heading - last);
                last = heading;
            }

            while (Math.Abs(wanted - turned) > TurnToleranceDegrees)
            {
                if (this._moveCancelled)
                    return this.Cancelled();
                if (clock.ElapsedMilliseconds > timeoutMs)
                    return this.MoveFailed("timeout", start, last, turned, wanted);

                var key = wanted - turned > 0 ? rightKey : leftKey;
                var sign = Math.Sign(wanted - turned);
                var response = this.SendKey(client, key, MoveLeaseMs);
                if (response != null)
                    return this.Report(response);
                held.Add(key);

                var heldFor = Stopwatch.StartNew();
                var lastRenew = 0L;
                var lastTurned = turned;
                while (true)
                {
                    if (this._moveCancelled)
                        return this.Cancelled();
                    Thread.Sleep(MovePollMs);
                    if (!this.TryReadPose(client, out var pose, out failed))
                        return failed;
                    Track(pose.Heading);

                    var step = Math.Abs(turned - lastTurned);
                    lastTurned = turned;
                    if (heldFor.ElapsedMilliseconds > MovePollMs * 2)
                        rate = Math.Max(rate, step / MovePollMs);

                    var left = (wanted - turned) * sign;
                    if (left <= rate * MovePollMs + TurnToleranceDegrees / 2 || clock.ElapsedMilliseconds > timeoutMs)
                        break;
                    if (heldFor.ElapsedMilliseconds - lastRenew >= MoveRenewMs)
                    {
                        lastRenew = heldFor.ElapsedMilliseconds;
                        response = this.SendKey(client, key, MoveLeaseMs);
                        if (response != null)
                            return this.Report(response);
                    }
                }

                ReleaseKeys(client, held);
                held.Clear();
                Thread.Sleep(MoveSettleMs);
                if (!this.TryReadPose(client, out var rest, out failed))
                    return failed;
                Track(rest.Heading);
            }

            this.WriteJson(new JsonObject
            {
                ["startHeading"] = Math.Round(start.Heading, 1),
                ["endHeading"] = Math.Round(last, 1),
                ["turned"] = Math.Round(turned, 1),
                ["wanted"] = Math.Round(wanted, 1),
            });
            return ExitOk;
        }
        finally
        {
            ReleaseKeys(client, held);
        }
    }

    /// <summary>
    /// Holds one move key until the character is <paramref name="meters"/> from where it started, letting go early by
    /// how far it goes in a poll.  Gives up (exit 1, stuck) if it moves less than <see cref="StuckMeters"/> in
    /// <see cref="StuckMs"/>.
    /// </summary>
    private int MoveDistance(BridgeInstance instance, string move, string action, string key, double meters, int timeoutMs)
    {
        using var client = Connect(instance, DefaultWaitTimeoutMs);
        var held = new List<string>();
        try
        {
            if (!this.TryReadPose(client, out var start, out var failed))
                return failed;

            var response = this.SendKey(client, key, MoveLeaseMs);
            if (response != null)
                return this.Report(response);
            held.Add(key);

            var clock = Stopwatch.StartNew();
            var lastRenew = 0L;
            var lastDistance = 0.0;
            var speed = 0.0;  // meters a millisecond
            var anchorDistance = 0.0;
            var anchorAt = 0L;
            string outcome = null;
            while (true)
            {
                if (this._moveCancelled)
                    return this.Cancelled();
                Thread.Sleep(MovePollMs);
                if (!this.TryReadPose(client, out var pose, out failed))
                    return failed;

                var distance = start.DistanceTo(pose);
                if (clock.ElapsedMilliseconds > MovePollMs * 3)
                    speed = Math.Max(speed, (distance - lastDistance) / MovePollMs);
                lastDistance = distance;

                if (distance >= meters - speed * MovePollMs)
                    break;
                if (distance - anchorDistance >= StuckMeters)
                {
                    anchorDistance = distance;
                    anchorAt = clock.ElapsedMilliseconds;
                }
                else if (clock.ElapsedMilliseconds - anchorAt > StuckMs)
                {
                    outcome = "stuck";
                    break;
                }
                if (clock.ElapsedMilliseconds > timeoutMs)
                {
                    outcome = "timeout";
                    break;
                }

                if (clock.ElapsedMilliseconds - lastRenew >= MoveRenewMs)
                {
                    lastRenew = clock.ElapsedMilliseconds;
                    response = this.SendKey(client, key, MoveLeaseMs);
                    if (response != null)
                        return this.Report(response);
                }
            }

            ReleaseKeys(client, held);
            held.Clear();
            Thread.Sleep(MoveSettleMs);
            if (!this.TryReadPose(client, out var end, out failed))
                return failed;

            var result = new JsonObject
            {
                ["move"] = move,
                ["action"] = action,
                ["key"] = key,
                ["meters"] = meters,
                ["moved"] = Math.Round(start.DistanceTo(end), 2),
                ["start"] = start.ToJson(),
                ["end"] = end.ToJson(),
            };
            if (outcome != null)
                result[outcome] = true;
            this.WriteJson(result);
            if (outcome != null)
                this._err.WriteLine(outcome == "stuck"
                    ? $"Stuck: moved under {StuckMeters} m in {StuckMs / 1000.0:0.#} s"
                    : $"Timed out after {timeoutMs} ms");
            return outcome == null ? ExitOk : ExitFailed;
        }
        finally
        {
            ReleaseKeys(client, held);
        }
    }

    /// <summary>Presses a key for a lease.  Returns the response if the plugin refused, else null.</summary>
    private JsonObject SendKey(BridgeClient client, string key, int holdMs)
    {
        var response = client.Send("key", new Dictionary<string, JsonNode> { ["key"] = key, ["holdMs"] = holdMs });
        return IsOk(response) ? null : response;
    }

    /// <summary>Lets go of keys now, best effort: the plugin's lease lets go of them anyway.</summary>
    private static void ReleaseKeys(BridgeClient client, IEnumerable<string> keys)
    {
        foreach (var key in keys.ToList())
        {
            try
            {
                client.Send("key", new Dictionary<string, JsonNode> { ["key"] = key, ["release"] = true });
            }
            catch (Exception e) when (e is IOException or System.Net.Sockets.SocketException)
            {
            }
        }
    }

    private int Cancelled()
    {
        this._err.WriteLine("Cancelled; keys let go");
        return ExitFailed;
    }

    private int MoveFailed(string outcome, Pose start, double heading, double turned, double wanted)
    {
        this.WriteJson(new JsonObject
        {
            ["startHeading"] = Math.Round(start.Heading, 1),
            ["endHeading"] = Math.Round(heading, 1),
            ["turned"] = Math.Round(turned, 1),
            ["wanted"] = Math.Round(wanted, 1),
            [outcome] = true,
        });
        this._err.WriteLine($"Turn {outcome}");
        return ExitFailed;
    }

    private bool TryReadPose(BridgeClient client, out Pose pose, out int exit)
    {
        pose = null;
        exit = ExitOk;
        var response = client.Send("state");
        if (!IsOk(response))
        {
            exit = this.Report(response);
            return false;
        }

        var location = response["result"]?["location"];
        if (location == null)
        {
            this._err.WriteLine("state has no location (not logged in?)");
            exit = ExitFailed;
            return false;
        }

        pose = new Pose(
            (uint)location["landcell"].GetValue<long>(),
            location["x"].GetValue<double>(),
            location["y"].GetValue<double>(),
            location["z"].GetValue<double>(),
            location["heading"].GetValue<double>());
        return true;
    }

    /// <summary>An angle as -180..180.</summary>
    public static double SignedAngle(double degrees)
    {
        var a = degrees % 360;
        if (a > 180)
            a -= 360;
        else if (a <= -180)
            a += 360;
        return a;
    }

    /// <summary>Where the character is: landcell, its local x/y/z (meters), and heading (degrees, 0 north, clockwise).</summary>
    public sealed class Pose
    {
        public Pose(uint landcell, double x, double y, double z, double heading)
        {
            this.Landcell = landcell;
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Heading = heading;
        }

        public uint Landcell { get; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double Heading { get; }

        /// <summary>East-west and north-south meters across landblocks: each landblock is 192 m.</summary>
        public double WorldX => ((this.Landcell >> 24) & 0xFF) * LandblockMeters + this.X;

        public double WorldY => ((this.Landcell >> 16) & 0xFF) * LandblockMeters + this.Y;

        /// <summary>Flat distance, ignoring height.</summary>
        public double DistanceTo(Pose other) => Math.Sqrt(Math.Pow(other.WorldX - this.WorldX, 2) + Math.Pow(other.WorldY - this.WorldY, 2));

        public JsonObject ToJson() => new()
        {
            ["landcell"] = $"{this.Landcell:X8}",
            ["x"] = Math.Round(this.X, 2),
            ["y"] = Math.Round(this.Y, 2),
            ["z"] = Math.Round(this.Z, 2),
            ["heading"] = Math.Round(this.Heading, 1),
        };
    }
}
