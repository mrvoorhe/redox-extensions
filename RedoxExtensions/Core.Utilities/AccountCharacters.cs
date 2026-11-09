using System;
using System.Collections.Generic;
using System.Linq;

namespace RedoxExtensions.Core.Utilities;

internal sealed class AccountCharacter
{
    public string Name { get; set; }
    public int Id { get; set; }
    public int Index { get; set; }

    public Dictionary<string, object> ToJson() => new()
    {
        ["name"] = this.Name,
        ["id"] = this.Id,
        ["index"] = this.Index,
    };
}

internal static class AccountCharacters
{
    public const string SwitchUsage = "Usage : /re switch <character name> [command]";

    public static List<AccountCharacter> SortedByName(IEnumerable<AccountCharacter> characters) =>
        characters.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>
    /// Arguments are split on spaces and names can contain spaces, so the longest run of leading arguments that is a
    /// character name wins.  Failing that, the first argument can be a unique prefix of a name.  The rest is the command.
    /// </summary>
    public static bool TryMatchSwitchArguments(
        IList<string> args,
        IList<string> names,
        string currentName,
        out string target,
        out string command,
        out string error)
    {
        target = null;
        command = null;
        error = null;

        var words = args.Where(a => !string.IsNullOrEmpty(a)).ToList();
        if (words.Count == 0)
        {
            error = SwitchUsage;
            return false;
        }

        var used = 0;
        for (var n = words.Count; n > 0 && target == null; n--)
        {
            var candidate = string.Join(" ", words.Take(n).ToArray());
            target = names.FirstOrDefault(name => string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase));
            used = n;
        }

        if (target == null)
        {
            var matches = names.Where(name => name.StartsWith(words[0], StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count != 1)
            {
                var others = names.Where(name => !string.Equals(name, currentName, StringComparison.OrdinalIgnoreCase));
                error = matches.Count == 0
                    ? $"No character named '{words[0]}' on this account.  Characters : {string.Join(", ", others.ToArray())}"
                    : $"'{words[0]}' matches more than one character : {string.Join(", ", matches.ToArray())}";
                return false;
            }

            target = matches[0];
            used = 1;
        }

        if (string.Equals(target, currentName, StringComparison.OrdinalIgnoreCase))
        {
            error = $"Already logged in as {target}";
            target = null;
            return false;
        }

        var rest = string.Join(" ", words.Skip(used).ToArray()).Trim();
        command = rest.Length == 0 ? null : rest;
        return true;
    }
}
