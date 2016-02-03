using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using RedoxExtensions.Commands;

namespace RedoxExtensions.Tests.CommandTests
{
    public abstract class AbstractDirectEntryOnlyCommandTests
    {
        protected abstract CommandType ExpectedCommandType { get; }

        [Test]
        public void Test_TryParse_WhenNoArgumentsCommand()
        {
            string text = this.CreateCommandText("test");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsTrue(isCommand);

            Assert.AreEqual("test", result.Name);
            Assert.AreEqual(0, result.Arguments.Count);
            Assert.AreEqual(text, result.RawValue);

            this.CommonCommandAsserts(result);
        }

        [Test]
        public void Test_TryParse_WhenSingleArgumentCommand()
        {
            string text = this.CreateCommandText("test", "one");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsTrue(isCommand);

            Assert.AreEqual("test", result.Name);
            Assert.AreEqual(1, result.Arguments.Count);
            Assert.AreEqual("one", result.Arguments[0]);
            Assert.AreEqual(text, result.RawValue);

            this.CommonCommandAsserts(result);
        }

        [Test]
        public void Test_TryParse_WhenManyArgumentsCommand()
        {
            string text = this.CreateCommandText("test", "one", "two", "three");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsTrue(isCommand);

            Assert.AreEqual("test", result.Name);
            Assert.AreEqual(3, result.Arguments.Count);
            Assert.AreEqual("one", result.Arguments[0]);
            Assert.AreEqual("two", result.Arguments[1]);
            Assert.AreEqual("three", result.Arguments[2]);
            Assert.AreEqual(text, result.RawValue);

            this.CommonCommandAsserts(result);
        }

        protected abstract string CreateCommandText(string cmdName);
        protected abstract string CreateCommandText(string cmdName, params object[] args);

        /// <summary>
        /// Performs assertions that are common to all commands of this type
        /// </summary>
        /// <param name="result"></param>
        private void CommonCommandAsserts(ICommand result)
        {
            Assert.AreEqual(CommandHelpers.DefaultYouValue, result.SourceCharacter);
            Assert.IsTrue(result.FromSelf);
            Assert.AreEqual(CommandChannel.DirectEntry, result.Channel);
            Assert.IsFalse(result.UsesChannelTag);

            Assert.AreEqual(this.ExpectedCommandType, result.CommandType);
            Assert.AreEqual(TargetType.SelfOnly, result.CommandTargetType);
            Assert.IsFalse(result.UsesExplicitTargettingTag);
        }
    }
}
