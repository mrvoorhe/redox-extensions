using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework;

using RedoxExtensions.Listeners.Monitors;
using RedoxExtensions.Tests.TestUtilities;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class MetaChainFileTests : TemporaryDirectoryPerTestTestSuite
    {
        [SetUp]
        public override void TestSetup()
        {
            base.TestSetup();

            MetaChainRunner.ChainDirectory = this.TestTemporaryDirectory;
        }

        [Test]
        public void FindChainFile_CharacterSpecificWins()
        {
            this.CreateFile("MetaChainList-Dailies.txt");
            var expected = this.CreateFile("MetaChainList-Redox-Dailies.txt");

            var result = MetaChainRunner.FindChainFile("Redox", "Dailies");

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindChainFile_FallsBackToGeneric()
        {
            var expected = this.CreateFile("MetaChainList-Dailies.txt");

            var result = MetaChainRunner.FindChainFile("Redox", "Dailies");

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindChainFile_NoMatch_ReturnsNull()
        {
            this.CreateFile("MetaChainList-Other.txt");

            var result = MetaChainRunner.FindChainFile("Redox", "Dailies");

            Assert.IsNull(result);
        }

        [Test]
        public void GetCurrentFilePath()
        {
            var result = MetaChainRunner.GetCurrentFilePath("Redox");

            Assert.AreEqual(Path.Combine(this.TestTemporaryDirectory, "MetaChainList-Redox-Current.txt"), result);
        }

        [Test]
        public void ReadChain_SkipsBlankAndCommentLinesAndTrims()
        {
            var path = this.CreateFile("MetaChainList-Dailies.txt");
            File.WriteAllLines(path, new[] { "  HowardTheMad  ", "", "# a comment", "   ", "Gauntlet" });

            var result = MetaChainRunner.ReadChain(path);

            CollectionAssert.AreEqual(new[] { "HowardTheMad", "Gauntlet" }, result);
        }

        [Test]
        public void WriteChain_RoundTrips()
        {
            var path = Path.Combine(this.TestTemporaryDirectory, "MetaChainList-Redox-Current.txt");
            var metas = new List<string> { "One", "Two", "Three" };

            MetaChainRunner.WriteChain(path, metas);
            var result = MetaChainRunner.ReadChain(path);

            CollectionAssert.AreEqual(metas, result);
        }
    }
}
