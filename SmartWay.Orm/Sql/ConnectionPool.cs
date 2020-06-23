using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SmartWay.Orm.Sql
{
    public class ConnectionPool : DisposableBase, IConnectionPool
    {
        private readonly IDbEngine _dbEngine;
        private readonly List<IDbConnection> _pool;

        public ConnectionPool(IDbEngine dbEngine)
        {
            _dbEngine = dbEngine;
            _pool = new List<IDbConnection>();
            ConnectionPoolSize = 20;
        }

        public int ConnectionPoolSize { get; set; }

        public IDbConnection GetConnection()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("ConnectionPool");

            lock (_pool)
            {
                IDbConnection connection;

                do
                {
                    connection = GetFreeConnectionInPool();

                    if (connection != null)
                    {
                        if (Open(connection))
                            return connection;

                        // Broken connection, maybe disposed
                        connection.Dispose();
                        _pool.Remove(connection);
                    }

                    if (_pool.Count < ConnectionPoolSize)
                    {
                        connection = _dbEngine.GetNewConnection();
                        connection.Open();
                        _pool.Add(connection);
                        OrmDebug.Trace("Creating pooled connection");
                        return connection;
                    }

                    OrmDebug.Trace("Pool full waiting for free connection");
                    Thread.Sleep(1000);

                    // TODO: add a timeout?
                } while (connection == null);

                throw new TimeoutException("Unable to get a pooled connection.");
            }
        }

        public void CloseConnections()
        {
            if (_pool == null)
                return;

            foreach (var connection in _pool)
            {
                if (connection.State != ConnectionState.Closed) connection.Close();
                connection.Dispose();
            }

            _pool.Clear();
        }

        private IDbConnection GetFreeConnectionInPool()
        {
            return _pool.FirstOrDefault(IsFreeForUse);
        }

        protected override void ReleaseManagedResources()
        {
            base.ReleaseManagedResources();
            try
            {
                foreach (var connection in _pool)
                {
                    if (connection.State != ConnectionState.Closed) connection.Close();
                    connection.Dispose();
                }

                _pool.Clear();
            }
            catch (Exception ex)
            {
                OrmDebug.Info(ex.Message);
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
        }

        private static bool Open(IDbConnection connection)
        {
            if (connection == null)
                return false;

            // make sure the connection is open (in the event we has some network condition that closed it, etc.)
            if (connection.State == ConnectionState.Open)
                return true;

            try
            {
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsFreeForUse(IDbConnection connection)
        {
            return connection.State != ConnectionState.Executing
                   && connection.State != ConnectionState.Fetching;
        }
    }
}