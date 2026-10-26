using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using RedoxExtensions.Commands;

namespace RedoxExtensions.Tests.CommandTests;

[TestFixture]
public class AreaChatTests : AbstractForeignCommandTests
{
    protected override Commands.CommandChannel ExpectedChannel => CommandChannel.AreaChat;

    protected override string CreateFromSelfSampleText(string cmdName)
    {
        var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
        return $"You say, \"{formatedCommand}\"\n";
    }

    protected override string CreateFromSelfSampleText(string cmdName, params object[] args)
    {
        var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
        return $"You say, \"{formatedCommand}\"\n";
    }

    protected override string CreateFromOtherSampleText(string sourceName, string cmdName)
    {
        var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName);
        return $"<Tell:IIDString:0:{sourceName}>{sourceName}<\\Tell> says, \"{formatedCommand}\"\n";
    }

    protected override string CreateFromOtherSampleText(string sourceName, string cmdName, params object[] args)
    {
        var formatedCommand = CommandHelpers.CreateForeignCommandText(cmdName, args);
        return $"<Tell:IIDString:0:{sourceName}>{sourceName}<\\Tell> says, \"{formatedCommand}\"\n";
    }
}
