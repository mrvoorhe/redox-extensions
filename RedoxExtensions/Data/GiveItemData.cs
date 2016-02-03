using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxExtensions.General.Utilities;

namespace RedoxExtensions.Data
{
    public class GiveItemData
    {
        public GiveItemData(WorldObject item, int giveCount, int targetId)
        {
            this.Item = item;
            this.GiveCount = giveCount;
            this.TargetId = targetId;
        }

        public WorldObject Item { get; private set; }
        public int GiveCount { get; private set; }
        public int TargetId { get; private set; }

        public static GiveItemSearchData From(Triple<Func<WorldObject, bool>, int, int> data)
        {
            return new GiveItemSearchData(data.Item1, data.Item2, data.Item3);
        }
    }

    public class GiveItemSearchData
    {
        public GiveItemSearchData(Func<WorldObject, bool> predicate, int giveCount, int targetId)
        {
            this.Predicate = predicate;
            this.GiveCount = giveCount;
            this.TargetId = targetId;
        }

        public Func<WorldObject, bool> Predicate { get; private set; }
        public int GiveCount { get; private set; }
        public int TargetId { get; private set; }

        public static GiveItemSearchData From(Triple<Func<WorldObject, bool>, int, int> data)
        {
            return new GiveItemSearchData(data.Item1, data.Item2, data.Item3);
        }

        public IEnumerable<GiveItemData> GetMatches(IEnumerable<WorldObject> sourceCollection)
        {
            return sourceCollection.Where(w => this.Predicate(w)).Select(w => new GiveItemData(w, this.GiveCount, this.TargetId));
        }
    }
}
