﻿using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Data.Linq;
using System.Threading;

namespace FarmingGPS.Database
{
    public class DatabaseOnlineChangedEventArgs : EventArgs
    {
        private bool _online;

        public DatabaseOnlineChangedEventArgs(bool online)
        {
            _online = online;
        }

        public bool Online
        {
            get { return _online; }
        }
    }

    public class DatabaseHandlerExceptionEventArgs : EventArgs
    {
        private Exception _exception;

        private string _method;

        public DatabaseHandlerExceptionEventArgs(Exception exception, string method)
        {
            _exception = exception;
            _method = method;
        }

        public Exception Exception
        {
            get { return _exception; }
        }

        public string Method
        {
            get { return _method; }
        }
    }

    public class DatabaseHandler : IDisposable
    {
        #region Private Variables

        private FarmingGPSDataContext _databaseContext;

        private bool _online = false;

        private Thread _onlineCheckerThread;

        private bool _stopThread = false;

        private object _syncObject = new object();

        #endregion

        #region Events

        public event EventHandler<DatabaseOnlineChangedEventArgs> DatabaseOnlineChanged;

        public event EventHandler<DatabaseHandlerExceptionEventArgs> DatabaseHandlerException;

        #endregion

        public DatabaseHandler(SqlConnectionStringBuilder connection)
        {
            _databaseContext = new FarmingGPSDataContext(connection.ConnectionString);
            _databaseContext.Connection.StateChange += Connection_StateChange;
            _onlineCheckerThread = new Thread(new ThreadStart(OnlineCheckThread));
            _onlineCheckerThread.Start();
        }

        public void Dispose()
        {
            _stopThread = true;
            _onlineCheckerThread.Join();
        }

        #region Public Methods

        public bool SubmitToDatabase()
        {
            try
            {
                lock (_syncObject)
                    _databaseContext.SubmitChanges();
                return true;
            }
            catch(Exception e)
            {
                OnDatabaseHandlerException(e, "SubmitToDatabase");
                return false;
            }
        }

        public bool SetCoordinatesOrder(GpsCoordinate[] coordinates, int fieldId)
        {
            lock (_syncObject)
            {
                if (SubmitToDatabase())
                {
                    for (int i = 0; i < coordinates.Length; i++)
                    {
                        var queryResult = from boundaryCoordinates in _databaseContext.FieldBoundaries
                                          where boundaryCoordinates.FieldId == fieldId
                                          select boundaryCoordinates;
                        FieldBoundary boundaryCoordinate = queryResult.First(coordinate => coordinate.PosId == coordinates[i].PosId);
                        boundaryCoordinate.OrderId = i;
                    }
                    return SubmitToDatabase();
                }
                else
                    return false;
            }
        }

        public void UndoChanges(Type[] tablesToUndo)
        {
            ChangeSet changes = _databaseContext.GetChangeSet();
            foreach (Type table in tablesToUndo)
            {
                foreach (object insert in changes.Inserts)
                    if (insert.GetType() == table)
                        _databaseContext.GetTable(insert.GetType()).DeleteOnSubmit(insert);

                foreach (object deletes in changes.Deletes)
                    if (deletes.GetType() == table)
                        _databaseContext.GetTable(deletes.GetType()).DeleteOnSubmit(deletes);
            }
        }

        public void AddField(Field field)
        {
            _databaseContext.Fields.InsertOnSubmit(field);
        }

        public void DeleteField(Field field)
        {
            var queryResult = from coordinate in _databaseContext.GpsCoordinates
                              join fieldPar in _databaseContext.FieldBoundaries on coordinate.PosId equals fieldPar.PosId
                              where fieldPar.FieldId == field.FieldId
                              select coordinate;
            _databaseContext.GpsCoordinates.DeleteAllOnSubmit(queryResult);
            _databaseContext.Fields.DeleteOnSubmit(field);
        }

        public void AddFieldBoundary(FieldBoundary boundary)
        {
            if (boundary.GpsCoordinate == null)
                throw new NullReferenceException("Coordinate of boundary needs to be set");

            _databaseContext.GpsCoordinates.InsertOnSubmit(boundary.GpsCoordinate);
            _databaseContext.FieldBoundaries.InsertOnSubmit(boundary);
        }

        public void DeleteFieldboundary(GpsCoordinate coordinate)
        {
            _databaseContext.GpsCoordinates.DeleteOnSubmit(coordinate);
            _databaseContext.FieldBoundaries.DeleteOnSubmit(coordinate.FieldBoundaries.First());
        }

        public void AddInstersect(SubFieldIntersect intersect)
        {
            _databaseContext.SubFieldIntersects.InsertOnSubmit(intersect);
        }

        public void DeleteIntersect(SubFieldIntersect intersect)
        {
            _databaseContext.SubFieldIntersects.DeleteOnSubmit(intersect);
        }

        public void AddVechile(Vechile vechile)
        {
            _databaseContext.Vechiles.InsertOnSubmit(vechile);
        }

        public void DeleteVechile(Vechile vechile)
        {
            _databaseContext.Vechiles.DeleteOnSubmit(vechile);
        }

        public void AddEquipment(Equipment equipment)
        {
            _databaseContext.Equipments.InsertOnSubmit(equipment);
        }

        public void DeleteEquipment(Equipment equipment)
        {
            _databaseContext.Equipments.DeleteOnSubmit(equipment);
        }

        public Field[] GetFields()
        {
            try
            {
                lock (_syncObject)
                {
                    var queryResult = from field in _databaseContext.Fields
                                      where field.ParentField == null
                                      select field;
                    return queryResult.ToArray();
                }
            }
            catch(Exception e)
            {
                OnDatabaseHandlerException(e, "GetFields");
                return null;
            }            
        }

        public SubFieldIntersect[] GetIntersects(int fieldId)
        {
            try
            {
                lock (_syncObject)
                {
                    var queryResult = from intersects in _databaseContext.SubFieldIntersects
                                      where intersects.FieldId == fieldId
                                      select intersects;
                    return queryResult.ToArray();
                }
            }
            catch (Exception e)
            {
                OnDatabaseHandlerException(e, "GetIntersects");
                return null;
            }
        }

        public GpsCoordinate[] GetCoordinatesForField(int fieldId)
        {
            try
            {
                lock (_syncObject)
                {
                    var queryResult = from coordinate in _databaseContext.GpsCoordinates
                                      join field in _databaseContext.FieldBoundaries on coordinate.PosId equals field.PosId
                                      orderby field.OrderId ascending
                                      where field.FieldId == fieldId
                                      select coordinate;
                    return queryResult.ToArray();
                }
            }
            catch (Exception e)
            {
                OnDatabaseHandlerException(e, "GetCoordinatesForField");
                return null;
            }
        }

        public Equipment[] GetEquipments()
        {
            try
            {
                lock (_syncObject)
                {
                    var queryResult = from equipment in _databaseContext.Equipments
                                      select equipment;
                    return queryResult.ToArray();
                }
            }
            catch (Exception e)
            {
                OnDatabaseHandlerException(e, "GetEquipments");
                return null;
            }
        }

        public Vechile[] GetVechiles()
        {
            try
            {
                lock (_syncObject)
                {
                    var queryResult = from vechile in _databaseContext.Vechiles
                                      select vechile;
                    return queryResult.ToArray();
                }
            }
            catch (Exception e)
            {
                OnDatabaseHandlerException(e, "GetVechiles");
                return null;
            }
        }

        public VechileAttach[] GetAttachPoints(int vechileId)
        {
            try
            {
                lock (_syncObject)
                {
                    var queryResult = from vechileAttach in _databaseContext.VechileAttaches
                                      where vechileAttach.VechileId == vechileId
                                      select vechileAttach;
                    return queryResult.ToArray();
                }
            }
            catch (Exception e)
            {
                OnDatabaseHandlerException(e, "GetIntersects");
                return null;
            }
        }

        #endregion

        private void Connection_StateChange(object sender, System.Data.StateChangeEventArgs e)
        {
            if(e.CurrentState == ConnectionState.Broken || e.CurrentState == ConnectionState.Closed)
                OnDatabaseOnlineChanged(false);
        }

        private void OnlineCheckThread()
        {
            DateTime nextConnectionCheck = DateTime.MinValue;
            while(!_stopThread)
            {
                if (DateTime.Now > nextConnectionCheck)
                {
                    try
                    {
                        lock (_syncObject)
                        {
                            _databaseContext.Connection.Close();
                            _databaseContext.Connection.Open();
                            if (_databaseContext.Connection.State == ConnectionState.Open)
                                OnDatabaseOnlineChanged(true);
                            else
                                OnDatabaseOnlineChanged(true);
                        }
                    }
                    catch (Exception e)
                    {
                        OnDatabaseHandlerException(e, "OnlineCheckThread");
                    }
                    nextConnectionCheck = DateTime.Now.AddSeconds(_online ? 120.0 : 30.0);
                }

                Thread.Sleep(10);
            }
        }

        protected void OnDatabaseOnlineChanged(bool online)
        {
            _online = online;
            if (DatabaseOnlineChanged != null)
                DatabaseOnlineChanged.Invoke(this, new DatabaseOnlineChangedEventArgs(online));
        }

        protected void OnDatabaseHandlerException(Exception exception, string method)
        {
            if (DatabaseHandlerException != null)
                DatabaseHandlerException.Invoke(this, new DatabaseHandlerExceptionEventArgs(exception, method));
        }
    }
}