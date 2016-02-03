using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using RedoxExtensions.Commands;

namespace RedoxExtensions.Tests.CommandTests
{
    [TestFixture]
    public class TellTests : AbstractForeignCommandTests
    {
        /// <summary>
        /// You Tell messages are not treated as commands
        /// </summary>
        [Test]
        public override void Test_TryParse_WhenFromSelf_NoArguments()
        {
            var text = this.CreateFromSelfSampleText("test");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsFalse(isCommand);
        }

        /// <summary>
        /// You Tell messages are not treated as commands
        /// </summary>
        [Test]
        public override void Test_TryParse_WhenFromSelf_SingleArgument()
        {
            var text = this.CreateFromSelfSampleText("test", "one");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsFalse(isCommand);
        }

        /// <summary>
        /// You Tell messages are not treated as commands
        /// </summary>
        [Test]
        public override void Test_TryParse_WhenFromSelf_ManyArguments()
        {
            var text = this.CreateFromSelfSampleText("test", "one", "two", "three");

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsFalse(isCommand);
        }

        protected override Commands.CommandChannel ExpectedChannel
        {
            get { return CommandChannel.Tell; }
        }

        protected override string CreateFromSelfSampleText(string cmdName)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
            throw new NotImplementedException();
        }

        protected override string CreateFromSelfSampleText(string cmdName, params object[] args)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
            throw new NotImplementedException();
        }

        protected override string CreateFromOtherSampleText(string sourceName, string cmdName)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
            throw new NotImplementedException();
        }

        protected override string CreateFromOtherSampleText(string sourceName, string cmdName, params object[] args)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
            throw new NotImplementedException();
        }
    }
}
