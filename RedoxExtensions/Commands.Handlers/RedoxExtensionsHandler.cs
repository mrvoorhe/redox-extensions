using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using RedoxExtensions.Core.Extensions;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Commands.Handlers;

internal static class RedoxExtensionsHandler
{
    internal static bool HandleCommand(ICommand command)
    {
        REPlugin.Instance.Debug.WriteObject(command);

        switch (command.Name.ToLower())
        {
            case "help":
                DisplayHelp();
                return true;

            case "exit":
                Actions.SimpleActions.ExitGame();
                return true;

            case "fellowup":
                Actions.MyActions.FellowUsualSuspects(false);
                return true;
            case "fellowupb":
                Actions.MyActions.FellowUsualSuspects(true);
                return true;
            case "pullkeys":
                Actions.CharacterActions.PullLegendaryKeys();
                return true;

            case "use":
                // Can be used for anything.
                Actions.Dispatched.UseObject.Create(command).Enqueue();
                return true;

            case "give":
                Actions.Dispatched.GiveItems.Create(command).Enqueue();
                return true;

            case "list":
                Actions.Dispatched.ListItems.Create(command).Enqueue();
                return true;

            case "cram":
                Actions.Dispatched.CramItems.Create(command).Enqueue();
                return true;

            case "copycat":
            case "cc":
                switch (command.Arguments[0].ToLower().Trim())
                {
                    case "on":
                        REPlugin.Instance.MonitorManager.CopyCatMaster.Enable();
                        break;
                    case "off":
                        REPlugin.Instance.MonitorManager.CopyCatMaster.Disable();
                        break;
                    default:
                        REPlugin.Instance.Chat.WriteLine("Unknown copycay option : {0} ", command.Arguments[0]);
                        break;
                }
                return true;
            
            // Enable diagnostic log lines
            case "diag":
                switch (command.Arguments[0].ToLower().Trim())
                {
                    case "on":
                        REPlugin.Instance.Debug.EnableDiagnosticsLogging = true;
                        REPlugin.Instance.Chat.WriteLine("[RE] Diagnostic logging on");
                        break;
                    case "off":
                        REPlugin.Instance.Debug.EnableDiagnosticsLogging = false;
                        REPlugin.Instance.Chat.WriteLine("[RE] Diagnostic logging off");
                        break;
                    default:
                        REPlugin.Instance.Chat.WriteLine("Unknown diag option : {0} ", command.Arguments[0]);
                        break;
                }
                return true;

            case "showcolors":
                {
                    var arg = command.Arguments.Count > 0 ? command.Arguments[0].ToLower().Trim() : null;
                    bool newState;
                    switch (arg)
                    {
                        case "on":
                            newState = true;
                            break;
                        case "off":
                            newState = false;
                            break;
                        case null:
                            newState = !REPlugin.Instance.ShowColors;
                            break;
                        default:
                            REPlugin.Instance.Chat.WriteLine("Unknown showcolors option : {0}", command.Arguments[0]);
                            return true;
                    }

                    REPlugin.Instance.ShowColors = newState;
                    REPlugin.Instance.Chat.WriteLine("[RE] Show colors on ID: {0}", newState ? "on" : "off");
                    return true;
                }

            case "unstuck":
            case "us":
                Actions.UnstuckActions.Unstuck();
                return true;

            case "unstuckdetect":
            case "usdetect":
                ProcessUnstuckDetect(command);
                return true;

            case "recordroute":
            case "rr":
                ProcessRecordRoute(command);
                return true;

            case "startmeta":
            case "sm":
                ProcessStartMeta(command);
                return true;

            case "schedule":
            case "sched":
                ProcessSchedule(command);
                return true;

            case "metachainwatch":
            case "mcw":
                ProcessMetaChainWatch(command);
                return true;
            case "switch":
                Actions.CharacterActions.SwitchCharacter(command.Arguments);
                return true;

            case "track":
            {
                var monitors = REPlugin.Instance.MonitorManager;
                monitors.Rewards.StartTracking();
                monitors.Banking.StartTracking(s => REPlugin.Instance.Chat.WriteLine(s));
                return true;
            }

            case "report":
            {
                var monitors = REPlugin.Instance.MonitorManager;
                System.Action<string> writer = s => REPlugin.Instance.Chat.WriteLine(s);
                monitors.Banking.Report(writer, () => monitors.Rewards.Report(writer));
                return true;
            }

            case "clearqueue":
                // Clears the dispatch pipeline queue
                REPlugin.Instance.Dispatch.Pipeline.Clear();
                return true;

            case "face":
                Actions.SimpleActions.FaceObject(command);
                return true;
            
            case "navto":
                Actions.SimpleActions.NavTo(command);
                return true;

            case "goto":
                Actions.Dispatched.GoTo.Create(command).Enqueue();
                return true;

            case "pets":
                Actions.SimpleActions.ProcessPetsCommand(command);
                return true;

            case "test":
                Actions.TestingActions.ProcessTestCommand(command);
                return true;

            // PhatAC Based
            case "tele":
                Actions.SimpleActions.Teleport(command);
                return true;

            case "teleinto":
                Actions.SimpleActions.TeleportInto(command);
                return true;

            // Handled by the wrapper (WrapperCommandListener).  Decal raises CommandLineText for every
            // subscriber regardless of Eat, so claim it here to avoid "Unknown Command" and un-eating it.
            case "agent":
                return true;

            default:
                REPlugin.Instance.Chat.WriteLine("Unknown Command : {0} ", command.RawValue);
                return false;
        }
    }

    private static void ProcessUnstuckDetect(ICommand command)
    {
        var detector = REPlugin.Instance.MonitorManager.StuckDetector;
        bool? requestedState = null;
        double? minutes = null;

        foreach (var rawArg in command.Arguments)
        {
            var arg = rawArg.ToLower().Trim();

            if (arg == "on")
            {
                requestedState = true;
            }
            else if (arg == "off")
            {
                requestedState = false;
            }
            else if (double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedMinutes) && parsedMinutes > 0)
            {
                minutes = parsedMinutes;
            }
            else
            {
                REPlugin.Instance.Chat.WriteLine("Unknown unstuckdetect option : {0}", rawArg);
                return;
            }
        }

        bool newState = requestedState ?? (minutes.HasValue || !detector.Enabled);

        if (newState)
        {
            detector.Enable(minutes);
            REPlugin.Instance.Chat.WriteLine("[RE] Unstuck detect: on ({0} min)", detector.ThresholdMinutes.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            detector.Disable();
            REPlugin.Instance.Chat.WriteLine("[RE] Unstuck detect: off");
        }
    }

    private static void ProcessRecordRoute(ICommand command)
    {
        var recorder = REPlugin.Instance.MonitorManager.RouteRecorder;
        var routeName = command.Arguments.Count > 0 ? VTNavRoute.NormalizeName(command.Arguments[0]) : string.Empty;

        if (routeName.Length == 0 || routeName.Equals("stop", StringComparison.OrdinalIgnoreCase))
        {
            recorder.Stop();
            return;
        }

        var routeType = VTNavRouteType.Circular;
        if (command.Arguments.Count > 1)
        {
            switch (command.Arguments[1].ToLower().Trim())
            {
                case "circular":
                    routeType = VTNavRouteType.Circular;
                    break;
                case "linear":
                    routeType = VTNavRouteType.Linear;
                    break;
                case "once":
                    routeType = VTNavRouteType.Once;
                    break;
                default:
                    REPlugin.Instance.Chat.WriteLine("Unknown recordroute route type : {0}", command.Arguments[1]);
                    return;
            }
        }

        recorder.Start(routeName, routeType);
    }

    private static void ProcessStartMeta(ICommand command)
    {
        if (command.Arguments.Count == 0)
        {
            REPlugin.Instance.Chat.WriteLine("Usage : /re startmeta|sm <name>");
            return;
        }

        Actions.CharacterActions.StartMeta(command.Arguments[0]);
    }

    private static void ProcessSchedule(ICommand command)
    {
        var scheduler = REPlugin.Instance.MonitorManager.Scheduler;
        var chat = REPlugin.Instance.Chat;

        if (command.Arguments.Count > 0)
        {
            if (command.Arguments[0].Trim().Equals("reload", StringComparison.OrdinalIgnoreCase))
            {
                scheduler.RequestReload();
                chat.WriteLine("[RE] Schedule : reloading {0}", scheduler.FilePath);
            }
            else
            {
                chat.WriteLine("Usage : /re schedule|sched [reload]");
            }

            return;
        }

        var entries = scheduler.Snapshot();
        var now = DateTime.Now;
        chat.WriteLine("[RE] Schedule : {0} entr{1} in {2}", entries.Count, entries.Count == 1 ? "y" : "ies", scheduler.FilePath);
        foreach (var entry in entries)
        {
            var next = Scheduling.ScheduleRules.NextRun(entry, now);
            chat.WriteLine("  {0} {1} {2} next {3} : {4}",
                entry.Id,
                entry.Repeat,
                Scheduling.ScheduleRules.Status(entry, now),
                next.HasValue ? next.Value.ToString("yyyy-MM-dd HH:mm") : "-",
                entry.Command);
        }
    }

    private const string MetaChainWatchPrefix = "[RE] MetaChainWatch : ";
    private const string MetaChainWatchUsage = "Usage : /re metachainwatch|mcw [on <minutes>|off|status|reload]";

    private static void ProcessMetaChainWatch(ICommand command)
    {
        var watcher = REPlugin.Instance.MonitorManager.MetaChainWatcher;
        var chat = REPlugin.Instance.Chat;
        var sub = command.Arguments.Count > 0 ? command.Arguments[0].ToLower().Trim() : "status";

        switch (sub)
        {
            case "on":
                {
                    if (command.Arguments.Count < 2
                        || !double.TryParse(command.Arguments[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes)
                        || !(minutes > 0)
                        || double.IsInfinity(minutes))
                    {
                        chat.WriteLine(MetaChainWatchPrefix + "on needs a timeout in minutes greater than 0, e.g. /re metachainwatch on 60");
                        return;
                    }

                    var result = watcher.TurnOn(minutes);
                    if (result.Started)
                    {
                        var status = watcher.GetStatus();
                        chat.WriteLine("{0}on, {1} min (meta: {2})", MetaChainWatchPrefix, FormatMinutes(minutes), status.CurrentMeta ?? "none");
                    }
                    else
                    {
                        chat.WriteLine("{0}already on, timeout {1} -> {2} min", MetaChainWatchPrefix, FormatMinutes(result.PreviousMinutes), FormatMinutes(minutes));
                    }

                    return;
                }

            case "off":
                chat.WriteLine(watcher.TurnOff().WasOn ? MetaChainWatchPrefix + "off" : MetaChainWatchPrefix + "already off");
                return;

            case "status":
                ShowMetaChainWatchStatus(watcher);
                return;

            case "reload":
                watcher.RequestReload();
                chat.WriteLine("{0}reloading {1} and {2}", MetaChainWatchPrefix, watcher.GlobalFilePath, watcher.CharacterFilePath);
                return;

            default:
                chat.WriteLine(MetaChainWatchUsage);
                return;
        }
    }

    private static void ShowMetaChainWatchStatus(Listeners.Monitors.MetaChainWatcher watcher)
    {
        var chat = REPlugin.Instance.Chat;
        var status = watcher.GetStatus();
        var merged = watcher.MergedConfig;

        if (status.IsOn)
            chat.WriteLine("{0}on, {1} min, {2}", MetaChainWatchPrefix, FormatMinutes(status.MainMinutes), status.Phase);
        else
            chat.WriteLine("{0}off", MetaChainWatchPrefix);

        if (status.CurrentMeta == null)
        {
            chat.WriteLine("  meta : none seen");
        }
        else if (status.IsOn && status.Elapsed.HasValue)
        {
            var limit = MetaChainWatch.MetaChainWatchRules.EffectiveLimit(merged, status.CurrentMeta, status.MainMinutes);
            chat.WriteLine("  meta : {0}, {1} / {2} min{3}",
                status.CurrentMeta,
                FormatMinutes(status.Elapsed.Value.TotalMinutes),
                FormatMinutes(limit.Limit.TotalMinutes),
                limit.IsOverride ? " (override)" : string.Empty);
        }
        else
        {
            chat.WriteLine("  meta : {0}, loaded {1} min ago", status.CurrentMeta, FormatMinutes(status.SinceLoad?.TotalMinutes ?? 0));
        }

        if (status.FallbackDeadline.HasValue)
            chat.WriteLine("  waiting for the next meta, fallback at {0}", status.FallbackDeadline.Value.ToString("HH:mm:ss"));

        chat.WriteLine("  fallback : {0}", merged.FallbackCommand);
        chat.WriteLine("  chain states : {0}", string.Join(", ", merged.ChainStates.Select(s => "'" + s + "'").ToArray()));
        chat.WriteLine("  timeouts : {0}", merged.Timeouts.Count == 0 ? "none" : merged.Timeouts.Count.ToString(CultureInfo.InvariantCulture));

        foreach (var entry in merged.Timeouts.Values.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            chat.WriteLine("  {0} : {1} min{2} ({3})",
                entry.Name,
                FormatMinutes(entry.Minutes),
                entry.ChainState != null ? " [" + entry.ChainState + "]" : string.Empty,
                entry.Source);
        }
    }

    private static string FormatMinutes(double minutes) => Listeners.Monitors.MetaChainWatcher.FormatMinutes(minutes);

    private static void DisplayHelp()
    {
        var chat = REPlugin.Instance.Chat;

        chat.WriteLine("**** RedoxExtensions Commands (prefix with /re) ****");

        chat.WriteLine("  help                    - Displays this help.");
        chat.WriteLine("  exit                    - Exits the game.");

        chat.WriteLine("  fellowup                - Fellows the usual suspects.");
        chat.WriteLine("  fellowupb               - Fellows the usual suspects (break existing fellowship first).");
        chat.WriteLine("  pullkeys                - Pulls legendary keys.");

        chat.WriteLine("  use <target>            - Uses the given object.");
        chat.WriteLine("  give <items>            - Gives items to the current selection.");
        chat.WriteLine("  list <items>            - Lists matching items.");
        chat.WriteLine("  cram <items>            - Crams items into a container.");

        chat.WriteLine("  copycat|cc <on|off>     - Enables or disables copycat mode.");
        chat.WriteLine("  showcolors [on|off]     - Toggles color info on object ID (no arg = flip).");
        chat.WriteLine("  unstuck|us              - Tries to get a stuck VT meta moving again.");
        chat.WriteLine("  unstuckdetect|usdetect [on|off] [min] - Toggles automatic unstuck after [min] idle minutes (default 1).");
        chat.WriteLine("  recordroute|rr <name> [circular|linear|once] - Records a VT nav route while you move (default circular).");
        chat.WriteLine("  recordroute|rr [stop]   - Stops recording and loads the route in VT.");
        chat.WriteLine("  startmeta|sm <name>     -Loads Normal profile, resets VT/fellow state, loads and starts the named meta (exact name, or a unique partial match).");
        chat.WriteLine("  schedule|sched [reload] - Lists this character's scheduled commands (edit them with rectl schedule).");
        chat.WriteLine("  metachainwatch|mcw on <min> - Watches the VT meta chain: a meta running past <min> (or its shorter override) goes to its chain state, else the fallback runs.");
        chat.WriteLine("  metachainwatch|mcw off|status|reload - Stops the watch, shows it and the timeout table (edit with rectl metatimeout), or re-reads the config.");
        chat.WriteLine("  switch <name> [command] - Logs out and logs in <name> on this account (via Mag-Filter), then runs [command] on it.");
        chat.WriteLine("  track                   - Starts/resets bank tracking (records a baseline).");
        chat.WriteLine("  report                  - Reports bank balances and rate of increase per hour.");
        chat.WriteLine("  clearqueue              - Clears the dispatch pipeline queue.");
        chat.WriteLine("  face <target>           - Faces the given object.");
        chat.WriteLine("  goto <target>           - Travels to the given object.");
        chat.WriteLine("  pets <args>             - Processes a pets command.");
        chat.WriteLine("  test <args>             - Runs a testing command.");

        chat.WriteLine("  tele [target]           - Teleports to the target (or the requestor).");
        chat.WriteLine("  teleinto <target>      - Teleports into the given target.");

        chat.WriteLine("****************************************************");
    }
}
