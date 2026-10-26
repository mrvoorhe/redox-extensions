using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using RedoxExtensions.Commands;
using RedoxExtensions.Core.Utilities;

namespace RedoxExtensions.Tests.CommandTests
{
    [TestFixture]
    public class FellowshipTests : AbstractForeignCommandTests
    {
        protected override Commands.CommandChannel ExpectedChannel => CommandChannel.Fellowship;

        protected override string CreateFromSelfSampleText(string cmdName)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
            return $"[Fellowship] You say, \"{formatedCommand}\"\n";
        }

        protected override string CreateFromSelfSampleText(string cmdName, params object[] args)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
            return $"[Fellowship] You say, \"{formatedCommand}\"\n";
        }

        protected override string CreateFromOtherSampleText(string sourceName, string cmdName)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
            return $"[Fellowship] <Tell:IIDString:0:{sourceName}>{sourceName}<\\Tell> says, \"{formatedCommand}\"\n";
        }

        protected override string CreateFromOtherSampleText(string sourceName, string cmdName, params object[] args)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
            return $"[Fellowship] <Tell:IIDString:0:{sourceName}>{sourceName}<\\Tell> says, \"{formatedCommand}\"\n";
        }
    }
}
