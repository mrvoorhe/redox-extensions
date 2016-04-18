using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Dispatching
{
    public interface IPipelineAction : IDisposable
    {
        /// <summary>
        /// Called on the game thread
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Checked on the game thread, after IsComplete has returned true.  This is a chance for the action
        /// to ask that it be retried.
        /// </summary>
        bool Retry { get; }

        bool RequireIdleToPerform { get; }

        bool RequiresExplicitVTState { get; }

        VirindiInterop.VTRunState DesiredVTRunState { get; }

        bool Ready();

        /// <summary>
        /// Called on the game thread.  Called before BeginInvoke.  Could be busy when this is called.
        /// </summary>
        void Init();

        /// <summary>
        /// Called on the game thread.  Must return quickly
        /// </summary>
        void Perform();

        /// <summary>
        /// Called on a background thread.  The action must complete at some point.
        /// </summary>
        void WaitForOutcome();

        /// <summary>
        /// Called on the game thread
        /// </summary>
        void End();

        /// <summary>
        /// Called on the game thread.  Gives the action a chance to reset any state before BeginInvoke is called again.
        /// </summary>
        void ResetForRetry();
    }
}
