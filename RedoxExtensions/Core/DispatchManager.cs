using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Core;
using RedoxExtensions.Dispatching;
using RedoxExtensions.Dispatching.Legacy;

namespace RedoxExtensions.Core
{
    /// <summary>
    /// A container class that holds any dispatcher instances so that RTPlugin doesn't have to house everything
    /// </summary>
    public class DispatchManager : IDisposable
    {
        private BackgroundDispatcher _backgroundDispatcher;
        private GameThreadDispatcher _gameThreadDispatcher;
        private PipelineDispatcher _pipelineDispatcher;

        public DispatchManager(IDecalEventsProxy decalEventsProxy)
        {
            this._backgroundDispatcher = new BackgroundDispatcher();
            this._gameThreadDispatcher = new GameThreadDispatcher(decalEventsProxy);
            this._pipelineDispatcher = new PipelineDispatcher(decalEventsProxy);
        }

        internal BackgroundDispatcher Background
        {
            get
            {
                return this._backgroundDispatcher;
            }
        }

        internal GameThreadDispatcher LegacyGameThread
        {
            get
            {
                return this._gameThreadDispatcher;
            }
        }

        internal PipelineDispatcher Pipeline
        {
            get
            {
                return this._pipelineDispatcher;
            }
        }

        public void Dispose()
        {
            if (this._pipelineDispatcher != null)
            {
                this._pipelineDispatcher.Dispose();
                this._pipelineDispatcher = null;
            }

            if (this._backgroundDispatcher != null)
            {
                this._backgroundDispatcher.Dispose();
                this._backgroundDispatcher = null;
            }

            if (this._gameThreadDispatcher != null)
            {
                this._gameThreadDispatcher.Dispose();
                this._gameThreadDispatcher = null;
            }
        }
    }
}
