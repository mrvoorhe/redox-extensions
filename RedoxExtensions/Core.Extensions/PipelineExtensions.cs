using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Core.Extensions
{
    public static class PipelineExtensions
    {
        public static void Enqueue(this IPipelineAction action)
        {
            REPlugin.Instance.Dispatch.Pipeline.EnqueueAction(action);
        }

        public static void Enqueue(this IAction action)
        {
            REPlugin.Instance.Dispatch.Pipeline.EnqueueAction(action);
        }
    }
}
