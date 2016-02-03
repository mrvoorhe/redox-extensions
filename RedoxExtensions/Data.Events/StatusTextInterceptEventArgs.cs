using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Data.Events
{
    /// <summary>
    /// The message that can appear, in yellow, in the upper center of the screen
    /// </summary>
    public class StatusTextInterceptEventArgs : EventArgs
    {
        public StatusTextInterceptEventArgs(string text)
        {
            this.Text = text;
        }

        public string Text { get; private set; }

        public bool Eat { get; set; }
    }
}
