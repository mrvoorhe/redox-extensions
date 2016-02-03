using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Commands
{
















    public interface ICommand : ISourceInformation, IExecutionData, ITargettingData, ISupportFeedback, ILoggableObject
    {
    }
}
