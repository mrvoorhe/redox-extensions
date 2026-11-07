using NUnit.Framework;

using RedoxExtensions.Core;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Data;

namespace RedoxExtensions.Tests;

[TestFixture]
public class PendingGiveItemFailureTests
{
    [Test]
    public void IsYouAllowToExamine_ParsesTargetAndItem()
    {
        var result = ChatParsingUtilities.IsYouAllowToExamine(
            "You allow Fiun Ruun to examine your Gem of Attribute Enlightenment.",
            out var targetName,
            out var itemName);

        Assert.That(result, Is.True);
        Assert.That(targetName, Is.EqualTo("Fiun Ruun"));
        Assert.That(itemName, Is.EqualTo("Gem of Attribute Enlightenment"));
    }

    [TestCase("You give Kreap Velvet Baggy Pants.")]
    [TestCase("You allow Kreap to loot your corpse.")]
    [TestCase("Fiun Ruun tells you, \"You allow Bob to examine your Gem.\"")]
    public void IsYouAllowToExamine_IgnoresOtherText(string text)
    {
        Assert.That(ChatParsingUtilities.IsYouAllowToExamine(text, out _, out _), Is.False);
    }

    [Test]
    public void RefuseEmoteChat_GivesRefusedOutcomeWithTargetName()
    {
        // MagTools /mt give to a quest NPC: the chat line comes before the "can't be given" status text,
        // and no busy / can't carry status message is sent.
        var failure = new PendingGiveItemFailure();

        Assert.That(failure.RecordChat("You allow Archmage Ichihiri to examine your Snow Tusker Blood Sample."), Is.True);

        failure.Complete(out var targetName, out var outcome);

        Assert.That(targetName, Is.EqualTo("Archmage Ichihiri"));
        Assert.That(outcome, Is.EqualTo(GiveItemOutcome.Refused));
    }

    [Test]
    public void BusyStatusMessage_GivesFailedBusy()
    {
        var failure = new PendingGiveItemFailure();

        failure.RecordStatusMessage((int)StatusMessageType.UnableToGiveItemBusy, "Town Crier");
        failure.Complete(out var targetName, out var outcome);

        Assert.That(targetName, Is.EqualTo("Town Crier"));
        Assert.That(outcome, Is.EqualTo(GiveItemOutcome.FailedBusy));
    }

    [Test]
    public void CannotCarryStatusMessage_GivesFailedCannotCarryAnymore()
    {
        var failure = new PendingGiveItemFailure();

        failure.RecordStatusMessage((int)StatusMessageType.UnableToGiveItemCannotCarryAnymore, "Trophy Cache");
        failure.Complete(out var targetName, out var outcome);

        Assert.That(targetName, Is.EqualTo("Trophy Cache"));
        Assert.That(outcome, Is.EqualTo(GiveItemOutcome.FailedCannotCarryAnymore));
    }

    [Test]
    public void NothingRecorded_GivesFailedUnknownInsteadOfThrowing()
    {
        var failure = new PendingGiveItemFailure();

        failure.Complete(out var targetName, out var outcome);

        Assert.That(targetName, Is.EqualTo(string.Empty));
        Assert.That(outcome, Is.EqualTo(GiveItemOutcome.FailedUnknown));
    }

    [Test]
    public void Complete_ResetsSoTheNextGiveDoesNotReuseStaleState()
    {
        var failure = new PendingGiveItemFailure();

        failure.RecordStatusMessage((int)StatusMessageType.UnableToGiveItemBusy, "Town Crier");
        failure.Complete(out _, out _);

        failure.Complete(out var targetName, out var outcome);

        Assert.That(targetName, Is.EqualTo(string.Empty));
        Assert.That(outcome, Is.EqualTo(GiveItemOutcome.FailedUnknown));
    }
}
