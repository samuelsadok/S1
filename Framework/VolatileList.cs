using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppInstall.Framework
{
    /// <summary>
    /// Represents a list of objects that are volatile.
    /// </summary>
    public class VolatileList<T>
    {
        private readonly TimeSpan timespan;
        private Dictionary<T, DateTime> objects = new Dictionary<T,DateTime>(); // contains every live object and it's time of death
        private AutoResetEvent touchedObject = new AutoResetEvent(false); // triggerd to signal the monitoring task that an object has been touched (in case it was asleep)

        /// <summary>
        /// Triggered when a new object is touched for the first time (or after having been lost).
        /// </summary>
        public event EventHandler<Tuple<T, object[]>> FoundObject;
        /// <summary>
        /// Triggered when a volatile object is touched (including the first time)
        /// </summary>
        public event EventHandler<Tuple<T, object[]>> TouchedObject;
        /// <summary>
        /// Triggered when an object was not touched for the lifespan associated with this list.
        /// </summary>
        public event EventHandler<T> LostObject;


        public VolatileList(TimeSpan objectLifespan)
        {
            this.timespan = objectLifespan;
        }

        /// <summary>
        /// Starts monitoring the objects in the list. Must be called before any other function. Must only be called once.
        /// </summary>
        public void StartMonitoring(CancellationToken cancellationToken)
        {
            Task.Run(() => {
                TimeSpan nextWaitTime;
                do {
                    nextWaitTime = TimeSpan.MaxValue;
                    List<T> deprecatedObjects = new List<T>();

                    lock (objects) {
                        // determine which objects are deprecated and how long to wait for the next monitoring step
                        foreach (var kv in objects) {
                            if (kv.Value != DateTime.MaxValue) {
                                DateTime now = DateTime.Now;
                                if (kv.Value <= now)
                                    deprecatedObjects.Add(kv.Key);
                                else if (kv.Value - now < nextWaitTime)
                                    nextWaitTime = kv.Value - now;
                            }
                        }

                        // remove deprecated elements
                        foreach (var obj in deprecatedObjects) {
                            objects.Remove(obj);
                            LostObject.SafeInvoke(this, obj);
                        }
                    }
                } while (nextWaitTime == TimeSpan.MaxValue ? (WaitHandle.WaitAny(new WaitHandle[] { cancellationToken.WaitHandle, touchedObject }) == 1) : !cancellationToken.WaitHandle.WaitOne(nextWaitTime));
            });
        }


        /// <summary>
        /// Renews the life of an object
        /// </summary>
        public void Touch(T obj, params object[] parameters)
        {
            lock (objects) {
                if (!objects.ContainsKey(obj))
                    FoundObject.SafeInvoke(this, new Tuple<T, object[]>(obj, parameters));

                if (!IsResilent(obj))
                    objects[obj] = DateTime.Now + timespan;

                TouchedObject.SafeInvoke(this, new Tuple<T, object[]>(obj, parameters));
            }

            touchedObject.Set();
        }

        /// <summary>
        /// Gives an object infinite lifespan
        /// </summary>
        public void MakeResilent(T obj)
        {
            lock (objects)
                objects[obj] = DateTime.MaxValue;
        }

        /// <summary>
        /// Makes a resilent object volatile again
        /// </summary>
        public void MakeVolatile(T obj)
        {
            lock (objects)
                objects[obj] = DateTime.Now + timespan;
        }

        /// <summary>
        /// Returns true if the specified object is resilent, returns false if it is volatile or not registred
        /// </summary>
        public bool IsResilent(T obj)
        {
            DateTime t;
            lock (objects)
                if (!objects.TryGetValue(obj, out t)) return false;
            return t == DateTime.MaxValue;
        }

        /// <summary>
        /// Removes all objects from the device. LostObject will be triggered for each object.
        /// </summary>
        public void Clear()
        {
            lock (objects) {
                foreach (var obj in objects.Keys)
                    LostObject.SafeInvoke(this, obj);
                objects.Clear();
            }
        }
    }
}