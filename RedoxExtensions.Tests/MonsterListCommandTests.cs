using System.Linq;

using NUnit.Framework;

using RedoxExtensions.Commands;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Tests;

[TestFixture]
public class MonsterListCommandTests
{
    private static bool Parse(string arguments, out MonsterListCommand command, out string error)
        => MonsterListCommand.TryParse(CommandHelpers.SplitDirectEntryArguments(arguments).ToList(), out command, out error);

    [TestCase("")]
    [TestCase("list")]
    [TestCase("LIST")]
    public void List(string arguments)
    {
        Assert.That(Parse(arguments, out var command, out _), Is.True);
        Assert.That(command.Action, Is.EqualTo(MonsterListAction.List));
    }

    [TestCase("get Drudge Prowler", "Get", "Drudge Prowler", 0)]
    [TestCase("remove Drudge Prowler", "Remove", "Drudge Prowler", 0)]
    [TestCase("rm Mosswart", "Remove", "Mosswart", 0)]
    [TestCase("set 3 Drudge Prowler", "Set", "Drudge Prowler", 3)]
    [TestCase("add -1 Olthoi Ripper", "Add", "Olthoi Ripper", -1)]
    [TestCase("Set 0 <DEFAULT>", "Set", "<DEFAULT>", 0)]
    [TestCase("set  2   Drudge  Prowler ", "Set", "Drudge  Prowler", 2)]
    [TestCase("add 4 Tusker 2", "Add", "Tusker 2", 4)]
    public void NameIsTheRestOfTheLine(string arguments, string action, string name, int priority)
    {
        Assert.That(Parse(arguments, out var command, out var error), Is.True, error);
        Assert.That(command.Action.ToString(), Is.EqualTo(action));
        Assert.That(command.Name, Is.EqualTo(name));
        Assert.That(command.Priority, Is.EqualTo(priority));
    }

    [TestCase("list extra")]
    [TestCase("bogus")]
    [TestCase("get")]
    [TestCase("remove   ")]
    [TestCase("set Drudge Prowler")]
    [TestCase("set 3")]
    [TestCase("add")]
    [TestCase("add high Drudge")]
    public void Rejects(string arguments)
    {
        Assert.That(Parse(arguments, out var command, out var error), Is.False);
        Assert.That(command, Is.Null);
        Assert.That(error, Is.Not.Empty);
    }
}
