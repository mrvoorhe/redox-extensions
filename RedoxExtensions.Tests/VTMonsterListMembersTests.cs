using System;
using System.IO;
using System.Reflection;

using NUnit.Framework;

using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Tests;

[TestFixture]
public class VTMonsterListMembersTests
{
    // The assembly is uTank2 but the file is utank2-i.dll, so the test host can't find it by name.
    private static readonly Assembly VTank = Assembly.LoadFrom(Path.Combine(TestContext.CurrentContext.TestDirectory, "utank2-i.dll"));

    [Test]
    public void ResolvesAgainstTheCompileTimeVTank()
    {
        var members = VTMonsterListMembers.Resolve(VTank);

        Assert.That(members.State.IsStatic, Is.True);
        Assert.That(members.Entry.DeclaringType, Is.EqualTo(members.Monsters.FieldType));
        Assert.That(members.Upsert.GetParameters()[1].ParameterType, Is.EqualTo(members.Entry));
        Assert.That(members.Remove, Is.Not.SameAs(members.Upsert));
    }

    [Test]
    public void EntryNameAndPriorityAreTheFirstTwoColumns()
    {
        var members = VTMonsterListMembers.Resolve(VTank);
        var columns = MonsterTableColumns();
        Assert.That(columns[0], Is.EqualTo("MonsterName"));
        Assert.That(columns[1], Is.EqualTo("AttackPriority"));

        var table = Activator.CreateInstance(VTank.GetType("bd"), new object[] { columns });
        var row = (System.Collections.IList)Activator.CreateInstance(VTank.GetType("cw"), table);
        var cell = VTank.GetType("gy");
        for (var i = 0; i < columns.Length; i++)
        {
            object value = i switch
            {
                0 => "Drudge Prowler",
                1 => 3,
                >= 4 and <= 17 => false,
                _ => 0,
            };
            row[i] = Activator.CreateInstance(cell, value);
        }

        var fromRow = members.Entry.GetMethod("a", BindingFlags.Public | BindingFlags.Static, null, new[] { row.GetType() }, null);
        var entry = fromRow.Invoke(null, new object[] { row });

        Assert.That(members.EntryName.GetValue(entry), Is.EqualTo("Drudge Prowler"));
        Assert.That(members.EntryPriority.GetValue(entry), Is.EqualTo(3));
        Assert.That(members.EntryIsDefault.GetValue(entry), Is.False);
    }

    [Test]
    public void AnotherBuildFailsToResolve()
    {
        var notVTank = typeof(VTMonsterListMembersTests).Assembly;
        Assert.That(() => VTMonsterListMembers.Resolve(notVTank), Throws.InstanceOf<MissingMemberException>());
    }

    private static string[] MonsterTableColumns()
    {
        using var stream = VTank.GetManifestResourceStream("uTank2.Resources.defaultsettingstemplate.usd");
        Assert.That(stream, Is.Not.Null);
        using var reader = new StreamReader(stream);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line != "MyMonsters")
                continue;

            var columns = new string[int.Parse(reader.ReadLine())];
            for (var i = 0; i < columns.Length; i++)
                columns[i] = reader.ReadLine();
            return columns;
        }

        Assert.Fail("No MyMonsters table in VTank's default settings");
        return null;
    }
}
