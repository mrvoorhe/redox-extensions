using NUnit.Framework;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class ChatChannelFilterTests
{
    [TestCase("[General] Someone says, \"wts stuff\"")]
    [TestCase("[Allegiance] <Tell:IIDString:123:Bob>Bob<\\Tell> says, \"hi\"")]
    [TestCase("[Trade] Someone says, \"wtb\"")]
    public void PublicChannelChatIsIgnored(string text)
    {
        Assert.That(ChatChannelFilter.IsIgnored(text), Is.True);
    }

    [TestCase("You say, \"[General] is noisy\"")]
    [TestCase("[Fellowship] Bob says, \"go\"")]
    [TestCase("Bob tells you, \"hi\"")]
    [TestCase("")]
    [TestCase(null)]
    public void OtherChatIsKept(string text)
    {
        Assert.That(ChatChannelFilter.IsIgnored(text), Is.False);
    }
}
