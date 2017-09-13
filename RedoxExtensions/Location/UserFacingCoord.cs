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

        public override string ToString()
        {
            return $"{Value}{Direction}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

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
            switch (directionValue)
            {
                case "N":
                case "S":
                case "E":
                case "W":
                    break;
                default:
                    return false;
            }

            double doubleValue;
            if (!Double.TryParse(sanitizedValue.Substring(0, sanitizedValue.Length - 1), out doubleValue))
                return false;

            coord = new UserFacingCoord(doubleValue, (Direction)Enum.Parse(typeof(Direction), directionValue));
            return true;
        }
    }
}
