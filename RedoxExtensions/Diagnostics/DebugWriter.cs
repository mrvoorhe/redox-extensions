using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using RedoxExtensions.Data;
using RedoxExtensions.Wrapper;
using RedoxExtensions.VirindiInterop.Events;
using Decal.Adapter.Wrappers;

using RedoxExtensions.Commands;
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Data.Events;
using RedoxExtensions.Settings;

namespace RedoxExtensions.Diagnostics;

/// <summary>
/// Here in case enhancements need to be made while AC is running.  Because in that case
/// the wrapper version of this class would be modifiable.
/// </summary>
public class DebugWriter : Wrapper.Diagnostics.WrapperDebugWriter
{
    #region Instance Data

    private int _lastLoggedBusyState = int.MaxValue;
    private int _lastLoggedBusyStateId = int.MaxValue;

    #endregion

    public DebugWriter(IPluginServices pluginServices, string logNamePrefix, string messagePrefix)
        : base(pluginServices, logNamePrefix, messagePrefix)
    {
    }

    #region Public Methods

    #region Special

    public void LogActionStateValues(bool avoidSpamming)
    {
        var currentBusyState = REPlugin.Instance.CoreManager.Actions.BusyState;
        var currentBusyStateId = REPlugin.Instance.CoreManager.Actions.BusyStateId;

        if (currentBusyState != this._lastLoggedBusyState || !avoidSpamming)
        {
            REPlugin.Instance.Debug.WriteLine("CharacterState.RengerFrame : Actions.BusyState = {0}", currentBusyState);
            this._lastLoggedBusyState = currentBusyState;
        }

        if (currentBusyStateId != this._lastLoggedBusyStateId || !avoidSpamming)
        {
            REPlugin.Instance.Debug.WriteLine("CharacterState.RengerFrame : Actions.BusyStateId = {0}", currentBusyStateId);
            this._lastLoggedBusyStateId = currentBusyStateId;
        }
    }

    #endregion

    #region WriteObject

    internal void WriteObject(ILoggableObject obj)
    {
        this.WriteLoggable(obj.MinimumRequiredDebugLevel, obj.GetLoggableFormat);
    }

    internal void WriteObject(NetworkMessageEventArgs obj, string eventDisplayName)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix(eventDisplayName), stream);
                // Format as hex so that it's easy to pair up with the decal doc
                this.LogRawMessage($"  Message.Type = {obj.Message.Type.ToString("X4")}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(ChangeFellowshipEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("ChangeFellowshipEventArgs"), stream);
                this.LogRawMessage($"  Id = {obj.Id}", stream);
                this.LogRawMessage($"  Type = {obj.Type}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(ObjectIdEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("ObjectIdEventArgs"), stream);
                this.LogRawMessage($"  ObjectId = {obj.ObjectId}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(BeginGiveItemEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("BeginGiveItemEventArgs"), stream);
                this.LogRawMessage($"  ObjectId = {obj.ObjectId}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(EndGiveItemEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("EndGiveItemEventArgs"), stream);
                this.LogRawMessage($"  ItemGiven = {obj.ItemGiven.ToShortSummary()}", stream);
                this.LogRawMessage($"  TargetName = {obj.TargetName}", stream);
                this.LogRawMessage($"  Outcome = {obj.Outcome}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(MoveObjectEventArgs obj)
    {
        this.WriteLoggable(obj.MinimumRequiredDebugLevel(), obj.ToLoggableFormat);
    }

    internal void WriteObject(CreateObjectEventArgs obj)
    {
        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("CreateObjectEventArgs"), stream);
                this.LogRawMessage($"  New (WObj) = {obj.New.Name}", stream);
                this.LogRawMessage($"    Id = {obj.New.Id}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(ChangeObjectEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("ChangeObjectEventArgs"), stream);
                this.LogRawMessage($"  Changed (WObj) = {obj.Changed.Name}", stream);
                this.LogRawMessage($"    Id = {obj.Changed.Id}", stream);
                this.LogRawMessage($"  WorldChangeType = {obj.Change.ToString()}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(EndNonZeroBusyStateEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("EndNonZeroBusyStateEventArgs"), stream);
                this.LogRawMessage($"  BusyState = {obj.BusyState}", stream);
                this.LogRawMessage($"  BusyStateId = {obj.BusyStateId}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(BeginNonZeroBusyStateEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("BeginNonZeroBusyStateEventArgs"), stream);
                this.LogRawMessage($"  BusyState = {obj.BusyState}", stream);
                this.LogRawMessage($"  BusyStateId = {obj.BusyStateId}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(UsingObjectEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("UsingObjectEventArgs"), stream);
                this.LogRawMessage($"  ObjectName = {obj.ObjectName}", stream);
                this.LogRawMessage($"  ObjectId = {obj.ObjectId}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(UsingPortalCompleteEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("UsingPortalCompleteEventArgs"), stream);
                this.LogRawMessage($"  Successful = {obj.Successful}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(BeginBusyEventArgs obj)
    {
        this.WriteObject_ArgLessEventArgs("BeginBusyEventArgs");
    }

    internal void WriteObject(BeginIdleEventArgs obj)
    {
        this.WriteObject_ArgLessEventArgs("BeginIdleEventArgs");
    }

    internal void WriteObject(YourTooBusyEventArgs obj)
    {
        this.WriteObject_ArgLessEventArgs("YourTooBusyEventArgs");
    }

    internal void WriteObject(ApproachVendorEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("ApproachVendorEventArgs"), stream);
                this.LogRawMessage($"  MerchantId = {obj.MerchantId}", stream);
                this.LogRawMessage("  Vendor [Complex Type - Details Not Available]", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(ApproachingObjectEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("ApproachingObjectEventArgs"), stream);
                this.LogRawMessage($"  ObjectName = {obj.ObjectName}", stream);
                this.LogRawMessage($"  ObjectId = {obj.ObjectId}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(UsingPortalEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("UsingPortalEventArgs"), stream);
                this.LogRawMessage($"  PortalName = {obj.PortalName}", stream);
                this.LogRawMessage($"  PortalId = {obj.PortalId}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(StatusTextInterceptEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("StatusTextInterceptEventArgs"), stream);
                this.LogRawMessage($"  Text = {obj.Text}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(ChangePortalModeEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("ChangePortalModeEventArgs"), stream);
                this.LogRawMessage($"  PortalEventType = {obj.Type}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(SpellCastEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("SpellCastEventArgs"), stream);
                this.LogRawMessage($"  SpellId = {obj.SpellId}", stream);
                this.LogRawMessage($"  TargetId = {obj.TargetId}", stream);
                this.LogRawMessage($"  CastEventType = {obj.EventType}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(StatusMessageEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("StatusMessageEventArgs"), stream);
                this.LogRawMessage($"  Text = {obj.Text}", stream);
                this.LogRawMessage($"  Type = {obj.Type}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(SpellCastCompleteEventArgs obj)
    {
        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("SpellCastCompleteEventArgs"), stream);
                this.LogRawMessage($"  SpellId = {obj.SpellId}", stream);
                this.LogRawMessage($"  Target = {obj.Target}", stream);
                this.LogRawMessage($"  Duration = {obj.Duration}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(SpellCastAttemptingEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("SpellCastAttemptingEventArgs"), stream);
                this.LogRawMessage($"  SpellId = {obj.SpellId}", stream);
                this.LogRawMessage($"  Target = {obj.Target}", stream);
                this.LogRawMessage($"  Skill = {obj.Skill}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    internal void WriteObject(ItemSelectedEventArgs obj)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("ItemSelectedEventArgs"), stream);
                this.LogRawMessage($"  ItemGuid = {obj.ItemGuid}", stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    public void WriteObject(ChatTextInterceptEventArgs obj)
    {
        this.WriteLoggable(obj.MinimumRequiredDebugLevel(), obj.ToLoggableFormat);
    }

    internal void WriteObject(ChatParserInterceptEventArgs obj)
    {
        this.WriteObject(obj, false);
    }

    internal void WriteObject(ChatParserInterceptEventArgs obj, bool toLogFileOnly)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (_writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix("ChatParserInterceptEventArgs"), stream, toLogFileOnly);
                this.LogRawMessage($"  Text = {obj.Text}", stream, toLogFileOnly);

                //this.WriteCurrentStateStuff(stream, toLogFileOnly);

                this.LogRawMessage("", stream, toLogFileOnly);
            }
        }
    }

    internal void WriteObject(Dictionary<string, string> obj, string title)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix(title), stream);
                foreach (var keyValue in obj)
                {
                    this.LogRawMessage($"  Name = {keyValue.Key} | Value = {keyValue}", stream);
                }

                this.LogRawMessage("", stream);
            }
        }
    }

    #endregion

    #region Log Object

    internal void LogObject(ChatParserInterceptEventArgs obj)
    {
        this.WriteObject(obj, true);
    }

    #endregion

    #endregion

    #region Private Methods

    internal void WriteLoggable(DebugLevel minimumRequiredDebugLevel, Func<string> getLoggableFormat)
    {
        if (ActiveSettings.Instance.DebugLevel < minimumRequiredDebugLevel)
        {
            return;
        }

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix(getLoggableFormat()), stream);
                this.LogRawMessage("", stream);
            }
        }
    }

    private void WriteObject_ArgLessEventArgs(string eventArgsName)
    {
        if (ActiveSettings.Instance.DebugLevel == DebugLevel.None)
            return;

        lock (this._writeLock)
        {
            using (StreamWriter stream = new StreamWriter(this._currentPath, true))
            {
                this.LogRawMessage(this.FormatWithPrefix(eventArgsName), stream);

                //this.WriteCurrentStateStuff(stream, false);

                this.LogRawMessage("", stream);
            }
        }
    }

    #endregion
}
