using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Data;

namespace RedoxExtensions.Core;

/// <summary>
/// Collects what we learn about a give that did not go through, between the begin of the give and the
/// "... can't be given" status text that ends it.
///
/// The server tells us why a give failed in different ways:
///  * Busy / can't carry anymore : a status message whose text is the target name
///  * NPC refuse emote : a "You allow X to examine your Y." chat line (MagTools /mt give to quest NPCs hits this)
/// The status text never carries the target name, so we hold on to what came before it.
/// </summary>
internal class PendingGiveItemFailure
{
    public string TargetName { get; private set; }

    public GiveItemOutcome Outcome { get; private set; } = GiveItemOutcome.Undefined;

    public void Reset()
    {
        this.TargetName = null;
        this.Outcome = GiveItemOutcome.Undefined;
    }

    public void RecordStatusMessage(int statusMessageType, string text)
    {
        if (statusMessageType == (int)StatusMessageType.UnableToGiveItemBusy)
        {
            // Ex:
            //[RT-DEBUG] StatusMessageEventArgs
            //    Text = Town Crier
            //    Type = 30
            this.TargetName = text;
            this.Outcome = GiveItemOutcome.FailedBusy;
        }
        else if (statusMessageType == (int)StatusMessageType.UnableToGiveItemCannotCarryAnymore)
        {
            // Ex:
            //[RT-DEBUG] StatusMessageEventArgs
            //    Text = Trophy Cache
            //    Type = 43
            this.TargetName = text;
            this.Outcome = GiveItemOutcome.FailedCannotCarryAnymore;
        }
    }

    /// <summary>
    /// Returns true if the chat text told us something about the pending give.
    /// </summary>
    public bool RecordChat(string text)
    {
        // Ex: You allow Fiun Ruun to examine your Gem of Attribute Enlightenment.
        if (ChatParsingUtilities.IsYouAllowToExamine(text, out var targetName, out _))
        {
            this.TargetName = targetName;
            this.Outcome = GiveItemOutcome.Refused;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Called when the "... can't be given" status text ends the give.  Returns what we know and resets.
    /// If nothing told us why the give failed, the outcome is FailedUnknown and the target name is empty.
    /// </summary>
    public void Complete(out string targetName, out GiveItemOutcome outcome)
    {
        targetName = this.TargetName ?? string.Empty;
        outcome = this.Outcome == GiveItemOutcome.Undefined ? GiveItemOutcome.FailedUnknown : this.Outcome;

        this.Reset();
    }
}
