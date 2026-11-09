using System.Collections.Generic;

using NUnit.Framework;

using RedoxExtensions.Core.Utilities;

namespace RedoxExtensions.Tests;

[TestFixture]
public class AccountCharactersTests
{
    private static readonly string[] Names = { "Virmar", "Woodford", "Bob", "Bob Smith", "Wanda" };

    private static (bool Ok, string Target, string Command, string Error) Match(string args, string current = "Virmar")
    {
        var split = new List<string>(args.Split(' '));
        var ok = AccountCharacters.TryMatchSwitchArguments(split, Names, current, out var target, out var command, out var error);
        return (ok, target, command, error);
    }

    [Test]
    public void ExactNameWithoutCommand()
    {
        var result = Match("woodford");

        Assert.That(result.Ok, Is.True, result.Error);
        Assert.That(result.Target, Is.EqualTo("Woodford"));
        Assert.That(result.Command, Is.Null);
    }

    [Test]
    public void LongestNameWithSpacesWins()
    {
        var result = Match("Bob Smith /re startmeta daily");

        Assert.That(result.Ok, Is.True, result.Error);
        Assert.That(result.Target, Is.EqualTo("Bob Smith"));
        Assert.That(result.Command, Is.EqualTo("/re startmeta daily"));
    }

    [Test]
    public void ShorterExactNameKeepsTheRestAsCommand()
    {
        var result = Match("bob /re help");

        Assert.That(result.Ok, Is.True, result.Error);
        Assert.That(result.Target, Is.EqualTo("Bob"));
        Assert.That(result.Command, Is.EqualTo("/re help"));
    }

    [Test]
    public void UniquePrefixMatches()
    {
        var result = Match("wood /vt start");

        Assert.That(result.Ok, Is.True, result.Error);
        Assert.That(result.Target, Is.EqualTo("Woodford"));
        Assert.That(result.Command, Is.EqualTo("/vt start"));
    }

    [Test]
    public void AmbiguousPrefixFails()
    {
        var result = Match("w");

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Error, Does.Contain("Woodford").And.Contain("Wanda"));
    }

    [Test]
    public void UnknownNameListsTheOtherCharacters()
    {
        var result = Match("Nobody");

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Error, Does.Contain("Woodford").And.Not.Contain("Virmar"));
    }

    [Test]
    public void CurrentCharacterFails()
    {
        var result = Match("virmar /re help");

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Target, Is.Null);
        Assert.That(result.Error, Does.Contain("Already logged in"));
    }

    [Test]
    public void NoArgumentsShowsUsage()
    {
        var ok = AccountCharacters.TryMatchSwitchArguments(new List<string>(), Names, "Virmar", out _, out _, out var error);

        Assert.That(ok, Is.False);
        Assert.That(error, Is.EqualTo(AccountCharacters.SwitchUsage));
    }

    [Test]
    public void SortedByNameIgnoresCase()
    {
        var sorted = AccountCharacters.SortedByName(new[]
        {
            new AccountCharacter { Name = "woodford" },
            new AccountCharacter { Name = "Bob" },
            new AccountCharacter { Name = "virmar" },
        });

        Assert.That(sorted.ConvertAll(c => c.Name), Is.EqualTo(new[] { "Bob", "virmar", "woodford" }));
    }
}
