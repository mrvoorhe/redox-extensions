using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using RedoxExtensions.Commands;

namespace RedoxExtensions.Tests.CommandTests
{
    public abstract class AbstractForeignCommandTests
    {
        protected abstract CommandChannel ExpectedChannel { get; }

        protected virtual bool UsesChannelTag
        {
            get
            {
                return false;
            }
        }

        protected virtual object ChannelTag
        {
            get
            {
                return null;
            }
        }

        protected virtual bool UsesExplicitTargettingTag
        {
            get
            {
                return false;
            }
        }

        protected virtual object ExplicitTargettingTag
        {
            get
            {
                return null;
            }
        }

        #region From Self

        [Test]
        public virtual void Test_TryParse_WhenFromSelf_NoArguments()
        {
            var text = this.CreateFromSelfSampleText("test");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsTrue(isCommand);

            Assert.AreEqual("test", result.Name);
            Assert.AreEqual(0, result.Arguments.Count);
            Assert.AreEqual(text, result.RawValue);

            this.CommonFromSelfCommandAsserts(result);
        }

        [Test]
        public virtual void Test_TryParse_WhenFromSelf_SingleArgument()
        {
            var text = this.CreateFromSelfSampleText("test", "one");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsTrue(isCommand);

            Assert.AreEqual("test", result.Name);
            Assert.AreEqual(1, result.Arguments.Count);
            Assert.AreEqual("one", result.Arguments[0]);
            Assert.AreEqual(text, result.RawValue);

            this.CommonFromSelfCommandAsserts(result);
        }

        [Test]
        public virtual void Test_TryParse_WhenFromSelf_ManyArguments()
        {
            var text = this.CreateFromSelfSampleText("test", "one", "two", "three");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsTrue(isCommand);

            Assert.AreEqual("test", result.Name);
            Assert.AreEqual(3, result.Arguments.Count);
            Assert.AreEqual("one", result.Arguments[0]);
            Assert.AreEqual("two", result.Arguments[1]);
            Assert.AreEqual("three", result.Arguments[2]);
            Assert.AreEqual(text, result.RawValue);

            this.CommonFromSelfCommandAsserts(result);
        }

        #endregion

        #region From Other

        [Test]
        public void Test_TryParse_WhenFromOther_NoArguments()
        {
            var text = this.CreateFromOtherSampleText("Big Ted", "test");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsTrue(isCommand);

            Assert.AreEqual("test", result.Name);
            Assert.AreEqual(0, result.Arguments.Count);
            Assert.AreEqual(text, result.RawValue);

            this.CommonFromOtherCommandAsserts(result, "Big Ted");
        }

        [Test]
        public void Test_TryParse_WhenFromOther_SingleArgument()
        {
            var text = this.CreateFromOtherSampleText("Big Ted", "test", "one");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsTrue(isCommand);

            Assert.AreEqual("test", result.Name);
            Assert.AreEqual(1, result.Arguments.Count);
            Assert.AreEqual("one", result.Arguments[0]);
            Assert.AreEqual(text, result.RawValue);

            this.CommonFromOtherCommandAsserts(result, "Big Ted");
        }

        [Test]
        public void Test_TryParse_WhenFromOther_ManyArguments()
        {
            var text = this.CreateFromOtherSampleText("Big Ted", "test", "one", "two", "three");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsTrue(isCommand);

            Assert.AreEqual("test", result.Name);
            Assert.AreEqual(3, result.Arguments.Count);
            Assert.AreEqual("one", result.Arguments[0]);
            Assert.AreEqual("two", result.Arguments[1]);
            Assert.AreEqual("three", result.Arguments[2]);
            Assert.AreEqual(text, result.RawValue);

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
            Assert.AreEqual(CommandHelpers.DefaultYouValue, result.SourceCharacter);
            Assert.IsTrue(result.FromSelf);
            Assert.AreEqual(this.ExpectedChannel, result.Channel);
            Assert.AreEqual(this.UsesChannelTag, result.UsesChannelTag);
            Assert.AreEqual(this.ChannelTag, result.ChannelTag);

            Assert.AreEqual(CommandType.Foreign, result.CommandType);
            Assert.AreEqual(TargetType.All, result.CommandTargetType);
            Assert.AreEqual(this.UsesExplicitTargettingTag, result.UsesExplicitTargettingTag);
            Assert.AreEqual(this.ExplicitTargettingTag, result.ExplicitTargettingTag);
        }

        private void CommonFromOtherCommandAsserts(ICommand result, string sourceName)
        {
            Assert.AreEqual(sourceName, result.SourceCharacter);
            Assert.IsFalse(result.FromSelf);
            Assert.AreEqual(this.ExpectedChannel, result.Channel);
            Assert.AreEqual(this.UsesChannelTag, result.UsesChannelTag);
            Assert.AreEqual(this.ChannelTag, result.ChannelTag);

            Assert.AreEqual(CommandType.Foreign, result.CommandType);
            Assert.AreEqual(TargetType.All, result.CommandTargetType);
            Assert.AreEqual(this.UsesExplicitTargettingTag, result.UsesExplicitTargettingTag);
            Assert.AreEqual(this.ExplicitTargettingTag, result.ExplicitTargettingTag);
        }
    }
}
