using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Commands;
using RedoxExtensions.Data;

namespace RedoxExtensions.Core
{
    public class DisplayToUserException : Exception
    {
        public DisplayToUserException(string message, ISupportFeedback requestor)
            : base(message)
        {
            this.Requestor = requestor;
        }

        public DisplayToUserException(string message, ISupportFeedback requestor, Exception innerException)
            : base(message, innerException)
        {
            this.Requestor = requestor;
        }

        public ISupportFeedback Requestor { get; private set; }
    }
}
