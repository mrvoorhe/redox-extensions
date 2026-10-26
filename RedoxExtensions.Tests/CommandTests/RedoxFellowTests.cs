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
    public class RedoxFellowTests : AbstractDirectEntryOnlyCommandTests
    {
        protected override Commands.CommandType ExpectedCommandType => CommandType.RedoxFellow;

        protected override string CreateCommandText(string cmdName) => CommandHelpers.CreateRedoxFellowCommandText(cmdName);

        protected override string CreateCommandText(string cmdName, params object[] args) => CommandHelpers.CreateRedoxFellowCommandText(cmdName, args);
    }
}
