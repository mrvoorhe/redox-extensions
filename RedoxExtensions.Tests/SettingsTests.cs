using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RedoxExtensions.Settings;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class SettingsTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            ActiveSettings.Clear();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            ActiveSettings.Clear();
        }

        [Test]
        public void LoadAndCheckCharactersByAccount()
        {
            var result = ActiveSettings.Instance.CharactersGroupedByAccount;

            Assert.IsTrue(result.Count >= 2);

            Assert.IsTrue(result[0].Count >= 2);
            Assert.AreEqual("Redox", result[0][0]);
            Assert.AreEqual("Virmar", result[0][1]);

            Assert.IsTrue(result[1].Count >= 2);
            Assert.AreEqual("Kreap", result[1][0]);
            Assert.AreEqual("Virmar Jr", result[1][1]);
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
            Assert.AreEqual(expectedRange, formations[formationName].RangeTable[characterName]);
        }

        [Test]
        public void LoadAndCheckPullLegendaryKeysMetaProfile()
        {
            Assert.AreEqual("LegendKeys", ActiveSettings.Instance.VTProfiles.Meta.PullLegendaryKeys);
        }

        [Test]
        public void LoadAndCheckLegendaryChestPullsLootProfile()
        {
            Assert.AreEqual("LootSnobV4LegChests", ActiveSettings.Instance.VTProfiles.Loot.LegendaryChestPulls);
        }

        [Test]
        public void LoadAndCheckDefaultLootProfile()
        {
            Assert.AreEqual("LootSnobV4LegChests", ActiveSettings.Instance.VTProfiles.Loot.Default);
        }

        [Test]
        public void LoadAndCheckDefaultMainProfile()
        {
            Assert.AreEqual("Normal", ActiveSettings.Instance.VTProfiles.Main.Default);
        }

        [TestCase("Rockdown Guy", "Support")]
        [TestCase("Zikka", "Support")]
        public void LoadAndCheckCharacterDefaultMainProfile(string characterName, string expectedResult)
        {
            Assert.AreEqual(expectedResult, ActiveSettings.Instance.VTProfiles.Main.CharacterDefaults[characterName]);
        }

        [Test]
        public void LoadAndCheckDebugLevel()
        {
            Assert.AreEqual(Diagnostics.DebugLevel.None, ActiveSettings.Instance.DebugLevel);
        }
    }
}
