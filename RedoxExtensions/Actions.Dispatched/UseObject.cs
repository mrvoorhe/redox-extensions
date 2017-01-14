using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxExtensions.Commands;
using RedoxExtensions.Core;
using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Actions.Dispatched
{
    /// <summary>
    /// Not a Pipeline action itself.
    /// 
    /// This class will automatically pick the right use implementation to use
    /// based on the object type.
    /// </summary>
    public static class UseObject
    {
        public static IAction Create(ICommand command)
        {
            int objectId;
            if (command.Arguments.Count == 0)
            {
                objectId = REPlugin.Instance.Actions.CurrentSelection;
            }
            else if (command.Arguments.Count > 1)
            {
                throw new DisplayToUserException(string.Format("Multi argument use is not currently supported : {0}", command.RebuildArgumentString()), command);
            }
            else
            {
                objectId = int.Parse(command.Arguments[0].Trim());
            }

            return Create(command, objectId);
        }

        public static IAction Create(ICommand command, int objectId)
        {
            var worldObj = REPlugin.Instance.CoreManager.WorldFilter[objectId];

            if (worldObj == null)
            {
                throw new DisplayToUserException(string.Format("Could not locate object : {0}", objectId), command);
            }

            switch (worldObj.ObjectClass)
            {
                case ObjectClass.Portal:
                    return Internal.UsePortal.Create(command, objectId);
                case ObjectClass.Npc:
                    if (worldObj.Name.ToLower().Contains("portal"))
                        return Internal.UsePortal.Create(command, objectId);

                    return Internal.UseNpc.Create(command, objectId);
                default:
                    throw new DisplayToUserException(string.Format("Unknown object class to use : {0}", worldObj.ObjectClass), command);
            }
        }
    }
}
