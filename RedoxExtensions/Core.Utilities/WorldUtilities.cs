using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxExtensions.Commands;
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Data;
using RedoxLib.Objects;

namespace RedoxExtensions.Core.Utilities
{
    public static class WorldUtilities
    {

        #region Find In Landscape

        public static IEnumerable<WorldObject> GetNearbyLandscapeObjectsByName(string objectName)
        {
            foreach (var wo in REPlugin.Instance.WorldFilter.GetLandscape())
            {
                if(wo.Name == objectName)
                {
                    yield return wo;
                }
            }
        }

        public static WorldObject GetFirstLandscapeObjectByName(string objectName)
        {
            foreach (var wo in REPlugin.Instance.WorldFilter.GetLandscape())
            {
                if (wo.Name == objectName)
                {
                    return wo;
                }
            }

            return null;
        }

        #endregion

        #region GetDistance

        /// <summary>
        /// This function will return the distance in meters.
        /// The manual distance units are in map compass units, while the distance units used in the UI are meters.
        /// In AC there are 240 meters in a kilometer; thus if you set your attack range to 1 in the UI it
        /// will showas 0.00416666666666667in the manual options (0.00416666666666667 being 1/240). 
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">Object passed with an Id of 0</exception>
        public static double GetDistance(WorldObject obj1, WorldObject obj2)
        {
            return GetDistance(obj1.Wrap(), obj2.Wrap());
        }

        /// <summary>
        /// This function will return the distance in meters.
        /// The manual distance units are in map compass units, while the distance units used in the UI are meters.
        /// In AC there are 240 meters in a kilometer; thus if you set your attack range to 1 in the UI it
        /// will showas 0.00416666666666667in the manual options (0.00416666666666667 being 1/240). 
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">Object passed with an Id of 0</exception>
        public static double GetDistance(IWorldObject obj1, IWorldObject obj2)
        {
            if (obj1.Id == 0)
                throw new ArgumentOutOfRangeException("obj1", "Object passed with an Id of 0");

            if (obj2.Id == 0)
                throw new ArgumentOutOfRangeException("obj2", "Object passed with an Id of 0");

            return REPlugin.Instance.WorldFilter.Distance(obj1.Id, obj2.Id) * 240;
        }

        public static double GetDistance(Location loc1, Location loc2)
        {
            return GetDistance(loc1.Coords, loc2.Coords);
        }

        public static double GetDistance(CoordsObject coords1, CoordsObject coords2)
        {
            return coords1.DistanceToCoords(coords2) * 240;
        }

        public static double GetDistanceFromSelf(ISourceInformation requestor)
        {
            if (requestor.IsSourceIdAvailable)
            {
                return GetDistanceFromSelf(requestor.SourceCharacterId);
            }

            return GetDistanceFromSelf(requestor.SourceCharacter);
        }

        public static double GetDistanceFromSelf(string objectName)
        {
            var match = GetFirstLandscapeObjectByName(objectName);

            if (match == null)
            {
                // Couldn't find the object near by
                return double.MaxValue;
            }

            return GetDistanceFromSelf(match);
        }

        /// <summary>
        /// This function will return the distance in meters.
        /// The manual distance units are in map compass units, while the distance units used in the UI are meters.
        /// In AC there are 240 meters in a kilometer; thus if you set your attack range to 1 in the UI it
        /// will showas 0.00416666666666667in the manual options (0.00416666666666667 being 1/240). 
        /// </summary>
        /// <param name="destObj"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">CharacterFilder.Id or Object passed with an Id of 0</exception>
        public static double GetDistanceFromSelf(int woId)
        {
            return GetDistanceFromSelf(woId.ToWorldObject());
        }

        /// <summary>
        /// This function will return the distance in meters.
        /// The manual distance units are in map compass units, while the distance units used in the UI are meters.
        /// In AC there are 240 meters in a kilometer; thus if you set your attack range to 1 in the UI it
        /// will showas 0.00416666666666667in the manual options (0.00416666666666667 being 1/240). 
        /// </summary>
        /// <param name="destObj"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">CharacterFilder.Id or Object passed with an Id of 0</exception>
        public static double GetDistanceFromSelf(WorldObject destObj)
        {
            return GetDistanceFromSelf(destObj.Wrap());
        }

        /// <summary>
        /// This function will return the distance in meters.
        /// The manual distance units are in map compass units, while the distance units used in the UI are meters.
        /// In AC there are 240 meters in a kilometer; thus if you set your attack range to 1 in the UI it
        /// will showas 0.00416666666666667in the manual options (0.00416666666666667 being 1/240). 
        /// </summary>
        /// <param name="destObj"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">CharacterFilder.Id or Object passed with an Id of 0</exception>
        public static double GetDistanceFromSelf(IWorldObject destObj)
        {
            if (REPlugin.Instance.CharacterFilter.Id == 0)
                throw new ArgumentOutOfRangeException("destObj", "CharacterFilter.Id of 0");

            if (destObj.Id == 0)
                throw new ArgumentOutOfRangeException("destObj", "Object passed with an Id of 0");

            return REPlugin.Instance.WorldFilter.Distance(REPlugin.Instance.CharacterFilter.Id, destObj.Id) * 240;
        }

        public static double GetDistanceFromSelf(Location point)
        {
            return GetDistanceFromSelf(point.Coords);
        }

        public static double GetDistanceFromSelf(CoordsObject point)
        {
            var selfLocation = Location.CaptureCurrent();

            return GetDistance(selfLocation.Coords, point);
        }

        #endregion

        #region WithinRangeOfSelf

        public static bool WithinRangeOfSelf(ISourceInformation requestor, int maxDistance)
        {
            if (requestor.FromSelf)
            {
                // Always within range of yourself
                return true;
            }

            if (requestor.IsSourceIdAvailable)
            {
                return WithinRangeOfSelf(requestor.SourceCharacterId, maxDistance);
            }

            // If the source id isn't available, that's not a good sign that they are near by,
            // but we'll give a by name check a chance just in case
            return WithinRangeOfSelf(requestor.SourceCharacter, maxDistance);
        }

        public static bool WithinRangeOfSelf(string objectName, int maxDistance)
        {
            var match = GetFirstLandscapeObjectByName(objectName);

            if(match == null)
            {
                // Couldn't find the object near by
                return false;
            }

            return WithinRangeOfSelf(match, maxDistance);
        }

        public static bool WithinRangeOfSelf(int worldObjectId, int maxDistance)
        {
            var wo = worldObjectId.ToWorldObject();

            if (wo == null)
            {
                // Couldn't find the object near by
                return false;
            }

            return WithinRangeOfSelf(wo.Coordinates(), maxDistance);
        }

        public static bool WithinRangeOfSelf(WorldObject wo, int maxDistance)
        {
            if (wo == null)
            {
                throw new ArgumentNullException("wo");
            }

            return WithinRangeOfSelf(wo.Coordinates(), maxDistance);
        }

        public static bool WithinRangeOfSelf(IWorldObject wo, int maxDistance)
        {
            if (wo == null)
            {
                throw new ArgumentNullException("wo");
            }

            return WithinRangeOfSelf(wo.Coordinates(), maxDistance);
        }

        public static bool WithinRangeOfSelf(Location location, int maxDistance)
        {
            return WithinRangeOfSelf(location.Coords, maxDistance);
        }

        public static bool WithinRangeOfSelf(CoordsObject coords, int maxDistance)
        {
            var tmp = GetDistanceFromSelf(coords);
            var distanceFromSelf = Math.Abs(tmp);

            return distanceFromSelf < maxDistance;
        }

        #endregion

        #region Face

        public static void Face(string objectName)
        {
            Face(GetFirstLandscapeObjectByName(objectName).Wrap());
        }

        public static void Face(int woId)
        {
            Face(woId.ToWorldObject());
        }

        public static void Face(IHaveCoordsObject coordsObject)
        {
            if(coordsObject == null)
            {
                throw new ArgumentNullException("coordsObject");
            }

            Face(coordsObject.Coords);
        }


        public static void Face(CoordsObject coords)
        {
            if (coords == null)
            {
                throw new ArgumentNullException("coords");
            }

            var selfCoords = Location.CaptureCurrent().Coords;
            var angle = selfCoords.AngleToCoords(coords);
            REPlugin.Instance.Actions.FaceHeading(angle, true);
        }

        #endregion
    }
}
