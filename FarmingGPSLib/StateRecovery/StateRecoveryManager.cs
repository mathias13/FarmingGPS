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

            try
            {
                var filesToUse = new List<string>();
                List<string> files = new List<string>(Directory.GetFiles(_folderPath));
                foreach (string file in files)
                {
                    if (filesToUse.Contains(file))
                        continue;
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Name.Contains(".old"))
                    {
                        var xmlFile = file.Replace(".old", ".xml");
                        if(File.Exists(xmlFile))
                            File.Delete(xmlFile);

                        File.Move(file, xmlFile);

                        if (!files.Contains(xmlFile))
                            files.Add(file);
                    }
                    else
                        filesToUse.Add(file);
                }

                foreach(var fileToUse in filesToUse)
                {
                    FileInfo fileInfo = new FileInfo(fileToUse);
                    Type restoredType = Type.GetType(fileInfo.Name.Remove(fileInfo.Name.Length - 4, 4));
                    IStateObject stateObject = Activator.CreateInstance(restoredType) as IStateObject;
                    XmlSerializer serializer = new XmlSerializer(stateObject.StateType);
                    using (var reader = new StreamReader(_folderPath + fileInfo.Name))
                        _objectsRecovered.Add(new KeyValuePair<Type, object>(restoredType, serializer.Deserialize(reader)));
                }
            }
            catch (Exception e)
            {
                Log.Error("Failed to restore StateObjects", e);
            }

            _preserveInterval = preserveInterval;
            _preserveThread = new Thread(new ThreadStart(PreserveThread));
            _preserveThread.Priority = ThreadPriority.Lowest;
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
                if (stateObject == null)
                    return;
                string objectName = stateObject.GetType().FullName;
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

        public void Clear()
        {
            lock (_syncobject)
                while (_objectsToPreserve.Count > 0)
                    RemoveStateObject(_objectsToPreserve[0]);

            foreach (string file in Directory.GetFiles(_folderPath))
                File.Delete(file);
        }

        public IDictionary<Type, object> ObjectsRecovered
        {
            get { return _objectsRecovered; }
        }

        public KeyValuePair<Type, object>? GetRecoveredObjectDerivedFrom(Type type)
        {
            foreach (KeyValuePair<Type, object> objectRecovered in _objectsRecovered)
                if (type.IsAssignableFrom(objectRecovered.Key))
                    return objectRecovered;
            return null;
        }

        private void PreserveThread()
        {
            DateTime nextPreserve = DateTime.Now.Add(_preserveInterval);
            while(!_preserveThreadStopped)
            {
                if (nextPreserve < DateTime.Now)
                {
                    lock (_syncobject)
                        Preserve();
                    nextPreserve = DateTime.Now.Add(_preserveInterval);
                }
                else
                    Thread.Sleep(500);
            }
        }

        private void Preserve()
        {
            foreach (IStateObject stateObject in _objectsToPreserve)
            {
                if (!stateObject.HasChanged)
                    continue;
                string objectName = stateObject.GetType().FullName;
                TextWriter writer = null;
                try
                {
                    File.Delete(_folderPath + objectName + ".old");
                    if (File.Exists(_folderPath + objectName + ".xml"))
                        File.Move(_folderPath + objectName + ".xml", _folderPath + objectName + ".old");

                    XmlSerializer serializer = new XmlSerializer(stateObject.StateType);
                    writer = new StreamWriter(_folderPath + objectName + ".xml", false);
                    serializer.Serialize(writer, stateObject.StateObject);
                    writer.Close();
                    File.Delete(_folderPath + objectName + ".old");
                }
                catch (Exception e)
                {
                    Log.Error("Failed to preserve object " + objectName, e);
                    if (writer != null)
                        writer.Close();
                    File.Delete(_folderPath + objectName + ".xml");
                }
            }
        }

        public void Dispose()
        {
            _preserveThreadStopped = true;
            _preserveThread.Join();
            Preserve();
        }
    }
}
