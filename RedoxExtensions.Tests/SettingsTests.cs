using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using RedoxExtensions.Settings;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class SettingsTests
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            ActiveSettings.Clear();
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
            ClassicAssert.AreEqual("Redox", result[0][0]);
            ClassicAssert.AreEqual("Virmar", result[0][1]);

            ClassicAssert.IsTrue(result[1].Count >= 2);
            ClassicAssert.AreEqual("Kreap", result[1][0]);
            ClassicAssert.AreEqual("Virmar Jr", result[1][1]);
        }

        // is the leader, so keep him low so that he doesn't get stuck wasting time trying to hit an out-of-range
        [TestCase("Redox", "outside", 12)]
        [TestCase("Kreap", "outside", 20)]  // A looter, so keep him a bit lower so he has time to loot
        [TestCase("Rockdown Guy", "outside", 60)]  // Support / lure
        [TestCase("Rathion", "outside", 40)]  // Set archers higher, to get a head start.
        [TestCase("Mini Bonsai", "outside", 40)]  // Set archers higher, to get a head start.
        [TestCase("Mrguy", "outside", 30)]  // Kill focused mage
        [TestCase("Zikka", "outside", 60)]  // Support / lure
        [TestCase("Context Bound", "outside", 40)]  // Archer
        [TestCase("New Riff", "outside", 30)]  // Kill mage
        [TestCase("Virmar", "outside", 10)]  // Melee, keep close
        [TestCase("Virmar Jr", "outside", 10)]  // Melee, keep close
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

        [TestCase("Rockdown Guy", "Support")]
        [TestCase("Zikka", "Support")]
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
}
