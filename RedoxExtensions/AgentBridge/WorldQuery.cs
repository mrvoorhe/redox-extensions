using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.AgentBridge;

/// <summary>
/// One object as the 'world' request reports it.  Plain data so the filtering can be tested without Decal.
/// </summary>
internal sealed class WorldItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ObjectClass { get; set; }

    /// <summary>Numeric <c>ObjectClass</c>, as VT nav files store it (Portal 14, Npc 37).</summary>
    public int ObjectClassId { get; set; }
    public int Container { get; set; }
    public int Wielder { get; set; }
    public int StackCount { get; set; }
    public bool HasIdData { get; set; }

    /// <summary>Position in its container (side packs: 0 is the first pack in the inventory window), or -1.</summary>
    public int Slot { get; set; } = -1;

    /// <summary>Meters from the character.  Landscape only.</summary>
    public double? Distance { get; set; }
    public double? NorthSouth { get; set; }
    public double? EastWest { get; set; }

    /// <summary>Height in nav units (raw Z / 240), as VT stores an object's position in a nav "use" node.</summary>
    public double? NavZ { get; set; }

    public Dictionary<string, object> ToJson()
    {
        var json = new Dictionary<string, object>
        {
            ["id"] = this.Id,
            ["name"] = this.Name,
            ["class"] = this.ObjectClass,
            ["classId"] = this.ObjectClassId,
        };

        if (this.Container != 0)
            json["container"] = this.Container;
        if (this.Wielder != 0)
            json["wielder"] = this.Wielder;
        if (this.StackCount > 1)
            json["stack"] = this.StackCount;
        if (this.Slot >= 0)
            json["slot"] = this.Slot;
        if (this.HasIdData)
            json["hasIdData"] = true;
        if (this.Distance.HasValue)
            json["distance"] = Math.Round(this.Distance.Value, 1);
        if (this.NorthSouth.HasValue && this.EastWest.HasValue)
        {
            json["ns"] = Math.Round(this.NorthSouth.Value, 2);
            json["ew"] = Math.Round(this.EastWest.Value, 2);

            // Full precision [ew, ns, z], what a nav "use" node needs to find the object
            if (this.NavZ.HasValue)
                json["nav"] = new List<object> { this.EastWest.Value, this.NorthSouth.Value, this.NavZ.Value };
        }

        return json;
    }
}

/// <summary>
/// Parameters of a 'world' request, and the filtering and paging applied to the objects it covers.
/// </summary>
internal sealed class WorldQuery
{
    public const string ScopeLandscape = "landscape";
    public const string ScopeInventory = "inventory";

    public const int DefaultLimit = 50;

    /// <summary>Keeps a response well under the 85 KB large object heap threshold.</summary>
    public const int MaxLimit = 200;

    public string Scope { get; private set; } = ScopeLandscape;
    public Regex Name { get; private set; }
    public HashSet<string> Classes { get; private set; }
    public int? Container { get; private set; }
    public double? MaxDistance { get; private set; }
    public int Offset { get; private set; }
    public int Limit { get; private set; } = DefaultLimit;

    /// <summary>Throws <see cref="ArgumentException"/> with a message meant for the caller.</summary>
    public static WorldQuery Parse(IDictionary<string, object> request)
    {
        var query = new WorldQuery();

        var scope = MiniJson.GetString(request, "scope");
        if (!string.IsNullOrEmpty(scope))
        {
            scope = scope.ToLowerInvariant();
            if (scope != ScopeLandscape && scope != ScopeInventory)
                throw new ArgumentException($"Unknown scope '{scope}'.  Use {ScopeLandscape} or {ScopeInventory}.");
            query.Scope = scope;
        }

        var name = MiniJson.GetString(request, "name");
        if (!string.IsNullOrEmpty(name))
        {
            try
            {
                query.Name = new Regex(name, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"Bad name regex: {e.Message}");
            }
        }

        var classes = MiniJson.GetStringList(request, "class");
        if (classes != null)
        {
            query.Classes = new HashSet<string>(
                classes.SelectMany(c => c.Split(',')).Select(c => c.Trim()).Where(c => c.Length > 0),
                StringComparer.OrdinalIgnoreCase);
        }

        var container = MiniJson.GetLong(request, "container");
        if (container.HasValue)
            query.Container = (int)container.Value;

        var maxDistance = MiniJson.GetString(request, "maxDistance");
        if (!string.IsNullOrEmpty(maxDistance))
        {
            if (!double.TryParse(maxDistance, NumberStyles.Float, CultureInfo.InvariantCulture, out var meters) || meters < 0)
                throw new ArgumentException($"Bad maxDistance '{maxDistance}'.  Use meters, e.g. 30.");
            query.MaxDistance = meters;
        }

        query.Offset = (int)Math.Max(MiniJson.GetLong(request, "offset") ?? 0, 0);
        query.Limit = (int)Math.Min(Math.Max(MiniJson.GetLong(request, "limit") ?? DefaultLimit, 1), MaxLimit);

        return query;
    }

    public bool Matches(WorldItem item)
    {
        if (this.Name != null && !this.Name.IsMatch(item.Name ?? string.Empty))
            return false;
        if (this.Classes != null && !this.Classes.Contains(item.ObjectClass ?? string.Empty))
            return false;
        if (this.Container.HasValue && item.Container != this.Container.Value)
            return false;
        if (this.MaxDistance.HasValue && (!item.Distance.HasValue || item.Distance.Value > this.MaxDistance.Value))
            return false;
        return true;
    }

    /// <summary>
    /// Filters, sorts (landscape by distance, inventory by name) and pages <paramref name="items"/>.
    /// </summary>
    public Dictionary<string, object> Apply(IEnumerable<WorldItem> items)
    {
        var matches = items.Where(this.Matches);
        matches = this.Scope == ScopeLandscape
            ? matches.OrderBy(i => i.Distance ?? double.MaxValue).ThenBy(i => i.Id)
            : matches.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.Id);

        var all = matches.ToList();
        var page = all.Skip(this.Offset).Take(this.Limit).Select(i => (object)i.ToJson()).ToList();

        var result = new Dictionary<string, object>
        {
            ["scope"] = this.Scope,
            ["total"] = all.Count,
            ["offset"] = this.Offset,
            ["objects"] = page,
        };

        if (this.Offset + page.Count < all.Count)
            result["nextOffset"] = this.Offset + page.Count;

        return result;
    }
}
