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
    public class GeneralTests
    {
        [Test]
        public void Test_TryParse_WhenNotACommand()
        {
            const string text = "/r Hello World";

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsFalse(isCommand);
        }

        [Test]
        public void Test_TryParse_NormalFellowshipChatMessage()
        {
            const string text = "[Fellowship] <Tell:IIDString:0:Redox>Redox<\\Tell> says, \"Hello World\"\n";

            ICommand result;
            bool isCommand = CommandHelpers.TryParse(text, out result);

            Assert.IsFalse(isCommand);
        }
    }
}
