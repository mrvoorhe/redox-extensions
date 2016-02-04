using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            MyUtilities.Init(CreateSampleUserSettings());
        }

        [TearDown]
        public void TearDown()
        {
            MyUtilities.Shutdown();
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

        private static UserSettings CreateSampleUserSettings()
        {
            var settings = new UserSettings();
            settings.CharactersGroupedByAccount = new List<List<string>>();
            settings.CharactersGroupedByAccount.Add(new List<string>(new []{"A1-C1", "A1-C2"}));
            settings.CharactersGroupedByAccount.Add(new List<string>(new[] { "A2-C1", "A2-C2" }));
            settings.CharactersGroupedByAccount.Add(new List<string>(new[] { "A3-C1"}));
            return settings;
        }
    }
}
