using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;
using RedoxExtensions.Commands;
using RedoxExtensions.Core;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Actions.Dispatched
{
    public static class ListItems
    {
        public static IAction Create(ICommand command)
        {
            // Value Formats :
            // <keyword>

            string keyword = command.Arguments[0];
            string secondOption = command.Arguments.Count > 1 ? command.Arguments[1] : string.Empty;

            ReadOnlyCollection<WorldObject> itemsToGive = ItemUtilities.TryGetInventoryItemsForKeyword(keyword, secondOption);

            // Null return value means it's not a known keyword
            if (itemsToGive == null)
            {
                // If it's not a keyword, fall threw to the normal give
                return Internal.ListItemsByName.Create(command);
            }

            if (itemsToGive.Count == 0)
            {
                throw new DisplayToUserException("No items found", command);
            }

            var action = Internal.ListItemsByKeyword.Create(command, itemsToGive);
            return action;
        }
    }
}
