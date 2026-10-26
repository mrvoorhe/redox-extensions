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

        protected override Commands.CommandChannel ExpectedChannel => CommandChannel.VirindiFellowship;

        protected override bool UsesChannelTag => true;

        protected override object ChannelTag => VIFellowName;

        protected override string CreateFromSelfSampleText(string cmdName)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
            return $"[VI] [{VIFellowName}] You say, \"{formatedCommand}\"";
        }

        protected override string CreateFromSelfSampleText(string cmdName, params object[] args)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
            return $"[VI] [{VIFellowName}] You say, \"{formatedCommand}\"";
        }

        protected override string CreateFromOtherSampleText(string sourceName, string cmdName)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
            return $"[VI] [{VIFellowName}] {sourceName} says, \"{formatedCommand}\"";
        }

        protected override string CreateFromOtherSampleText(string sourceName, string cmdName, params object[] args)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
            return $"[VI] [{VIFellowName}] {sourceName} says, \"{formatedCommand}\"";
        }
    }
}
