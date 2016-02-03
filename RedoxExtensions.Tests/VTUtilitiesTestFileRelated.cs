using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using NUnit.Framework;

using RedoxExtensions.Tests.TestUtilities;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class VTUtilitiesTestFileRelated : TemporaryDirectoryPerTestTestSuite
    {
        [SetUp]
        public override void TestSetup()
        {
            base.TestSetup();

            VTUtilities.VTankDirectory = this.TestTemporaryDirectory;
        }

        [Test]
        public void GetFollowNavForCharacter_SingleWordName()
        {
            this.CreateFile("FollowRedox.nav");

            // Test
            var result = VTUtilities.GetFollowNavForCharacter("Redox");

            Assert.AreEqual("FollowRedox.nav", result);
        }

        [Test]
        public void GetFollowNavForCharacter_MultiWord_ExpectingFullMatchMinusSpaces()
        {
            this.CreateFile("FollowRockdownGuy.nav");

            // Test
            var result = VTUtilities.GetFollowNavForCharacter("Rockdown Guy");

            Assert.AreEqual("FollowRockdownGuy.nav", result);
        }

        [Test]
        public void GetFollowNavForCharacter_MultiWord_ExpectingSubsetOfNameMatch()
        {
            this.CreateFile("FollowGuy.nav");

            // Test
            var result = VTUtilities.GetFollowNavForCharacter("Rockdown Guy");

            Assert.AreEqual("FollowGuy.nav", result);
        }
    }
}
