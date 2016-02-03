using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class SettingsTests
    {
        [Test]
        public void LoadAndCheckCharactersByAccount()
        {
            var result = Settings.Main.Instance.User.CharactersGroupedByAccount;

            Assert.IsTrue(result.Count >= 2);

            Assert.IsTrue(result[0].Count >= 2);
            Assert.AreEqual("Redox", result[0][0]);
            Assert.AreEqual("Virmar", result[0][1]);

            Assert.IsTrue(result[1].Count >= 2);
            Assert.AreEqual("Kreap", result[1][0]);
            Assert.AreEqual("Virmar Jr", result[1][1]);
        }
    }
}
