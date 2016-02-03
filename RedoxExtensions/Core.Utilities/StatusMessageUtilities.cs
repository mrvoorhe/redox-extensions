using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Core.Utilities
{
    /// <summary>
    /// Utilities for parsing data from status message events
    /// </summary>
    public static class StatusMessageUtilities
    {
        private const string CantBeGiven = "can't be given";
        private const string YoureTooBusy = "You're too busy!";

        private const string UsingObjectStatusTextPrefix = "Using the";
        private const string ApproachingObjectStatusTextPrefix = "Approaching";

        public static bool IsUsingObject(string text)
        {
            return text.StartsWith(UsingObjectStatusTextPrefix);
        }

        public static bool IsUsingObject(string text, out string objectName)
        {
            if (IsUsingObject(text))
            {
                objectName = text.Substring(UsingObjectStatusTextPrefix.Length + 1);
                return true;
            }

            objectName = string.Empty;
            return false;
        }

        public static bool IsApproachingObject(string text)
        {
            return text.StartsWith(ApproachingObjectStatusTextPrefix);
        }

        public static bool IsApproachingObject(string text, out string objectName)
        {
            if (IsApproachingObject(text))
            {
                objectName = text.Substring(ApproachingObjectStatusTextPrefix.Length + 1);
                return true;
            }

            objectName = string.Empty;
            return false;
        }

        public static bool IsYoureTooBusy(string text)
        {
            return text.StartsWith(YoureTooBusy);
        }

        /// <summary>
        /// Ex: The Aged Legendary Key can't be given
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsCantBeGiven(string text)
        {
            return text.Trim().EndsWith(CantBeGiven);
        }

        /// <summary>
        /// Ex: The Aged Legendary Key can't be given
        /// </summary>
        /// <param name="text"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public static bool IsCantBeGiven(string text, out string itemName)
        {
            if (IsCantBeGiven(text))
            {
                var tmp = text.Trim().Substring(4);
                itemName = tmp.Substring(0, tmp.Length - CantBeGiven.Length).Trim();
                return true;
            }

            itemName = string.Empty;
            return false;
        }

        #region Extension Methods

        public static bool IsCantBeGiven(this StatusTextInterceptEventArgs eventArgs)
        {
            return IsCantBeGiven(eventArgs.Text);
        }

        public static bool IsYoureTooBusy(this StatusTextInterceptEventArgs eventArgs)
        {
            return IsYoureTooBusy(eventArgs.Text);
        }

        public static bool IsUsingObject(this StatusTextInterceptEventArgs eventArgs)
        {
            return IsUsingObject(eventArgs.Text);
        }

        public static bool IsUsingObject(this StatusTextInterceptEventArgs eventArgs, out string objectName)
        {
            return IsUsingObject(eventArgs.Text, out objectName);
        }

        public static bool IsApproachingObject(this StatusTextInterceptEventArgs eventArgs)
        {
            return IsApproachingObject(eventArgs.Text);
        }

        public static bool IsApproachingObject(this StatusTextInterceptEventArgs eventArgs, out string objectName)
        {
            return IsApproachingObject(eventArgs.Text, out objectName);
        }


        #endregion
    }
}
