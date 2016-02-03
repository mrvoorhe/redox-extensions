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
        protected override Commands.CommandChannel ExpectedChannel
        {
            get { return CommandChannel.Fellowship; }
        }

        protected override string CreateFromSelfSampleText(string cmdName)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
            return string.Format("[Fellowship] You say, \"{0}\"\n", formatedCommand);
        }

        protected override string CreateFromSelfSampleText(string cmdName, params object[] args)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
            return string.Format("[Fellowship] You say, \"{0}\"\n", formatedCommand);
        }

        protected override string CreateFromOtherSampleText(string sourceName, string cmdName)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
            return string.Format("[Fellowship] <Tell:IIDString:0:{0}>{0}<\\Tell> says, \"{1}\"\n", sourceName, formatedCommand);
        }

        protected override string CreateFromOtherSampleText(string sourceName, string cmdName, params object[] args)
        {
            var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
            return string.Format("[Fellowship] <Tell:IIDString:0:{0}>{0}<\\Tell> says, \"{1}\"\n", sourceName, formatedCommand);
        }
    }
}
