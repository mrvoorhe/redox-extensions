using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NiceIO;

namespace RedoxLib.Location
{
    static class DungeonDatabase
    {
        private static readonly Dictionary<string, Dungeon> knownDungeons = new Dictionary<string, Dungeon>();
        private static bool loaded;

        public static bool TryFind(string name, out Dungeon dungeon)
        {
            LoadIfNeeded();

            throw new NotImplementedException();
        }

        private static void LoadIfNeeded()
        {
            if (loaded)
                return;

            var databaseFile = new Uri(typeof(DungeonDatabase).Assembly.CodeBase).LocalPath.ToNPath().Parent.Combine("DungeonDatabase.txt");

            // TODO by mike : Left off here
            throw new NotImplementedException();
        }
    }
}
