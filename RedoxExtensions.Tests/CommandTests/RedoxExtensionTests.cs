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
    public class RedoxExtensionTests : AbstractDirectEntryOnlyCommandTests
    {
        protected override CommandType ExpectedCommandType
        {
            get
            {
                return CommandType.RedoxExtension;
            }
        }

        protected override string CreateCommandText(string cmdName)
        {
            return CommandHelpers.CreateRedoxExtensionsCommandText(cmdName);
        }

        protected override string CreateCommandText(string cmdName, params object[] args)
        {
            return CommandHelpers.CreateRedoxExtensionsCommandText(cmdName, args);
        }
    }
}
