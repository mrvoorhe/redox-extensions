using NUnit.Framework;
using NUnit.Framework.Legacy;
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
            ClassicAssert.IsTrue(ChatParsingUtilities.IsChannel(text, Mag.Shared.Util.ChatChannels.Tells));
            ClassicAssert.IsTrue(ChatParsingUtilities.IsChannel(text, Mag.Shared.Util.ChatChannels.All));

            // False cases
            ClassicAssert.IsFalse(ChatParsingUtilities.IsChannel(text, Mag.Shared.Util.ChatChannels.Fellowship));
            ClassicAssert.IsFalse(ChatParsingUtilities.IsChannel(text, Mag.Shared.Util.ChatChannels.None));
        }

        [Test]
        public void GetSourceOfChatCanHandleSimpleNpcName()
        {
            var text = "Hunter tells you, \"Kill 10 Snow Tusker Leaders and I will reward you for your efforts.\"";

            var result = ChatParsingUtilities.GetSourceOfChat(text);

            ClassicAssert.AreEqual("Hunter", result);
        }

        [Test]
        public void GetSourceOfChatCanHandleNpcWithCommaInName()
        {
            var text = "Gregoria, Gurog Destroyer tells you, \"I have made it my mission to rid the land of those... things... which violate the laws of nature before they can infest the rest of Dereth. If you wish to help me then go destroy as many of these things as you can and I shall reward you for your time.\"";

            bool bugged;
            string workAroundText;
            var result = ChatParsingUtilities.GetSourceOfChat(text, out bugged, out workAroundText);

            ClassicAssert.AreEqual("Gregoria, Gurog Destroyer", result);
            ClassicAssert.IsTrue(bugged);
            ClassicAssert.AreEqual("Gurog Destroyer tells you, \"I have made it my mission to rid the land of those... things... which violate the laws of nature before they can infest the rest of Dereth. If you wish to help me then go destroy as many of these things as you can and I shall reward you for your time.\"", workAroundText);
        }

        [Test]
        public void GetSourceOfChatCanHandleFellowshopMessage()
        {
            var text = "Cowhead says to your fellowship, \"!come\"";

            var result = ChatParsingUtilities.GetSourceOfChat(text);

            ClassicAssert.AreEqual("Cowhead", result);
        }

        [Test]
        public void GetSourceOfChatCanHandleFellowshopMessage_SpaceInName()
        {
            var text = "Rockdown Guy says to your fellowship, \"!come\"";

            var result = ChatParsingUtilities.GetSourceOfChat(text);

            ClassicAssert.AreEqual("Rockdown Guy", result);
        }

        [Test]
        public void GetSourceOfChatCanHandleFellowshopMessage_FromSelf()
        {
            var result = ChatParsingUtilities.GetSourceOfChat("You say to your fellowship, \"!come\"");

            ClassicAssert.AreEqual("You", result);
        }

        [Test]
        public void TestIsFellowshipCreated()
        {
            var text = "You have created the Fellowship of My Fellow.";

            string fellowName;
            bool result = ChatParsingUtilities.IsFellowshipCreated(text, out fellowName);
            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("My Fellow", fellowName);
        }

        [Test]
        public void TestIsPlayerJoinedFellowship()
        {
            var text = "Mini Bonsai is now a member of your Fellowship.";

            string playerName;
            bool result = ChatParsingUtilities.IsPlayerJoinedFellowship(text, out playerName);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("Mini Bonsai", playerName);
        }

        [Test]
        public void TestIsPlayerLeftFellowship()
        {
            var text = "Kreap has left your Fellowship.";

            string playerName;
            bool result = ChatParsingUtilities.IsPlayerLeftFellowship(text, out playerName);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("Kreap", playerName);
        }

        [Test]
        public void TestIsYouLeftFellowship()
        {
            var text = "You are no longer a member of the X Fellowship.";
            string fellowshipName;

            bool result = ChatParsingUtilities.IsYouLeftFellowship(text, out fellowshipName);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("X", fellowshipName);
        }

        [Test]
        public void TestIsYouHaveBeenDismissedFromFellowship()
        {
            var text = "Kreap has dismissed you from the Fellowship.";
            string dismisser;

            bool result = ChatParsingUtilities.IsYouHaveBeenDismissedFromFellowship(text, out dismisser);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("Kreap", dismisser);
        }

        [Test]
        public void TestIsFellowshipDisbanded()
        {
            var text = "Kreap has disbanded your Fellowship.";

            string disbandoner;

            bool result = ChatParsingUtilities.IsFellowshipDisbanded(text, out disbandoner);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("Kreap", disbandoner);
        }

        [Test]
        public void TestIsFellowshipDisbanded_WhenSelf()
        {
            var text = "You have disbanded your Fellowship.";

            string disbandoner;

            bool result = ChatParsingUtilities.IsFellowshipDisbanded(text, out disbandoner);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("You", disbandoner);
        }

        [Test]
        public void TestIsYouHaveBeenRecruitedToFellowship()
        {
            var text = "You have been recruited into the X fellowship, a fellowship led by Redox.";

            string fellowName;
            string leader;

            bool result = ChatParsingUtilities.IsYouHaveBeenRecruitedToFellowship(text, out fellowName, out leader);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("X", fellowName);
            ClassicAssert.AreEqual("Redox", leader);
        }

        [Test]
        public void TestIsVIMessage_WhenFellowMessage_FromOther()
        {
            string channel;
            string message;
            string source;

            var text = "[VI] [Redoxrules] Redox says, \"test\"";

            var result = ChatParsingUtilities.IsVIMessage(text, out channel, out source, out message);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("Redoxrules", channel);
            ClassicAssert.AreEqual("Redox", source);
            ClassicAssert.AreEqual("test", message);
        }

        [Test]
        public void TestVIMessage_WhenFellowMessageFromSelf()
        {
            string channel;
            string message;
            string source;

            var text = "[VI] [Redoxrules] You say, \"test\"";

            var result = ChatParsingUtilities.IsVIMessage(text, out channel, out source, out message);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("Redoxrules", channel);
            ClassicAssert.AreEqual("You", source);
            ClassicAssert.AreEqual("test", message);
        }

        [Test]
        public void TestVIMessage_WhenNotVIMessage()
        {
            string channel;
            string message;
            string source;

            var text = "Redox say, \"test\"";

            var result = ChatParsingUtilities.IsVIMessage(text, out channel, out source, out message);

            ClassicAssert.IsFalse(result);
            ClassicAssert.IsNull(channel);
            ClassicAssert.IsNull(source);
            ClassicAssert.IsNull(message);
        }

        [Test]
        public void IsChatTextFromFellowship_PhatACStyle()
        {
            bool fromSelf;
            var result = ChatParsingUtilities.IsChatTextFromFellowship("Cowhead says to your fellowship, \"!come\"", out fromSelf);

            ClassicAssert.IsTrue(result);
            ClassicAssert.IsFalse(fromSelf);
        }

        [Test]
        public void IsChatTestFromFellowship_PhatACStyle_FromSelf()
        {
            bool fromSelf;
            var result = ChatParsingUtilities.IsChatTextFromFellowship("You say to your fellowship, \"!come\"", out fromSelf);

            ClassicAssert.IsTrue(result);
            ClassicAssert.IsTrue(fromSelf);
        }

        [Test]
        public void IsChatTestFromFellowship_GeneralChatMessage()
        {
            bool fromSelf;
            var result = ChatParsingUtilities.IsChatTextFromFellowship("Cowhead says, \"hey\"", out fromSelf);

            ClassicAssert.IsFalse(result);
            ClassicAssert.IsFalse(fromSelf);
        }

        [Test]
        public void TryParseBankBalanceLine_ParsesCommaSeparatedAmount()
        {
            string name;
            long amount;
            var result = ChatParsingUtilities.TryParseBankBalanceLine("[BANK] Pyreals: 47,584,323", out name, out amount);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("Pyreals", name);
            ClassicAssert.AreEqual(47584323L, amount);
        }

        [Test]
        public void TryParseBankBalanceLine_HandlesNameWithSpace()
        {
            string name;
            long amount;
            var result = ChatParsingUtilities.TryParseBankBalanceLine("[BANK] Legendary Keys: 246", out name, out amount);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("Legendary Keys", name);
            ClassicAssert.AreEqual(246L, amount);
        }

        [Test]
        public void TryParseBankBalanceLine_HandlesZero()
        {
            string name;
            long amount;
            var result = ChatParsingUtilities.TryParseBankBalanceLine("[BANK] Mythical Keys: 0", out name, out amount);

            ClassicAssert.IsTrue(result);
            ClassicAssert.AreEqual("Mythical Keys", name);
            ClassicAssert.AreEqual(0L, amount);
        }

        [Test]
        public void TryParseBankBalanceLine_ReturnsFalseForHeader()
        {
            string name;
            long amount;
            var result = ChatParsingUtilities.TryParseBankBalanceLine("[BANK] Your balances are:", out name, out amount);

            ClassicAssert.IsFalse(result);
            ClassicAssert.IsNull(name);
            ClassicAssert.AreEqual(0L, amount);
        }

        [Test]
        public void TryParseBankBalanceLine_ReturnsFalseForNonBankLine()
        {
            string name;
            long amount;
            var result = ChatParsingUtilities.TryParseBankBalanceLine("Cowhead says, \"hey\"", out name, out amount);

            ClassicAssert.IsFalse(result);
            ClassicAssert.IsNull(name);
            ClassicAssert.AreEqual(0L, amount);
        }
    }
}
