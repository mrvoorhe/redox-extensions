using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Commands
{
    public interface ISupportFeedback : ISourceInformation
    {
        void GiveFeedback(FeedbackType feedbackType, string message);
        void GiveFeedback(FeedbackType feedbackType, string message, params object[] args);
    }
}
