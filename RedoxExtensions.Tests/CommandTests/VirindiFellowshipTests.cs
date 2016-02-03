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
    public class VirindiFellowshipTests : AbstractForeignCommandTests
    {
        private const string VIFellowName = "TestFellow";

        protected override Commands.CommandChannel ExpectedChannel
        {
            get { return CommandChannel.VirindiFellowship; }
        }

        protected override bool UsesChannelTag
        {
            get
            {
                return true;
            }
        }

        protected override object ChannelTag
        {
            get
            {
                return VIFellowName;
            }
        }

        protected override string CreateFromSelfSampleText(string cmdName)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
            return string.Format("[VI] [{0}] You say, \"{1}\"", VIFellowName, formatedCommand);
        }

        protected override string CreateFromSelfSampleText(string cmdName, params object[] args)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
            return string.Format("[VI] [{0}] You say, \"{1}\"", VIFellowName, formatedCommand);
        }

        protected override string CreateFromOtherSampleText(string sourceName, string cmdName)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
            return string.Format("[VI] [{0}] {1} says, \"{2}\"", VIFellowName, sourceName, formatedCommand);
        }

        protected override string CreateFromOtherSampleText(string sourceName, string cmdName, params object[] args)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
            return string.Format("[VI] [{0}] {1} says, \"{2}\"", VIFellowName, sourceName, formatedCommand);
        }
    }
}
