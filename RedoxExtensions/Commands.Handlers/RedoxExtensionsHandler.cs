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
                ProcessStartMeta(command);
                return true;

            case "track":
                REPlugin.Instance.MonitorManager.Banking.StartTracking(s => REPlugin.Instance.Chat.WriteLine(s));
                return true;

            case "report":
                REPlugin.Instance.MonitorManager.Banking.Report(s => REPlugin.Instance.Chat.WriteLine(s));
                return true;

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
            REPlugin.Instance.Chat.WriteLine("Usage : /re startmeta <name>");
            return;
        }

        Actions.CharacterActions.StartMeta(command.Arguments[0]);
    }

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
        chat.WriteLine("  startmeta <name>        - Loads Normal profile, resets VT/fellow state, loads and starts the named meta (exact name, or a unique partial match).");
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
