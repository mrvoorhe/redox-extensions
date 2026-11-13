using System.Collections.Generic;
using System.Globalization;

namespace RedoxExtensions.VirindiInterop;

internal enum MonsterListAction
{
    List,
    Get,
    Set,
    Add,
    Remove,
}

/// <summary>
/// The arguments of <c>/re monsters</c>.  The monster name comes last and is the rest of the line, spaces included,
/// because list entries are names like "Drudge Prowler" (or VTank match rules).
/// </summary>
internal sealed class MonsterListCommand
{
    public const string Usage = "Usage : /re monsters|mon [list] | get <name> | set <priority> <name> | add <priority> <name> | remove <name>";

    private MonsterListCommand(MonsterListAction action, string name, int priority)
    {
        this.Action = action;
        this.Name = name;
        this.Priority = priority;
    }

    public MonsterListAction Action { get; }

    public string Name { get; }

    public int Priority { get; }

    public static bool TryParse(IList<string> arguments, out MonsterListCommand command, out string error)
    {
        command = null;
        error = null;

        var rest = new Queue<string>(arguments ?? new string[0]);
        var sub = TakeToken(rest);
        if (sub == null)
        {
            command = new MonsterListCommand(MonsterListAction.List, null, 0);
            return true;
        }

        MonsterListAction action;
        switch (sub.ToLowerInvariant())
        {
            case "list":
                action = MonsterListAction.List;
                break;
            case "get":
                action = MonsterListAction.Get;
                break;
            case "set":
                action = MonsterListAction.Set;
                break;
            case "add":
                action = MonsterListAction.Add;
                break;
            case "remove":
            case "rm":
                action = MonsterListAction.Remove;
                break;
            default:
                error = Usage;
                return false;
        }

        if (action == MonsterListAction.List)
        {
            if (TakeToken(rest) != null)
            {
                error = Usage;
                return false;
            }

            command = new MonsterListCommand(action, null, 0);
            return true;
        }

        var priority = 0;
        if (action == MonsterListAction.Set || action == MonsterListAction.Add)
        {
            var priorityText = TakeToken(rest);
            if (priorityText == null || !int.TryParse(priorityText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out priority))
            {
                error = $"{sub.ToLowerInvariant()} needs a priority and then a monster name, e.g. /re monsters {sub.ToLowerInvariant()} 3 Drudge Prowler";
                return false;
            }
        }

        var name = string.Join(" ", rest.ToArray()).Trim();
        if (name.Length == 0)
        {
            error = $"{sub.ToLowerInvariant()} needs a monster name";
            return false;
        }

        command = new MonsterListCommand(action, name, priority);
        return true;
    }

    private static string TakeToken(Queue<string> rest)
    {
        while (rest.Count > 0)
        {
            var token = rest.Dequeue();
            if (!string.IsNullOrEmpty(token))
                return token;
        }

        return null;
    }
}
