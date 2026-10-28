using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Framework.Legacy;

using RedoxExtensions.Commands;

namespace RedoxExtensions.Tests.CommandTests;

public abstract class AbstractForeignCommandTests
{
    protected abstract CommandChannel ExpectedChannel { get; }

    protected virtual bool UsesChannelTag => false;

    protected virtual object ChannelTag => null;

    protected virtual bool UsesExplicitTargettingTag => false;

    protected virtual object ExplicitTargettingTag => null;

    #region From Self

    [Test]
    public virtual void Test_TryParse_WhenFromSelf_NoArguments()
    {
        var text = this.CreateFromSelfSampleText("test");

        bool isCommand = CommandHelpers.TryParse(text, out var result);

        ClassicAssert.IsTrue(isCommand);

        ClassicAssert.AreEqual("test", result.Name);
        ClassicAssert.AreEqual(0, result.Arguments.Count);
        ClassicAssert.AreEqual(text, result.RawValue);

        this.CommonFromSelfCommandAsserts(result);
    }

    [Test]
    public virtual void Test_TryParse_WhenFromSelf_SingleArgument()
    {
        var text = this.CreateFromSelfSampleText("test", "one");

        bool isCommand = CommandHelpers.TryParse(text, out var result);

        ClassicAssert.IsTrue(isCommand);

        ClassicAssert.AreEqual("test", result.Name);
        ClassicAssert.AreEqual(1, result.Arguments.Count);
        ClassicAssert.AreEqual("one", result.Arguments[0]);
        ClassicAssert.AreEqual(text, result.RawValue);

        this.CommonFromSelfCommandAsserts(result);
    }

    [Test]
    public virtual void Test_TryParse_WhenFromSelf_ManyArguments()
    {
        var text = this.CreateFromSelfSampleText("test", "one", "two", "three");

        bool isCommand = CommandHelpers.TryParse(text, out var result);

        ClassicAssert.IsTrue(isCommand);

        ClassicAssert.AreEqual("test", result.Name);
        ClassicAssert.AreEqual(3, result.Arguments.Count);
        ClassicAssert.AreEqual("one", result.Arguments[0]);
        ClassicAssert.AreEqual("two", result.Arguments[1]);
        ClassicAssert.AreEqual("three", result.Arguments[2]);
        ClassicAssert.AreEqual(text, result.RawValue);

        this.CommonFromSelfCommandAsserts(result);
    }

    #endregion

    #region From Other

    [Test]
    public void Test_TryParse_WhenFromOther_NoArguments()
    {
        var text = this.CreateFromOtherSampleText("Big Ted", "test");

        bool isCommand = CommandHelpers.TryParse(text, out var result);

        ClassicAssert.IsTrue(isCommand);

        ClassicAssert.AreEqual("test", result.Name);
        ClassicAssert.AreEqual(0, result.Arguments.Count);
        ClassicAssert.AreEqual(text, result.RawValue);

        this.CommonFromOtherCommandAsserts(result, "Big Ted");
    }

    [Test]
    public void Test_TryParse_WhenFromOther_SingleArgument()
    {
        var text = this.CreateFromOtherSampleText("Big Ted", "test", "one");

        bool isCommand = CommandHelpers.TryParse(text, out var result);

        ClassicAssert.IsTrue(isCommand);

        ClassicAssert.AreEqual("test", result.Name);
        ClassicAssert.AreEqual(1, result.Arguments.Count);
        ClassicAssert.AreEqual("one", result.Arguments[0]);
        ClassicAssert.AreEqual(text, result.RawValue);

        this.CommonFromOtherCommandAsserts(result, "Big Ted");
    }

    [Test]
    public void Test_TryParse_WhenFromOther_ManyArguments()
    {
        var text = this.CreateFromOtherSampleText("Big Ted", "test", "one", "two", "three");

        bool isCommand = CommandHelpers.TryParse(text, out var result);

        ClassicAssert.IsTrue(isCommand);

        ClassicAssert.AreEqual("test", result.Name);
        ClassicAssert.AreEqual(3, result.Arguments.Count);
        ClassicAssert.AreEqual("one", result.Arguments[0]);
        ClassicAssert.AreEqual("two", result.Arguments[1]);
        ClassicAssert.AreEqual("three", result.Arguments[2]);
        ClassicAssert.AreEqual(text, result.RawValue);

        this.CommonFromOtherCommandAsserts(result, "Big Ted");
    }

    #endregion

    protected abstract string CreateFromSelfSampleText(string cmdName);
    protected abstract string CreateFromSelfSampleText(string cmdName, params object[] args);

    protected abstract string CreateFromOtherSampleText(string sourceName, string cmdName);
    protected abstract string CreateFromOtherSampleText(string sourceName, string cmdName, params object[] args);

    /// <summary>
    /// Performs assertions that are common to all commands of this type
    /// </summary>
    /// <param name="result"></param>
    private void CommonFromSelfCommandAsserts(ICommand result)
    {
        ClassicAssert.AreEqual(CommandHelpers.DefaultYouValue, result.SourceCharacter);
        ClassicAssert.IsTrue(result.FromSelf);
        ClassicAssert.AreEqual(this.ExpectedChannel, result.Channel);
        ClassicAssert.AreEqual(this.UsesChannelTag, result.UsesChannelTag);
        ClassicAssert.AreEqual(this.ChannelTag, result.ChannelTag);

        ClassicAssert.AreEqual(CommandType.Foreign, result.CommandType);
        ClassicAssert.AreEqual(TargetType.All, result.CommandTargetType);
        ClassicAssert.AreEqual(this.UsesExplicitTargettingTag, result.UsesExplicitTargettingTag);
        ClassicAssert.AreEqual(this.ExplicitTargettingTag, result.ExplicitTargettingTag);
    }

    private void CommonFromOtherCommandAsserts(ICommand result, string sourceName)
    {
        ClassicAssert.AreEqual(sourceName, result.SourceCharacter);
        ClassicAssert.IsFalse(result.FromSelf);
        ClassicAssert.AreEqual(this.ExpectedChannel, result.Channel);
        ClassicAssert.AreEqual(this.UsesChannelTag, result.UsesChannelTag);
        ClassicAssert.AreEqual(this.ChannelTag, result.ChannelTag);

        ClassicAssert.AreEqual(CommandType.Foreign, result.CommandType);
        ClassicAssert.AreEqual(TargetType.All, result.CommandTargetType);
        ClassicAssert.AreEqual(this.UsesExplicitTargettingTag, result.UsesExplicitTargettingTag);
        ClassicAssert.AreEqual(this.ExplicitTargettingTag, result.ExplicitTargettingTag);
    }
}
