using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxLib.Location
{
    public struct Town
    {
        public readonly string Name;

        public Town(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            var other = (Town) obj;
            return Name == other.Name;
        }

        public static bool operator ==(Town a, Town b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Town a, Town b)
        {
            return !(a == b);
        }

        public static bool TryParse(string value, out Town town)
        {
            town = default(Town);
            if (string.IsNullOrEmpty(value))
                return false;

            var sanitizedName = LookupTown(value);
            if (string.IsNullOrEmpty(sanitizedName))
                return false;

            town = new Town(sanitizedName);
            return true;
        }

        private static string LookupTown(string userValue)
        {
            switch (userValue.Trim().ToLower())
            {
                // Aluvian
                case "holtburg": // official name
                case "holt":
                    return "Holtburg";
                case "cragstone": // official name
                case "crag":
                    return "Cragstone";
                case "arwic": // official name
                    return "Arwic";
                case "dryreach": // official name
                    return "Dryreach";
                case "eastham": // official name
                    return "Eastham";
                case "fort tethana": // official name
                case "teth":
                    return "Fort Tethana";
                case "glenden wood": // official name
                case "glenden":
                    return "Glenden Wood";
                case "lytelthorpe": // official name
                    return "Lytelthorpe";
                case "plateau village": // official name
                case "plateau":
                    return "Plateau Village";
                case "rithwic": // official name
                    return "Rithwic";
                case "stonehold": // official name
                    return "Stonehold";
                case "underground city": // official name
                    return "Underground City";

                // Sho
                case "shoushi": // official name
                    return "Shoushi";
                case "hebian-to": // official name
                case "heb":
                    return "Hebian-To";
                case "baishi": // official name
                    return "Baishi";
                case "kara": // official name
                    return "Kara";
                case "kryst": // official name
                    return "Kryst";
                case "lin": // official name
                    return "Lin";
                case "mayoi": // official name
                    return "Mayoi";
                case "nanto": // official name
                    return "Nanto";
                case "sawato": // official name
                    return "Sawato";
                case "tou-tou": // official name
                case "tou tou":
                    return "Tou-Tou";
                case "wai jhou": // official name
                case "bh":
                    return "Wai Jhou";
                case "yanshi": // official name
                    return "Yanshi";

                // Gharu'ndim
                case "yaraq": // official name
                    return "Yaraq";
                case "zaikhal": // official name
                    return "Zaikhal";
                case "al-arqas": // official name
                    return "Al-Arqas";
                case "al-jalima": // official name
                    return "Al-Jalima";
                case "ayan baqur": // official name
                case "ayan":
                case "ab":
                    return "Ayan Baqur";
                case "khayyaban": // official name
                    return "Khayyaban";
                case "qalaba'r": // official name
                case "qal":
                    return "Qalaba'r";
                case "samsur": // official name
                    return "Samsur";
                case "tufa": // official name
                    return "Tufa";
                case "uziz": // official name
                    return "Uziz";
                case "xarabydun": // official name
                    return "Xarabydun";

                // New Viamont
                case "sanamar": // official name
                    return "Sanamar";
                case "bluespire": // official name
                    return "Bluespire";
                case "eastwatch": // official name
                    return "Eastwatch";
                case "greenspire": // official name
                    return "Greenspire";
                case "redspire": // official name
                    return "Redspire";
                case "silyun": // official name
                    return "Silyun";
                case "westwatch": // official name
                    return "Westwatch";

                // Independent/Other Towns
                case "ahurenga": // official name
                    return "Ahurenga";
                case "bandit Castle": // official name
                    return "Bandit Castle";
                case "beach Fort": // official name
                    return "Beach Fort";
                case "candeth keep": // official name
                case "candeth":
                case "ck":
                    return "Candeth Keep";
                case "crater lake village": // official name
                case "crater":
                    return "Crater Lake Village";
                case "danby's outpost": // official name
                    return "Danby's Outpost";
                case "fiun outpost": // official name
                    return "Fiun Outpost";
                case "kor-gursha": // official name
                    return "Kor-Gursha";
                case "linvak tukal": // official name
                case "linvak":
                    return "Linvak Tukal";
                case "macniall's freehold": // official name
                case "freehold":
                    return "MacNiall's Freehold";
                case "mar'uun": // official name
                    return "Mar'uun";
                case "merwart village": // official name
                case "merwart":
                    return "Merwart Village";
                case "neydisa castle": // official name
                case "neydisa":
                    return "Neydisa Castle";
                case "oolatanga's refuge": // official name
                    return "Oolatanga's Refuge";
                case "timaru": // official name
                    return "Timaru";
                default:
                    return null;
            }
    }
    }
}
