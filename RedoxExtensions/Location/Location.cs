using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;

using Decal.Adapter.Wrappers;
using RedoxExtensions.Data;
using RedoxLib.Objects;

namespace RedoxExtensions.Location
{
    public class Location : AbstractSerializableData, IHaveCoordsObject
    {
        private CoordsObject _coords;

        public Location(double x, double y, double z, int landblock, double headingInDegrees)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.LandBlock = landblock;
            this.HeadingInDegrees = headingInDegrees;
            this._coords = Mag.Shared.Util.GetCoords(landblock, x, y);
        }

        /// <summary>
        /// Needed for deserialization
        /// </summary>
        public Location()
        {
        }

        public static Location CaptureCurrent()
        {
            return new Location(
                REPlugin.Instance.Actions.LocationX,
                REPlugin.Instance.Actions.LocationY,
                REPlugin.Instance.Actions.LocationZ,
                REPlugin.Instance.Actions.Landcell,
                REPlugin.Instance.Actions.Heading);
        }

        public bool CurrentIsSameAs(Location otherLocation, bool compareHeading)
        {
            return !CurrentDiffersFrom(otherLocation, compareHeading);
        }

        public static bool CurrentDiffersFrom(Location otherLocation, bool compareHeading)
        {
            if(otherLocation.X != REPlugin.Instance.Actions.LocationX)
            {
                return true;
            }

            if(otherLocation.Y != REPlugin.Instance.Actions.LocationY)
            {
                return true;
            }

            if(otherLocation.Z != REPlugin.Instance.Actions.LocationZ)
            {
                return true;
            }

            if (compareHeading)
            {
                if (otherLocation.HeadingInDegrees != REPlugin.Instance.Actions.Heading)
                {
                    return true;
                }
            }

            return false;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public int LandBlock { get; set; }
        public double HeadingInDegrees { get; set; }

        [ScriptIgnore]
        public CoordsObject Coords
        {
            get
            {
                // If the instance was created after deserialization, this will be null, so lazy create it
                if (this._coords == null)
                {
                    this._coords = Mag.Shared.Util.GetCoords(this.LandBlock, this.X, this.Y);
                }

                return this._coords;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public string ToPrettyString()
        {
            return string.Format("({0}, {1}, {2}, {3})", this.Coords.NorthSouth.ToString("F1"), this.Coords.EastWest.ToString("F1"), this.Z.ToString("F2"), this.HeadingInDegrees.ToString("F1"));
        }
    }
}
