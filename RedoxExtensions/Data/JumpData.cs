using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Data
{
    public class JumpData
    {
        public JumpData(Location fromLocation, double heading, double height)
        {
            this.Location = fromLocation;
            this.Heading = heading;
            this.Height = height;
        }

        public Location Location { get; private set; }
        public double Heading { get; private set; }
        public double Height { get; private set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} - {1} - {2}", this.Location, this.Heading.ToString("F2"), this.Height.ToString("F2"));
        }
    }
}
