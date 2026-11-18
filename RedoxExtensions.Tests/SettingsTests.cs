using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using RedoxExtensions.Settings;

namespace RedoxExtensions.Tests;

[TestFixture]
public class SettingsTests
{
    // Self-contained settings, so these tests don't depend on the developer's own mysettings.json.
    private const string FixtureSettingsJson = @"{
  ""DebugLevel"": ""None"",
  ""CharactersGroupedByAccount"": [
    [""Aldric Fenn"", ""Brannock Vale""],
    [""Mirabel Tosk"", ""Tamsin Orrel""]
  ],
  ""VTProfiles"": {
    ""Meta"": { ""PullLegendaryKeys"": ""LegendKeys"" },
    ""Loot"": { ""LegendaryChestPulls"": ""LootSnobV4LegChests"", ""Default"": ""LootSnobV4LegChests"" },
    ""Main"": {
      ""Default"": ""Normal"",
      ""CharacterDefaults"": { ""Garrick Holt"": ""Support"", ""Wren Ashby"": ""Support"" }
    }
  },
  ""Formations"": {
    ""outside"": {
      ""RangeDefault"": 10,
      ""RangeTable"": {
        ""Aldric Fenn"": 12, ""Mirabel Tosk"": 20, ""Garrick Holt"": 60, ""Sorrel Quin"": 40, ""Pip Larkin"": 40,
        ""Odo Penhallow"": 30, ""Wren Ashby"": 60, ""Lysander Moor"": 40, ""Isolde Crane"": 30, ""Brannock Vale"": 10, ""Tamsin Orrel"": 10
      }
    }
  }
}";

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        ActiveSettings.Init(JsonConvert.DeserializeObject<UserSettings>(FixtureSettingsJson));
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        ActiveSettings.Clear();
    }

    [Test]
    public void LoadAndCheckCharactersByAccount()
    {
        var result = ActiveSettings.Instance.CharactersGroupedByAccount;

        ClassicAssert.IsTrue(result.Count >= 2);

        ClassicAssert.IsTrue(result[0].Count >= 2);
        ClassicAssert.AreEqual("Aldric Fenn", result[0][0]);
        ClassicAssert.AreEqual("Brannock Vale", result[0][1]);

        ClassicAssert.IsTrue(result[1].Count >= 2);
        ClassicAssert.AreEqual("Mirabel Tosk", result[1][0]);
        ClassicAssert.AreEqual("Tamsin Orrel", result[1][1]);
    }

    // is the leader, so keep him low so that he doesn't get stuck wasting time trying to hit an out-of-range
    [TestCase("Aldric Fenn", "outside", 12)]
    [TestCase("Mirabel Tosk", "outside", 20)]  // A looter, so keep him a bit lower so he has time to loot
    [TestCase("Garrick Holt", "outside", 60)]  // Support / lure
    [TestCase("Sorrel Quin", "outside", 40)]  // Set archers higher, to get a head start.
    [TestCase("Pip Larkin", "outside", 40)]  // Set archers higher, to get a head start.
    [TestCase("Odo Penhallow", "outside", 30)]  // Kill focused mage
    [TestCase("Wren Ashby", "outside", 60)]  // Support / lure
    [TestCase("Lysander Moor", "outside", 40)]  // Archer
    [TestCase("Isolde Crane", "outside", 30)]  // Kill mage
    [TestCase("Brannock Vale", "outside", 10)]  // Melee, keep close
    [TestCase("Tamsin Orrel", "outside", 10)]  // Melee, keep close
    public void LoadAndCheckFormationRanges(string characterName, string formationName, int expectedRange)
    {
        var formations = ActiveSettings.Instance.Formations;
        ClassicAssert.AreEqual(expectedRange, formations[formationName].RangeTable[characterName]);
    }

    [Test]
    public void LoadAndCheckPullLegendaryKeysMetaProfile()
    {
        ClassicAssert.AreEqual("LegendKeys", ActiveSettings.Instance.VTProfiles.Meta.PullLegendaryKeys);
    }

    [Test]
    public void LoadAndCheckLegendaryChestPullsLootProfile()
    {
        ClassicAssert.AreEqual("LootSnobV4LegChests", ActiveSettings.Instance.VTProfiles.Loot.LegendaryChestPulls);
    }

    [Test]
    public void LoadAndCheckDefaultLootProfile()
    {
        ClassicAssert.AreEqual("LootSnobV4LegChests", ActiveSettings.Instance.VTProfiles.Loot.Default);
    }

    [Test]
    public void LoadAndCheckDefaultMainProfile()
    {
        ClassicAssert.AreEqual("Normal", ActiveSettings.Instance.VTProfiles.Main.Default);
    }

    [TestCase("Garrick Holt", "Support")]
    [TestCase("Wren Ashby", "Support")]
    public void LoadAndCheckCharacterDefaultMainProfile(string characterName, string expectedResult)
    {
        ClassicAssert.AreEqual(expectedResult, ActiveSettings.Instance.VTProfiles.Main.CharacterDefaults[characterName]);
    }

    [Test]
    public void LoadAndCheckDebugLevel()
    {
        ClassicAssert.AreEqual(Diagnostics.DebugLevel.None, ActiveSettings.Instance.DebugLevel);
    }
}
