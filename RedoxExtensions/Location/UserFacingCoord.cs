using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Location
{
    public struct UserFacingCoord
    {
        public readonly Direction Direction;
        public readonly double Value;

        public UserFacingCoord(double value, Direction direction)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"{nameof(value)} must be greater than 0.0.  Was {value}");
            Value = value;
            Direction = direction;
        }

        public override string ToString() => $"{Value}{Direction}";

        public override int GetHashCode() => ToString().GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var other = (UserFacingCoord)obj;
            return Value == other.Value && Direction == other.Direction;
        }

        public static bool operator ==(UserFacingCoord a, UserFacingCoord b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(UserFacingCoord a, UserFacingCoord b)
        {
            return !(a == b);
        }

        public static bool TryParse(string value, out UserFacingCoord coord)
        {
            coord = default(UserFacingCoord);
            if (string.IsNullOrEmpty(value))
                return false;

            var sanitizedValue = value.Trim().TrimEnd('.', ',');

            var directionValue = sanitizedValue[sanitizedValue.Length - 1].ToString().ToUpper();
            if (directionValue is not ("N" or "S" or "E" or "W"))
                return false;

            if (!Double.TryParse(sanitizedValue.Substring(0, sanitizedValue.Length - 1), out var doubleValue))
                return false;

            coord = new UserFacingCoord(doubleValue, (Direction)Enum.Parse(typeof(Direction), directionValue));
            return true;
        }
    }
}
