using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Core.Utilities;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class ChatParsingUtilitiesTests
    {
        [Test]
        public void IsChannel_ReturnsExpectedValuesWhenCheckingNpcTell()
        {
            string text =  "Umbral Guard tells you, \"Kill 15 Shadow Flyers and I will reward you for your efforts.\"";

            // True cases.
            Assert.IsTrue(ChatParsingUtilities.IsChannel(text, Mag.Shared.Util.ChatChannels.Tells));
            Assert.IsTrue(ChatParsingUtilities.IsChannel(text, Mag.Shared.Util.ChatChannels.All));

            // False cases
            Assert.IsFalse(ChatParsingUtilities.IsChannel(text, Mag.Shared.Util.ChatChannels.Fellowship));
            Assert.IsFalse(ChatParsingUtilities.IsChannel(text, Mag.Shared.Util.ChatChannels.None));
        }

        [Test]
        public void GetSourceOfChatCanHandleSimpleNpcName()
        {
            var text = "Hunter tells you, \"Kill 10 Snow Tusker Leaders and I will reward you for your efforts.\"";

            var result = ChatParsingUtilities.GetSourceOfChat(text);

            Assert.AreEqual("Hunter", result);
        }

        [Test]
        public void GetSourceOfChatCanHandleNpcWithCommaInName()
        {
            var text = "Gregoria, Gurog Destroyer tells you, \"I have made it my mission to rid the land of those... things... which violate the laws of nature before they can infest the rest of Dereth. If you wish to help me then go destroy as many of these things as you can and I shall reward you for your time.\"";

            bool bugged;
            string workAroundText;
            var result = ChatParsingUtilities.GetSourceOfChat(text, out bugged, out workAroundText);

            Assert.AreEqual("Gregoria, Gurog Destroyer", result);
            Assert.IsTrue(bugged);
            Assert.AreEqual("Gurog Destroyer tells you, \"I have made it my mission to rid the land of those... things... which violate the laws of nature before they can infest the rest of Dereth. If you wish to help me then go destroy as many of these things as you can and I shall reward you for your time.\"", workAroundText);
        }

        [Test]
        public void GetSourceOfChatCanHandleFellowshopMessage()
        {
            var text = "Cowhead says to your fellowship, \"!come\"";

            var result = ChatParsingUtilities.GetSourceOfChat(text);

            Assert.AreEqual("Cowhead", result);
        }

        [Test]
        public void GetSourceOfChatCanHandleFellowshopMessage_SpaceInName()
        {
            var text = "Rockdown Guy says to your fellowship, \"!come\"";

            var result = ChatParsingUtilities.GetSourceOfChat(text);

            Assert.AreEqual("Rockdown Guy", result);
        }

        [Test]
        public void GetSourceOfChatCanHandleFellowshopMessage_FromSelf()
        {
            var result = ChatParsingUtilities.GetSourceOfChat("You say to your fellowship, \"!come\"");

            Assert.AreEqual("You", result);
        }

        [Test]
        public void TestIsFellowshipCreated()
        {
            var text = "You have created the Fellowship of My Fellow.";

            string fellowName;
            bool result = ChatParsingUtilities.IsFellowshipCreated(text, out fellowName);
            Assert.IsTrue(result);
            Assert.AreEqual("My Fellow", fellowName);
        }

        [Test]
        public void TestIsPlayerJoinedFellowship()
        {
            var text = "Mini Bonsai is now a member of your Fellowship.";

            string playerName;
            bool result = ChatParsingUtilities.IsPlayerJoinedFellowship(text, out playerName);

            Assert.IsTrue(result);
            Assert.AreEqual("Mini Bonsai", playerName);
        }

        [Test]
        public void TestIsPlayerLeftFellowship()
        {
            var text = "Kreap has left your Fellowship.";

            string playerName;
            bool result = ChatParsingUtilities.IsPlayerLeftFellowship(text, out playerName);

            Assert.IsTrue(result);
            Assert.AreEqual("Kreap", playerName);
        }

        [Test]
        public void TestIsYouLeftFellowship()
        {
            var text = "You are no longer a member of the X Fellowship.";
            string fellowshipName;

            bool result = ChatParsingUtilities.IsYouLeftFellowship(text, out fellowshipName);

            Assert.IsTrue(result);
            Assert.AreEqual("X", fellowshipName);
        }

        [Test]
        public void TestIsYouHaveBeenDismissedFromFellowship()
        {
            var text = "Kreap has dismissed you from the Fellowship.";
            string dismisser;

            bool result = ChatParsingUtilities.IsYouHaveBeenDismissedFromFellowship(text, out dismisser);

            Assert.IsTrue(result);
            Assert.AreEqual("Kreap", dismisser);
        }

        [Test]
        public void TestIsFellowshipDisbanded()
        {
            var text = "Kreap has disbanded your Fellowship.";

            string disbandoner;

            bool result = ChatParsingUtilities.IsFellowshipDisbanded(text, out disbandoner);

            Assert.IsTrue(result);
            Assert.AreEqual("Kreap", disbandoner);
        }

        [Test]
        public void TestIsFellowshipDisbanded_WhenSelf()
        {
            var text = "You have disbanded your Fellowship.";

            string disbandoner;

            bool result = ChatParsingUtilities.IsFellowshipDisbanded(text, out disbandoner);

            Assert.IsTrue(result);
            Assert.AreEqual("You", disbandoner);
        }

        [Test]
        public void TestIsYouHaveBeenRecruitedToFellowship()
        {
            var text = "You have been recruited into the X fellowship, a fellowship led by Redox.";

            string fellowName;
            string leader;

            bool result = ChatParsingUtilities.IsYouHaveBeenRecruitedToFellowship(text, out fellowName, out leader);

            Assert.IsTrue(result);
            Assert.AreEqual("X", fellowName);
            Assert.AreEqual("Redox", leader);
        }

        [Test]
        public void TestIsVIMessage_WhenFellowMessage_FromOther()
        {
            string channel;
            string message;
            string source;

            var text = "[VI] [Redoxrules] Redox says, \"test\"";

            var result = ChatParsingUtilities.IsVIMessage(text, out channel, out source, out message);

            Assert.IsTrue(result);
            Assert.AreEqual("Redoxrules", channel);
            Assert.AreEqual("Redox", source);
            Assert.AreEqual("test", message);
        }

        [Test]
        public void TestVIMessage_WhenFellowMessageFromSelf()
        {
            string channel;
            string message;
            string source;

            var text = "[VI] [Redoxrules] You say, \"test\"";

            var result = ChatParsingUtilities.IsVIMessage(text, out channel, out source, out message);

            Assert.IsTrue(result);
            Assert.AreEqual("Redoxrules", channel);
            Assert.AreEqual("You", source);
            Assert.AreEqual("test", message);
        }

        [Test]
        public void TestVIMessage_WhenNotVIMessage()
        {
            string channel;
            string message;
            string source;

            var text = "Redox say, \"test\"";

            var result = ChatParsingUtilities.IsVIMessage(text, out channel, out source, out message);

            Assert.IsFalse(result);
            Assert.IsNull(channel);
            Assert.IsNull(source);
            Assert.IsNull(message);
        }

        [Test]
        public void IsChatTextFromFellowship_PhatACStyle()
        {
            bool fromSelf;
            var result = ChatParsingUtilities.IsChatTextFromFellowship("Cowhead says to your fellowship, \"!come\"", out fromSelf);

            Assert.IsTrue(result);
            Assert.IsFalse(fromSelf);
        }

        [Test]
        public void IsChatTestFromFellowship_PhatACStyle_FromSelf()
        {
            bool fromSelf;
            var result = ChatParsingUtilities.IsChatTextFromFellowship("You say to your fellowship, \"!come\"", out fromSelf);

            Assert.IsTrue(result);
            Assert.IsTrue(fromSelf);
        }

        [Test]
        public void IsChatTestFromFellowship_GeneralChatMessage()
        {
            bool fromSelf;
            var result = ChatParsingUtilities.IsChatTextFromFellowship("Cowhead says, \"hey\"", out fromSelf);

            Assert.IsFalse(result);
            Assert.IsFalse(fromSelf);
        }
    }
}
