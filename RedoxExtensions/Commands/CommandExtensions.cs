using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Commands
{
    public static class CommandExtensions
    {
        public static bool IsSlaveOnlyAndFromSelf(this ICommand command)
        {
            return command.CommandTargetType == TargetType.SlavesOnly && command.FromSelf;
        }

        public static string RebuildArgumentString(this ICommand command)
        {
            if (command.CommandType == CommandType.Foreign)
            {
                return CommandHelpers.CollapseForeignCommandArguments(command.Arguments);
            }

            if (command.CommandType == CommandType.RedoxExtension || command.CommandType == CommandType.RedoxFellow)
            {
                return CommandHelpers.CollapseDirectEntryCommandArguments(command.Arguments);
            }

            throw new InvalidOperationException("unknown command type");
        }

        public static string RebuildArgumentsWithSpaceSeparator(this ICommand command)
        {
            return command.Arguments.Aggregate(string.Empty, (accum, next) => string.Format("{0}{1}{2}", accum, ' ', next)).Trim();
        }
    }
}
