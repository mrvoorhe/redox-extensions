using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Actions;

namespace RedoxExtensions.Commands.Handlers
{
    internal static class RedoxFellowHandler
    {
        internal static bool HandleCommand(ICommand command)
        {
            REPlugin.Instance.Debug.WriteObject(command);

            switch (command.Name.ToLower())
            {
                case "help":
                    DisplayHelp();
                    return true;

                // Some commands can't be translated straight to a fellow command because some additional data is needed.  Handle
                // these first.
                case "use":
                    // Can be used for anything.
                    if (command.Arguments.Count == 0)
                    {
                        TellActions.TellFellow("!use {0}", REPlugin.Instance.PluginHost.Actions.CurrentSelection);
                    }
                    else
                    {
                        var argsConvertedToForgeinForm = CommandHelpers.CollapseForeignCommandArguments(command.Arguments);
                        TellActions.TellFellow("!use {0}|{1}", REPlugin.Instance.PluginHost.Actions.CurrentSelection, argsConvertedToForgeinForm);
                    }
                    return true;
                case "give":
                    TellActions.TellFellow("!give {0}|{1}", command.Arguments[0], REPlugin.Instance.PluginHost.Actions.CurrentSelection);
                    return true;

                case "goto":

                    if (command.Arguments.Count > 0)
                    {
                        TellActions.TellFellow("!goto {0}", CommandHelpers.CollapseForeignCommandArguments(command.Arguments));
                        return true;
                    }

                    // If there are no arguments, use the current selection
                    TellActions.TellFellow("!goto {0}", REPlugin.Instance.PluginHost.Actions.CurrentSelection);
                    return true;

                default:
                    // By default, pass along the arguments as is
                    TellActions.TellFellow("!{0} {1}", command.Name, CommandHelpers.CollapseForeignCommandArguments(command.Arguments));
                    return true;
            }
        }

        private static void DisplayHelp()
        {
            var chat = REPlugin.Instance.Chat;

            chat.WriteLine("**** RedoxFellow Commands (prefix with /rf) ****");
            chat.WriteLine("Sends commands to your fellowship as foreign (!) commands.");

            chat.WriteLine("  help                    - Displays this help.");
            chat.WriteLine("  use [target]            - Tells the fellow to use the target (or your current selection).");
            chat.WriteLine("  give <item>             - Tells the fellow to give the item to your current selection.");
            chat.WriteLine("  goto [target]           - Tells the fellow to travel to the target (or your current selection).");

            chat.WriteLine("  <other> <args>          - Forwarded to the fellowship as \"!<other> <args>\".");

            chat.WriteLine("************************************************");
        }
    }
}
