
using System;
using System.ComponentModel;
using System.Security.Permissions;
using System.Threading;

namespace Examine
{
    /// <summary>
    /// A BackgroundWorker that has an end event handler, this worker does not report progress and also flags itself as not busy when it's operation is complete, not when the
    /// finialized event handler is triggered.
    /// </summary>
    [DefaultEvent("DoWork")]
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public class BackgroundWorkerSlim : Component
    {
        private static readonly object DoWorkKey = new object();
        private static readonly object RunWorkerCompletedKey = new object();
        private volatile bool _cancellationPending;
        private volatile bool _isRunning;
        private AsyncOperation _asyncOperation;
        private readonly WorkerThreadStartDelegate _threadStart;
        private readonly SendOrPostCallback _operationCompleted;

        [Browsable(false)]
        public bool CancellationPending
        {
            get
            {
                return this._cancellationPending;
            }
        }

        [Browsable(false)]
        public bool IsBusy
        {
            get
            {
                return _isRunning;
            }
        }

        public event DoWorkEventHandler DoWork
        {
            add
            {
                Events.AddHandler(DoWorkKey, value);
            }
            remove
            {
                Events.RemoveHandler(DoWorkKey, value);
            }
        }

        public event RunWorkerCompletedEventHandler RunWorkerCompleted
        {
            add
            {
                Events.AddHandler(RunWorkerCompletedKey, value);
            }
            remove
            {
                Events.RemoveHandler(RunWorkerCompletedKey, value);
            }
        }

        public BackgroundWorkerSlim()
        {
           _threadStart = new WorkerThreadStartDelegate(WorkerThreadStart);
           _operationCompleted = new SendOrPostCallback(AsyncOperationCompleted);
        }

        private void AsyncOperationCompleted(object arg)
        {
            _isRunning = false;
            _cancellationPending = false;

            OnRunWorkerCompleted((RunWorkerCompletedEventArgs)arg);
        }

        public void CancelAsync()
        {
            _cancellationPending = true;
        }

        protected virtual void OnDoWork(DoWorkEventArgs e)
        {
            var workEventHandler = (DoWorkEventHandler)Events[DoWorkKey];
            if (workEventHandler != null)
                workEventHandler((object)this, e);
        }

        protected virtual void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            var completedEventHandler = (RunWorkerCompletedEventHandler)this.Events[RunWorkerCompletedKey];
            if (completedEventHandler != null)
                completedEventHandler((object)this, e);
        }

        public void RunWorkerAsync()
        {
            RunWorkerAsync(null);
        }

        public void RunWorkerAsync(object argument)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("BackgroundWorkerSlim already running");
            }

            _isRunning = true;
            _cancellationPending = false;
            _asyncOperation = AsyncOperationManager.CreateOperation(null);
            _threadStart.BeginInvoke(argument, null, null);
        }

        private void WorkerThreadStart(object argument)
        {
            object result = null;
            Exception error = null;
            var cancelled = false;
            try
            {
                var e = new DoWorkEventArgs(argument);
                OnDoWork(e);
                if (e.Cancel)
                    cancelled = true;
                else
                    result = e.Result;
            }
            catch (Exception ex)
            {
                error = ex;
            }

            _asyncOperation.PostOperationCompleted(_operationCompleted, new RunWorkerCompletedEventArgs(result, error, cancelled));

            //we need to set that this operation is completed now, not when the event fires.
            //this is because if there's a thread blocking the event call, then it appears as though this background worker never completes.
            _isRunning = false;
            _cancellationPending = false;
        }

        private delegate void WorkerThreadStartDelegate(object argument);
    }
}
