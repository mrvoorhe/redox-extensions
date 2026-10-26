using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxExtensions.Data;
using RedoxExtensions.Data.Events;
using Decal.Adapter;

using RedoxExtensions.Core.Extensions;

namespace RedoxExtensions.Diagnostics;

public static class EventDebugExtensions
{
    #region Extensions for ILoggableObject

    public static DebugLevel MinimumRequiredDebugLevel(this ILoggableObject e) => e.MinimumRequiredDebugLevel;

    public static string ToLoggableFormat(this ILoggableObject e) => e.GetLoggableFormat();

    #endregion

    #region Extensions for objects that can't implement ILoggableObject

    public static DebugLevel MinimumRequiredDebugLevel(this MoveObjectEventArgs e)
    {
        // This event can be spammy.
        return DebugLevel.High;
    }

    public static string ToLoggableFormat(this MoveObjectEventArgs e)
    {
        var builder = new StringBuilder();
        builder.AppendLine(e.GetType().ToString());
        builder.AppendLine($"  New (WObj) = {e.Moved.ToShortSummary()}");
        builder.AppendLine($"    Id = {e.Moved.Id}");

        return builder.ToString();
    }

    public static DebugLevel MinimumRequiredDebugLevel(this ChatTextInterceptEventArgs e)
    {
        // This event can be spammy.
        return DebugLevel.High;
    }

    public static string ToLoggableFormat(this ChatTextInterceptEventArgs e)
    {
        var builder = new StringBuilder();
        builder.AppendLine(e.GetType().ToString());
        builder.AppendLine($"  Target = {e.Target}");
        builder.AppendLine($"  Text = {e.Text}");

        return builder.ToString();
    }

    #endregion

    #region Misc

    public static string ToPrettyString(this ReadOnlyCollection<Location.Location> locationCollection)
    {
        var builder = new StringBuilder();
        builder.AppendLine();
        foreach(var loc in locationCollection)
        {
            builder.AppendLine(loc.ToPrettyString());
        }

        return builder.ToString();
    }

    #endregion
}
