using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace RedoxExtensions.Tests;

[TestFixture]
public class WrapperUtilitiesTests
{
    [Test]
    public void TestDllFilePathToPdbFilePath()
    {
        var result = Wrapper.WrapperUtilities.DllFilePathToPdbFilePath(@"C:\some.dll");
        ClassicAssert.AreEqual(@"C:\some.pdb", result);
    }

    private string _root;

    [SetUp]
    public void SetUp()
    {
        this._root = Path.Combine(Path.GetTempPath(), "RETmpDllsTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this._root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this._root))
            Directory.Delete(this._root, true);
    }

    private string MakeCopyFolder(string name, DateTime createdUtc)
    {
        var folder = Path.Combine(this._root, name);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, $"RedoxExtensions-{name}.dll"), "dll");
        File.WriteAllText(Path.Combine(folder, "RedoxLib.dll"), "dll");
        File.WriteAllText(Path.Combine(folder, "settings.json"), "{}");
        Directory.SetCreationTimeUtc(folder, createdUtc);
        return folder;
    }

    [Test]
    public void CleanupTmpDllFoldersDeletesOldUnusedFolders()
    {
        var now = DateTime.UtcNow;
        var old1 = this.MakeCopyFolder("old1", now.AddDays(-30));
        var old2 = this.MakeCopyFolder("old2", now.AddHours(-1));

        var deleted = Wrapper.WrapperUtilities.CleanupTmpDllFolders(this._root, now, TimeSpan.FromMinutes(10));

        ClassicAssert.AreEqual(2, deleted);
        ClassicAssert.IsFalse(Directory.Exists(old1));
        ClassicAssert.IsFalse(Directory.Exists(old2));
    }

    [Test]
    public void CleanupTmpDllFoldersKeepsFoldersInUseWhole()
    {
        var now = DateTime.UtcNow;
        var inUse = this.MakeCopyFolder("inuse", now.AddDays(-1));

        // A client that has the dll loaded holds it open like this.
        using (new FileStream(Path.Combine(inUse, "RedoxLib.dll"), FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var deleted = Wrapper.WrapperUtilities.CleanupTmpDllFolders(this._root, now, TimeSpan.FromMinutes(10));
            ClassicAssert.AreEqual(0, deleted);
        }

        // Nothing in it was touched, including the settings the running plugin reads from there.
        ClassicAssert.AreEqual(3, Directory.GetFiles(inUse).Length);
    }

    [Test]
    public void CleanupTmpDllFoldersKeepsYoungFolders()
    {
        var now = DateTime.UtcNow;
        var young = this.MakeCopyFolder("young", now.AddMinutes(-2));

        var deleted = Wrapper.WrapperUtilities.CleanupTmpDllFolders(this._root, now, TimeSpan.FromMinutes(10));

        ClassicAssert.AreEqual(0, deleted);
        ClassicAssert.IsTrue(Directory.Exists(young));
    }

    [Test]
    public void CleanupTmpDllFoldersIgnoresMissingRoot()
    {
        var deleted = Wrapper.WrapperUtilities.CleanupTmpDllFolders(Path.Combine(this._root, "missing"), DateTime.UtcNow, TimeSpan.FromMinutes(10));
        ClassicAssert.AreEqual(0, deleted);
    }
}
