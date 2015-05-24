using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppInstall.Framework
{

    /// <summary>
    /// Allows an action to be triggered in a lazy fashion.
    /// The action is guaranteed not to be executed several times concurrently.
    /// When the action is triggered, it is guaranteed to execute at least once AFTER being triggered.
    /// It is NOT guaranteed to execute as often as it is triggered.
    /// </summary>
    public class SlowAction
    {
        private readonly Action<CancellationToken> action;
        private readonly object lockRef = new object();
        private EventWaitHandle executionFinishedHandle; // a non-null value in this field indicates that another execution of the action must be done
        private bool executing;

        public ActivityTracker Tracker { get; private set; }

        public SlowAction(Action<CancellationToken> action)
        {
            if (action == null) throw new ArgumentNullException("action");

            Tracker = new ActivityTracker();

            this.action = (c) => {
                Tracker.SwitchToActive();
                try {
                    action(c);
                } catch (Exception ex) {
                    Tracker.SwitchToFailed(ex);
                    return;
                }
                Tracker.SwitchToSucceeded();
            };
        }

        /// <summary>
        /// Determines if the execution handler thread should be launched
        /// </summary>
        /// <param name="waitHandle">set to the wait handle that is triggered upon completition of the execution</param>
        /// <returns></returns>
        private bool ShouldExecute(out WaitHandle waitHandle)
        {
            lock (lockRef) {
                if (executionFinishedHandle == null)
                    executionFinishedHandle = new ManualResetEvent(false);
                waitHandle = executionFinishedHandle;
                if (!executing) {
                    executing = true;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// determines if another execution is required in which case it returns the wait handle that waits for the execution to complete
        /// </summary>
        private EventWaitHandle AcquireHandleForExecution()
        {
            EventWaitHandle result;
            lock (lockRef) {
                if ((result = executionFinishedHandle) == null)
                    executing = false;
                executionFinishedHandle = null;
                return result;
            }
        }

        /// <summary>
        /// Triggers the underlying action and returns a wait handle that will be triggered upon completition of the first execution of the action that was started after triggering.
        /// </summary>
        public WaitHandle Trigger(CancellationToken cancellationToken)
        {
            WaitHandle result;
            if (ShouldExecute(out result))
                Task.Run(() => {
                    EventWaitHandle handle;
                    while ((handle = AcquireHandleForExecution()) != null) {
                        action(cancellationToken);
                        handle.Set();
                    }
                });
            return result;
        }

        /// <summary>
        /// Triggers the underlying action and blocks until it was completed.
        /// </summary>
        /// <param name="cancellationToken">causes the routine to stop waiting but does not revoke or cancel the triggered action</param>
        public async Task TriggerAndWait(CancellationToken cancellationToken)
        {
            await Trigger(cancellationToken).WaitAsync(cancellationToken); // todo: propagate errors from this triggering
        }

        /// <summary>
        /// Starts a new task that triggers the action at the specified interval.
        /// That means that the action is executed at most at the specified frequency.
        /// Periodic triggering can be used in combination with explicit triggering.
        /// </summary>
        /// <param name="interval">interval in milliseconds</param>
        /// <param name="cancellationToken">Cancels the periodic triggering.</param>
        public void TriggerPeriodically(int interval, CancellationToken cancellationToken)
        {
            Task.Run(() => {
                do {
                    Trigger(cancellationToken);
                } while (!cancellationToken.WaitHandle.WaitOne(interval));
            });
        }
    }
}