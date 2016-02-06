using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using NUnit.Framework;
using RedoxExtensions.Mine;
using RedoxExtensions.Settings;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class MyUtilitiesTests
    {
        [SetUp]
        public void SetUp()
        {
            ActiveSettings.Init(CreateSampleUserSettings());
            MyUtilities.Init();
        }

        [TearDown]
        public void TearDown()
        {
            MyUtilities.Shutdown();
            ActiveSettings.Clear();
        }

        [TestCase("A1-C1", 0)]
        [TestCase("A2-C1", 1)]
        [TestCase("A2-C2", 1)]
        [TestCase("A3-C1", 2)]
        public void TestGetNpcSleepDelayFactor(string characterName, int expectedResult)
        {
            Assert.AreEqual(expectedResult, MyUtilities.GetNpcSleepDelayFactor(characterName));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestGetNpcSleepDelayFactorThrowsWhenUnknownCharacter()
        {
            MyUtilities.GetNpcSleepDelayFactor("Unknown Character");
        }

        [TestCase("A1-C1", 10)]
        [TestCase("A1-C2", 15)]
        [TestCase("A2-C1", 30)]
        [TestCase("A2-C2", 30)]
        [TestCase("A3-C1", 20)]
        public void TestGetFormationRange(string characterName, int expectedResult)
        {
            Assert.AreEqual(expectedResult, MyUtilities.GetFormationRange(characterName, "formation1"));
        }

        [Test]
        public void TestGetFormationRangeInputIsCaseInsensitive()
        {
            Assert.AreEqual(10, MyUtilities.GetFormationRange("A1-C1", "FORMATION1"));
        }

        [Test]
        public void TestGetFormationRangeUsesDefaultIfUnknownCharacter()
        {
            Assert.AreEqual(5, MyUtilities.GetFormationRange("Unknown Character", "formation1"));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestGetFormationRangeThrowsIfUnknownFormation()
        {
            MyUtilities.GetFormationRange("A1-C1", "Unknown Formation");
        }

        [TestCase("A1-C1")]
        [TestCase("A3-C1")]
        public void TestEnableLootForFormation(string characterName)
        {
            Assert.IsTrue(MyUtilities.EnableLootForFormation(characterName, "formation1"));
        }

        [TestCase("A1-C1", MyMainProfiles.Normal, "Normal")]
        [TestCase("A1-C1", MyMainProfiles.CharacterSpecificDefault, "Normal")]
        [TestCase("A3-C1", MyMainProfiles.Normal, "Normal")]
        [TestCase("A3-C1", MyMainProfiles.CharacterSpecificDefault, "Support")]
        public void TestLookupProfileName(string characterName, MyMainProfiles profile, string expectedResult)
        {
            var result = MyUtilities.LookupProfileName(characterName, profile);
            Assert.AreEqual(expectedResult, result);
        }

        #region Helpers

        private static UserSettings CreateSampleUserSettings()
        {
            var settings = new UserSettings();
            settings.CharactersGroupedByAccount = new List<List<string>>();
            settings.CharactersGroupedByAccount.Add(new List<string>(new []{"A1-C1", "A1-C2"}));
            settings.CharactersGroupedByAccount.Add(new List<string>(new[] { "A2-C1", "A2-C2" }));
            settings.CharactersGroupedByAccount.Add(new List<string>(new[] { "A3-C1"}));

            settings.Formations = new Dictionary<string, Formation>();
            var formation1 = new Formation();
            formation1.RangeDefault = 5;
            formation1.RangeTable = new Dictionary<string, int>();
            formation1.RangeTable.Add("A1-C1", 10);
            formation1.RangeTable.Add("A1-C2", 15);

            formation1.RangeTable.Add("A2-C1", 30);
            formation1.RangeTable.Add("A2-C2", 30);

            formation1.RangeTable.Add("A3-C1", 20);

            formation1.Looters = new HashSet<string>();
            formation1.Looters.Add("A1-C1");
            formation1.Looters.Add("A3-C1");

            settings.Formations.Add("formation1", formation1);

            // Setup Profiles
            settings.VTProfiles = new VTProfiles();
            settings.VTProfiles.Main = new VTMain();
            settings.VTProfiles.Main.Default = "Normal";
            settings.VTProfiles.Main.CharacterDefaults = new Dictionary<string, string>();
            settings.VTProfiles.Main.CharacterDefaults.Add("A3-C1", "Support");
            return settings;
        }

        #endregion
    }
}
