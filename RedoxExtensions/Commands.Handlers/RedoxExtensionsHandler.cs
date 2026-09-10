using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Core.Extensions;

namespace RedoxExtensions.Commands.Handlers
{
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

                case "clearqueue":
                    // Clears the dispatch pipeline queue
                    REPlugin.Instance.Dispatch.Pipeline.Clear();
                    return true;

                case "face":
                    Actions.SimpleActions.FaceObject(command);
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
}
