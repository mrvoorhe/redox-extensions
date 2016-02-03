using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class VTStateScopeTests
    {
        [Test]
        public void TestAddOptionIfGoingToChangeWhenShouldBeAddedToChangeList()
        {
            List<string> optionsToChange = new List<string>();

            VTStateScope.AddOptionIfGoingToChange(optionsToChange, VTStateOptions.DontChange, VTStateOptions.Nav, VTStateOptions.Nav, VTOptionNames.EnableNav);

            Assert.AreEqual(1, optionsToChange.Count);
            Assert.AreEqual(VTOptionNames.EnableNav, optionsToChange[0]);
        }

        [Test]
        public void TestAddOptionIfGoingToChangeWhenNotAddedDueToNotCheckingSuppliedState()
        {
            List<string> optionsToChange = new List<string>();

            VTStateScope.AddOptionIfGoingToChange(optionsToChange, VTStateOptions.DontChange, VTStateOptions.Nav, VTStateOptions.Combat, VTOptionNames.EnableNav);

            Assert.AreEqual(0, optionsToChange.Count);
        }
    }
}
