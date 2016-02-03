using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using NUnit.Framework;

using RedoxExtensions.Mine;
using RedoxExtensions.VirindiInterop;

namespace Characher1Tools.Tests
{
    /// <summary>
    /// This suite is less a code unit test and more for making sure all of my primary profiles
    /// for each character exist in my VT directory
    /// </summary>
    [TestFixture]
    public class VerifyMainProfiles
    {
        [TestFixtureSetUp]
        public void TestSetup()
        {
            // TODO : Refactor to be account independent
            //var unitTestAssemblyLocation = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
            //var rootDecalPluginsCheckoutDir = Path.Combine(unitTestAssemblyLocation, "..\\..\\..\\..");
            //var myVTankProfiles = Path.Combine(rootDecalPluginsCheckoutDir, "MyPluginFiles\\VirindiTank");
            //VTUtilities.VTankDirectory = Path.GetFullPath(myVTankProfiles);
        }

        [Test]
        [Ignore("Needs to be refactored to be account independent or remove entirely if no longer useful")]
        public void VerifyNormalProfiles()
        {
            // TODO : Refactor to be account independent or remove entirely
            var expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Normal.ToString(), "Characher1", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Normal.ToString(), "Characher2", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Normal.ToString(), "Characher7", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Normal.ToString(), "Characher4", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Normal.ToString(), "Characher5", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Normal.ToString(), "Characher6", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Normal.ToString(), "Characher8", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));
        }

        [Test]
        [Ignore("Needs to be refactored to be account independent or remove entirely if no longer useful")]
        public void VerifyLightProfiles()
        {
            // TODO : Refactor to be account independent or remove entirely
            var expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Light.ToString(), "Characher1", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Light.ToString(), "Characher2", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Light.ToString(), "Characher7", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Light.ToString(), "Characher4", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Light.ToString(), "Characher5", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Light.ToString(), "Characher6", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Light.ToString(), "Characher8", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));
        }

        [Test]
        [Ignore("Needs to be refactored to be account independent or remove entirely if no longer useful")]
        public void VerifySupportProfiles()
        {
            // TODO : Refactor to be account independent or remove entirely
            var expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Support.ToString(), "Characher2", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Support.ToString(), "Characher5", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Support.ToString(), "Characher8", "Frostfell");
            Assert.IsTrue(File.Exists(expectedFilePath));
        }

        [Test]
        [Ignore("Needs to be refactored to be account independent or remove entirely if no longer useful")]
        public void VerifyNotSupportedSupportProfiles()
        {
            // TODO : Refactor to be account independent or remove entirely
            var expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Support.ToString(), "Characher1", "Frostfell");
            Assert.IsFalse(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Support.ToString(), "Characher7", "Frostfell");
            Assert.IsFalse(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Support.ToString(), "Characher4", "Frostfell");
            Assert.IsFalse(File.Exists(expectedFilePath));

            expectedFilePath = VTUtilities.GetCharacterSpecificProfileFullFilePath(MyMainProfiles.Support.ToString(), "Characher6", "Frostfell");
            Assert.IsFalse(File.Exists(expectedFilePath));
        }
    }
}
