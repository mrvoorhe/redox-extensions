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

namespace RedoxExtensions.Diagnostics
{
    public static class EventDebugExtensions
    {
        #region Extensions for ILoggableObject

        public static DebugLevel MinimumRequiredDebugLevel(this ILoggableObject e)
        {
            return e.MinimumRequiredDebugLevel;
        }

        public static string ToLoggableFormat(this ILoggableObject e)
        {
            return e.GetLoggableFormat();
        }

        #endregion

        #region Extensions for objects that can't implement ILoggableObject

        public static DebugLevel MinimumRequiredDebugLevel(this MoveObjectEventArgs e)
        {
            // This event can be spammy.
            return DebugLevel.High;
        }

        public static string ToLoggableFormat(this MoveObjectEventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(e.GetType().ToString());
            builder.AppendLine(string.Format("  New (WObj) = {0}", e.Moved.ToShortSummary()));
            builder.AppendLine(string.Format("    Id = {0}", e.Moved.Id));

            return builder.ToString();
        }

        public static DebugLevel MinimumRequiredDebugLevel(this ChatTextInterceptEventArgs e)
        {
            // This event can be spammy.
            return DebugLevel.High;
        }

        public static string ToLoggableFormat(this ChatTextInterceptEventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(e.GetType().ToString());
            builder.AppendLine(string.Format("  Target = {0}", e.Target));
            builder.AppendLine(string.Format("  Text = {0}", e.Text));

            return builder.ToString();
        }

        #endregion

        #region Misc

        public static string ToPrettyString(this ReadOnlyCollection<Location> locationCollection)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine();
            foreach(var loc in locationCollection)
            {
                builder.AppendLine(loc.ToPrettyString());
            }

            return builder.ToString();
        }

        #endregion
    }
}
