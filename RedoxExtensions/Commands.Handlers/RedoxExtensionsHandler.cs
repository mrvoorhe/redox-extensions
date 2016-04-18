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

                default:
                    REPlugin.Instance.Chat.WriteLine("Unknown Command : {0} ", command.RawValue);
                    return false;
            }
        }

        private static void DisplayHelp()
        {
            REPlugin.Instance.Chat.WriteLine("********************");
            REPlugin.Instance.Chat.WriteLine("This will become the help!");
            REPlugin.Instance.Chat.WriteLine("********************");
        }
    }
}
