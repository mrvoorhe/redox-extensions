using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;

using Decal.Adapter.Wrappers;

namespace RedoxLib.Objects
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
                PluginProvider.Instance.Actions.LocationX,
                PluginProvider.Instance.Actions.LocationY,
                PluginProvider.Instance.Actions.LocationZ,
                PluginProvider.Instance.Actions.Landcell,
                PluginProvider.Instance.Actions.Heading);
        }

        public bool CurrentIsSameAs(Location otherLocation, bool compareHeading)
        {
            return !CurrentDiffersFrom(otherLocation, compareHeading);
        }

        public static bool CurrentDiffersFrom(Location otherLocation, bool compareHeading)
        {
            if(otherLocation.X != PluginProvider.Instance.Actions.LocationX)
            {
                return true;
            }

            if(otherLocation.Y != PluginProvider.Instance.Actions.LocationY)
            {
                return true;
            }

            if(otherLocation.Z != PluginProvider.Instance.Actions.LocationZ)
            {
                return true;
            }

            if (compareHeading)
            {
                if (otherLocation.HeadingInDegrees != PluginProvider.Instance.Actions.Heading)
                {
                    return true;
                }
            }

            return false;
        }

        public static double CurrentHeading
        {
            get { return PluginProvider.Instance.Actions.Heading; }
        }

        public static bool HeadingMatches(double targetHeading)
        {
            return HeadingMatches(CurrentHeading, targetHeading);
        }

        public static bool HeadingMatches(double heading1, double heading2)
        {
            var h1Rounded = Math.Round(heading1, 3);
            var h2Rounded = Math.Round(heading2, 3);
            return h1Rounded == h2Rounded;
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
