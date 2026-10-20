using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Framework.Legacy;

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

            ClassicAssert.IsTrue(isCommand);

            ClassicAssert.AreEqual("test", result.Name);
            ClassicAssert.AreEqual(0, result.Arguments.Count);
            ClassicAssert.AreEqual(text, result.RawValue);

            this.CommonCommandAsserts(result);
        }

        [Test]
        public void Test_TryParse_WhenSingleArgumentCommand()
        {
            string text = this.CreateCommandText("test", "one");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            ClassicAssert.IsTrue(isCommand);

            ClassicAssert.AreEqual("test", result.Name);
            ClassicAssert.AreEqual(1, result.Arguments.Count);
            ClassicAssert.AreEqual("one", result.Arguments[0]);
            ClassicAssert.AreEqual(text, result.RawValue);

            this.CommonCommandAsserts(result);
        }

        [Test]
        public void Test_TryParse_WhenManyArgumentsCommand()
        {
            string text = this.CreateCommandText("test", "one", "two", "three");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            ClassicAssert.IsTrue(isCommand);

            ClassicAssert.AreEqual("test", result.Name);
            ClassicAssert.AreEqual(3, result.Arguments.Count);
            ClassicAssert.AreEqual("one", result.Arguments[0]);
            ClassicAssert.AreEqual("two", result.Arguments[1]);
            ClassicAssert.AreEqual("three", result.Arguments[2]);
            ClassicAssert.AreEqual(text, result.RawValue);

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
            ClassicAssert.AreEqual(CommandHelpers.DefaultYouValue, result.SourceCharacter);
            ClassicAssert.IsTrue(result.FromSelf);
            ClassicAssert.AreEqual(CommandChannel.DirectEntry, result.Channel);
            ClassicAssert.IsFalse(result.UsesChannelTag);

            ClassicAssert.AreEqual(this.ExpectedCommandType, result.CommandType);
            ClassicAssert.AreEqual(TargetType.SelfOnly, result.CommandTargetType);
            ClassicAssert.IsFalse(result.UsesExplicitTargettingTag);
        }
    }
}
