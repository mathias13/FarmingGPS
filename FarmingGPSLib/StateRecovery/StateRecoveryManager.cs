using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace FarmingGPSLib.StateRecovery
{
    public class StateRecoveryManager: IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IList<IStateObject> _objectsToPreserve = new List<IStateObject>();

        private IDictionary<Type, object> _objectsRecovered = new Dictionary<Type, object>();

        private string _folderPath;

        private object _syncobject = new object();

        private bool _preserveThreadStopped = false;

        private Thread _preserveThread;

        private TimeSpan _preserveInterval;

        public StateRecoveryManager(TimeSpan preserveInterval)
        {
            FileInfo execFile = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            _folderPath = execFile.DirectoryName + @"\State\";
            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);

            TextReader reader = null;
            try
            {
                List<string> files = new List<string>(Directory.GetFiles(_folderPath));
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Name.Contains(".old"))
                        if (files.Contains(file.Replace(".old", ".xml")))
                            continue;

                    Type restoredType = Type.GetType(fileInfo.Name.Remove(fileInfo.Name.Length - 4, 4));
                    IStateObject stateObject = Activator.CreateInstance(restoredType) as IStateObject;
                    XmlSerializer serializer = new XmlSerializer(stateObject.StateType);
                    reader = new StreamReader(_folderPath + fileInfo.Name);
                    _objectsRecovered.Add(new KeyValuePair<Type, object>(restoredType, serializer.Deserialize(reader)));
                }
            }
            catch (Exception e)
            {
                Log.Error("Failed to restore StateObjects", e);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            _preserveInterval = preserveInterval;
            _preserveThread = new Thread(new ThreadStart(PreserveThread));
            _preserveThread.Start();
        }

        public void AddStateObject(IStateObject stateObject)
        {
            lock (_syncobject)
                _objectsToPreserve.Add(stateObject);
        }

        public void RemoveStateObject(IStateObject stateObject)
        {
            lock (_syncobject)
            {
                string objectName = stateObject.GetType().Name;
                try
                {
                    File.Delete(_folderPath + objectName + ".old");
                    File.Delete(_folderPath + objectName + ".xml");
                    _objectsToPreserve.Remove(stateObject);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to remove StateObject " + objectName, e);
                }
            }
        }

        public IDictionary<Type, object> ObjectsRecovered
        {
            get { return _objectsRecovered; }
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
                        {
                            Type stateType = stateObject.GetType();
                            string objectName = stateType.FullName;
                            TextWriter writer = null;
                            try
                            {
                                File.Delete(_folderPath + objectName + ".old");
                                if (File.Exists(_folderPath + objectName + ".xml"))
                                    File.Move(_folderPath + objectName + ".xml", _folderPath + objectName + ".old");

                                XmlSerializer serializer = new XmlSerializer(stateObject.StateType);
                                writer = new StreamWriter(_folderPath + objectName + ".xml", false);
                                serializer.Serialize(writer, stateObject.StateObject);
                            }
                            catch (Exception e)
                            {
                                Log.Error("Failed to preserve object " + objectName, e);
                            }
                            finally
                            {
                                if (writer != null)
                                    writer.Close();
                            }
                        }
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
