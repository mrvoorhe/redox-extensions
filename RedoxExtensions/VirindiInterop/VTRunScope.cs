using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RedoxExtensions.VirindiInterop
{
    public enum VTRunState
    {
        On,
        Off
    }

    /// <summary>
    /// Creates a scope where VT is either On or Off
    /// </summary>
    public class VTRunScope : IDisposable
    {
        private readonly VTRunState _desiredRunState;
        //private readonly VTRunState _originalRunState;

        private VTRunScope(VTRunState desiredVTState)
        {
            this._desiredRunState = desiredVTState;
            //this._originalRunState = RTPlugin.Instance.MonitorManager.CharacterState.VTRunning.WaitOne(0) ? VTRunState.On : VTRunState.Off;

            //RTPlugin.Instance.DebugWriter.WriteLine(this._originalRunState.ToString());

            //if (this._desiredRunState == VTRunState.On && this._originalRunState == VTRunState.Off)
            //{
            //    RTPlugin.Instance.DebugWriter.WriteLine("Create Run Scope : Starting VT");
            //    VTActions.StartVT();
            //}
            //else if (this._desiredRunState == VTRunState.Off && this._originalRunState == VTRunState.On)
            //{
            //    RTPlugin.Instance.DebugWriter.WriteLine("Create Run Scope : Stopping VT");
            //    VTActions.StopVT();
            //}

            if (this._desiredRunState == VTRunState.On)
            {
                VTActions.StartVT();
            }
            else
            {
                VTActions.StopVT();
            }
        }

        public static VTRunScope Enter(VTRunState desiredState)
        {
            return new VTRunScope(desiredState);
        }

        public static VTRunScope EnterStopped()
        {
            return Enter(VTRunState.Off);
        }

        public static VTRunScope EnterRunning()
        {
            return Enter(VTRunState.On);
        }

        public static VTRunScope EnterStoppedFromBackground()
        {
            // TODO : Implement
            throw new NotImplementedException();
        }

        public VTRunState DesiredState
        {
            get
            {
                return this._desiredRunState;
            }
        }

        public void Dispose()
        {
            //if (this._desiredRunState == VTRunState.On && this._originalRunState == VTRunState.Off)
            //{
            //    RTPlugin.Instance.DebugWriter.WriteLine("Dispose Run Scope : Stopping VT");
            //    VTActions.StopVT();
            //}
            //else if(this._desiredRunState == VTRunState.Off && this._originalRunState == VTRunState.On)
            //{
            //    RTPlugin.Instance.DebugWriter.WriteLine("Dispose Run Scope : Starting VT");
            //    VTActions.StartVT();
            //}

            if (this._desiredRunState == VTRunState.On)
            {
                VTActions.StopVT();
            }
            else
            {
                VTActions.StartVT();
            }
        }
    }
}
