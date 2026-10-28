using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using NUnit.Framework.Legacy;

using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Tests;

[TestFixture]
public class VTStateScopeTests
{
    [Test]
    public void TestAddOptionIfGoingToChangeWhenShouldBeAddedToChangeList()
    {
        var optionsToChange = new List<string>();

        VTStateScope.AddOptionIfGoingToChange(optionsToChange, VTStateOptions.DontChange, VTStateOptions.Nav, VTStateOptions.Nav, VTOptionNames.EnableNav);

        ClassicAssert.AreEqual(1, optionsToChange.Count);
        ClassicAssert.AreEqual(VTOptionNames.EnableNav, optionsToChange[0]);
    }

    [Test]
    public void TestAddOptionIfGoingToChangeWhenNotAddedDueToNotCheckingSuppliedState()
    {
        var optionsToChange = new List<string>();

        VTStateScope.AddOptionIfGoingToChange(optionsToChange, VTStateOptions.DontChange, VTStateOptions.Nav, VTStateOptions.Combat, VTOptionNames.EnableNav);

        ClassicAssert.AreEqual(0, optionsToChange.Count);
    }
}
