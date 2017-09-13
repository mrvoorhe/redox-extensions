using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Data;
using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Core
{
    internal class JumpRecorder : IDisposable
    {
        /// <summary>
        /// The number of consecutive z coords necessary to consider ourselves to have landed
        /// </summary>
        internal const int NumberOfConsecutiveZCoordsSameToSingleLand = 20;

        private readonly IDecalEventsProxy _decalEvents;
        private readonly IREEvents _reEvents;
        private readonly Action<SelfJumpCompleteEventArgs> _fireJumpComplete;
        private readonly Func<Location.Location> _captureCurrentLocation;

        private readonly List<Location.Location> _jumpTrajectory = new List<Location.Location>();

        private bool _isRecording = false;
        private JumpData _cachedJumpData;

        private double _maxZ;
        private DateTime _startTime;

        internal JumpRecorder(
            IREEvents rtEvents,
            IDecalEventsProxy decalEvents,
            Action<SelfJumpCompleteEventArgs> fireJumpComplete,
            Func<Location.Location> captureLocation)
        {
            this._decalEvents = decalEvents;
            this._reEvents = rtEvents;
            this._fireJumpComplete = fireJumpComplete;
            this._captureCurrentLocation = captureLocation;

            this._reEvents.SelfJump += _rtEvents_SelfJump;
        }

        internal JumpRecorder(
            IREEvents rtEvents,
            IDecalEventsProxy decalEvents,
            Action<SelfJumpCompleteEventArgs> fireJumpComplete)
            : this(rtEvents, decalEvents, fireJumpComplete, Location.Location.CaptureCurrent)
        {
        }

        internal bool IsRecording
        {
            get
            {
                return this._isRecording;
            }
        }

        public void Dispose()
        {
            this._decalEvents.RenderFrame -= _decalEvents_RenderFrame;
            this._reEvents.SelfJump -= _rtEvents_SelfJump;
        }

        internal bool HaveLanded()
        {
            // Determine if we've landed based on lack of change to Z coord

            // If the total count is less than the min required to consider ourselves to have landed,
            // then we haven't landed.
            if (this._jumpTrajectory.Count < NumberOfConsecutiveZCoordsSameToSingleLand)
            {
                return false;
            }

            int sameCount = 0;
            for (int i = this._jumpTrajectory.Count - 1; i > 0; i--)
            {
                if (this._jumpTrajectory[i].Z == this._jumpTrajectory[i -1].Z)
                {
                    sameCount++;
                    if(sameCount >= NumberOfConsecutiveZCoordsSameToSingleLand)
                    {
                        return true;
                    }

                    continue;
                }

                return false;
            }

            return false;
        }

        internal void Begin(JumpData jumpData)
        {
            this._jumpTrajectory.Clear();
            this._cachedJumpData = jumpData;
            this._isRecording = true;
            this._maxZ = jumpData.Location.Z;
            this._startTime = DateTime.Now;
            this._decalEvents.RenderFrame += _decalEvents_RenderFrame;
        }

        internal void Update()
        {
            // Capture current coords
            var currentLocation = this._captureCurrentLocation();
            this._jumpTrajectory.Add(currentLocation);

            if (currentLocation.Z > this._maxZ)
            {
                this._maxZ = currentLocation.Z;
            }

            if (this.HaveLanded())
            {
                this.Complete();
            }
        }

        internal Location.Location DetermineLandingLocation()
        {
            // Figure out what the landing location was.  Initially keep it simple. use the last location in the trajectory,
            // Although it may require a little finesse to compensate for bouncing/lag
            return this._jumpTrajectory.Last();
        }

        private void Complete()
        {
            this._decalEvents.RenderFrame -= _decalEvents_RenderFrame;
            this._isRecording = false;
            this._fireJumpComplete(this.CreateEventArgs());
        }

        private SelfJumpCompleteEventArgs CreateEventArgs()
        {
            var landingLoc = this.DetermineLandingLocation();

            // Times 240 to get into AC Meters
            var height = (this._maxZ - this._cachedJumpData.Location.Z) * 240;

            var distance = WorldUtilities.GetDistance(this._cachedJumpData.Location, landingLoc);

            var airTime = DateTime.Now - this._startTime;
            return new SelfJumpCompleteEventArgs(this._cachedJumpData, this._jumpTrajectory, landingLoc, height, distance, airTime);
        }

        private void _rtEvents_SelfJump(object sender, Data.Events.JumpEventArgs e)
        {
            this.Begin(e.Data);
        }

        private void _decalEvents_RenderFrame(object sender, EventArgs e)
        {
            this.Update();
        }
    }
}
