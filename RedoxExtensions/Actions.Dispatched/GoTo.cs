using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Commands;
using RedoxExtensions.Dispatching;
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Core;
using Decal.Adapter.Wrappers;

namespace RedoxExtensions.Actions.Dispatched
{
    /// <summary>
    /// Not a Pipeline action itself.
    /// 
    /// This class will automatically pick the right use implementation to use
    /// based on supplied Command
    /// </summary>
    public static class GoTo
    {
        private const int MaxDistanceToConsiderNearbyWhenOutdoors = 100;
        private const int MaxDistanceToConsiderNearbyWhenIndoors = 10;

        public static IAction Create(ICommand command)
        {
            if (command.Arguments.Count == 0)
            {
                throw new ArgumentException("At least 1 argument is required");
            }

            // TODO : Support passing in the final heading
            CoordsObject coords;
            if (command.Arguments.Count == 1)
            {
                // goto is being used to go to an object.  Locate the object
                var woId = int.Parse(command.Arguments[0]);
                var woObj = woId.ToWorldObject();

                if (woObj == null)
                {
                    command.GiveFeedback(FeedbackType.Ignored, "Could not locate object : {0}", woId);
                    throw new DisplayToUserException(string.Format("Could not locate object : {0}", woId), command);
                }

                coords = woObj.Coordinates();
                //x = woCoords.NorthSouth;
                //y = woCoords.EastWest;
            }
            else if (command.Arguments.Count == 2)
            {
                double ns = double.Parse(command.Arguments[0]);
                double ew = double.Parse(command.Arguments[1]);
                coords = new CoordsObject(ns, ew);
            }
            else
            {
                // Note : In the future maybe I support more complex goto abilities.
                throw new NotImplementedException("Support for more complicated goto operations has not been implemented");
            }

            return Create(command, coords);
        }

        public static IAction Create(ISupportFeedback requestor, CoordsObject coords)
        {
            // This overload is always equivalent to use goto point
            return Internal.GoToNearbyLocation.Create(requestor, coords, REPlugin.Instance.CoreManager.Actions.Heading);
        }
    }
}
