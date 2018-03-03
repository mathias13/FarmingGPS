using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FarmingGPS.StateRecovery
{
    public class StateRecoveryManager: IDisposable
    {
        private IList<IStateObject> _objectsToPreserve = new List<IStateObject>();

        private IList<object> _objectsRecovered;

        private object _syncobject = new object();

        private bool _preserveThreadStopped = false;

        private Thread _preserveThread;

        private TimeSpan _preserveInterval;

        public StateRecoveryManager(TimeSpan preserveInterval)
        {
            _preserveInterval = preserveInterval;
            _preserveThread = new Thread(new ThreadStart(PreserveThread));
            _preserveThread.Start();
        }

        public void AddStateObject(IStateObject stateObject)
        {
            lock (_syncobject)
                _objectsToPreserve.Add(stateObject);
        }

        private void PreserveThread()
        {
            DateTime nextPreserve = DateTime.Now.Add(_preserveInterval);
            while(!_preserveThreadStopped)
            {
                if (nextPreserve < DateTime.Now)
                {
                    lock(_syncobject)
                    {
                        foreach (IStateObject stateObject in _objectsToPreserve)
                            ;
                    }
                    nextPreserve = DateTime.Now.Add(_preserveInterval);
                }
                else
                    Thread.Sleep(1);
            }
        }

        public void Dispose()
        {
            _preserveThreadStopped = true;
            _preserveThread.Join();
        }
    }
}
