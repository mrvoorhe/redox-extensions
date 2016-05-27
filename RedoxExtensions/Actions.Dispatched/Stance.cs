using System;
using Decal.Adapter.Wrappers;
using RedoxExtensions.Commands;
using RedoxExtensions.Core;
using RedoxExtensions.Dispatching;
using RedoxLib;
using RedoxLib.Objects.Extensions;

namespace RedoxExtensions.Actions.Dispatched
{
    public static class Stance
    {
        public const string DefaultStanceName = "basic";

        public static IAction Create(ICommand command)
        {
            // Expected arguments : 
            // stance name
            // center NorthSouth
            // center EastWest
            if (command.Arguments.Count != 3)
            {
                throw new ArgumentException(string.Format("This command expects 3 arguments, but had : {0}", command.Arguments.Count));
            }

            var stanceName = command.Arguments[0];
            var ns = double.Parse(command.Arguments[1]);
            var ew = double.Parse(command.Arguments[2]);

            return Create(command, stanceName, new CoordsObject(ns, ew));
        }

        public static IAction Create(ISupportFeedback requestor, string stanceName, CoordsObject centerPoint)
        {
            var newPostion = CalculateStancePosition(requestor, stanceName, CurrentCharacter.Name, centerPoint);

            requestor.GiveFeedback(FeedbackType.Information, "Going to : {0}", newPostion.ToString());

            return GoTo.Create(requestor, newPostion);
        }

        private static CoordsObject CalculateStancePosition(ISupportFeedback requestor, string stanceName, string characterName, CoordsObject centerPoint)
        {
            switch (stanceName)
            {
                case DefaultStanceName:
                    switch (characterName)
                    {
                        case "Redox":
                            // For now keep it real simple
                            return centerPoint;
                        case "Kreap":
                            return centerPoint.Shift(1.0, 0);
                        case "Rathion":
                            throw new NotImplementedException();
                        case "Rockdown Guy":
                            throw new NotImplementedException();
                        default:
                            throw new DisplayToUserException(string.Format("Don't know stance location for {0} in stance : {1}", characterName, stanceName), requestor);

                    }
                default:
                    throw new DisplayToUserException(string.Format("Unknown stance : {0}", stanceName), requestor);
            }
        }
    }
}
