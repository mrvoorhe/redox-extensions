using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;

namespace RedoxExtensions.Location
{
    public class LocationDatabase
    {
        private readonly NPath _dataDirectory;

        public readonly Dictionary<string, Dungeon> Dungeons;

        public LocationDatabase()
            : this(DefaultDataLocation)
        {
        }

        public static NPath DefaultDataLocation => new Uri(typeof(LocationDatabase).Assembly.CodeBase).LocalPath.ToNPath().Parent.Combine("Databases");

        public LocationDatabase(NPath dataDirectory)
        {
            _dataDirectory = dataDirectory;

            Dungeons = LoadDungeonInfo(dataDirectory);
        }

        public static Dictionary<string, Dungeon> LoadDungeonInfo(NPath dataDirectory)
        {
            var allNames = new HashSet<string>();
            var dungeons = new Dictionary<string, Dungeon>();
            var dungeonToLocation = new Dictionary<string, List<UserFacingLocation>>();
            var dungeonToDropLocation = new Dictionary<string, List<FullLocation>>();

            foreach (var line in dataDirectory.Combine("DungeonsDatabase.txt").ReadAllLines())
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                var splitLine = line.Split(new[] {'\t'}, StringSplitOptions.RemoveEmptyEntries);

                // Either it's not a line with dungeon info, or it doesn't have the minimum info we need
                if (splitLine.Length < 3)
                    continue;

                // Skip header lines
                if (splitLine[0] == "Name")
                    continue;

                // if the first element is not a dungeon name, it's data we won't be able to parse
                if (!char.IsLetter(splitLine[0][0]))
                    continue;

                var name = splitLine[0];
                allNames.Add(name);

                var locationString = splitLine[1];

                // TODO by Mike : Handle multi location dungeons, ex: "Halls of Metos".  Probably going to need to write a tool to clean up the data rather than try and handle
                // the complexity of parsing here.

                UserFacingLocation location;
                if (!UserFacingLocation.TryParse(locationString, out location))
                {
                    // TODO by Mike : Still add it, but as null location
                    Console.WriteLine($"Failed to parse location for dungeon line : {line}");
                    continue;
                }

                List<UserFacingLocation> locations;
                if (dungeonToLocation.TryGetValue(name, out locations))
                    locations.Add(location);
                else
                {
                    locations = new List<UserFacingLocation>();
                    locations.Add(location);
                    dungeonToLocation.Add(name, locations);
                }
            }

            // TODO by Mike : Need to implement FullLocation in order to implement this section
            //foreach (var line in dataDirectory.Combine("Landblocks-Dungeons.tsv").ReadAllLines())
            //{
            //    if (string.IsNullOrEmpty(line))
            //        continue;

            //    var splitLine = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

            //    if (splitLine.Length < 3)
            //        continue;
            //}

            foreach (var name in allNames)
            {
                List<UserFacingLocation> locations;
                dungeonToLocation.TryGetValue(name, out locations);

                List<FullLocation> dropLocations;
                dungeonToDropLocation.TryGetValue(name, out dropLocations);

                dungeons.Add(name.ToLower(),
                    new Dungeon(
                        name.ToLower(),
                        name,
                        locations?.ToArray() ?? new UserFacingLocation[0],
                        dropLocations?.ToArray() ?? new FullLocation[0]));
            }

            // TODO by mike : Left off here
            //throw new NotImplementedException();
            return dungeons;
        }
    }
}
