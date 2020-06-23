using System;
using System.Data;

namespace SmartWay.Orm.Sql
{
    public interface IConnectionPool : IDisposable
    {
        /// <summary>
        ///     Get free to use connection in pool
        /// </summary>
        /// <returns>Free to use connection</returns>
        IDbConnection GetConnection();

        /// <summary>
        ///     Close all connection in pool
        /// </summary>
        void CloseConnections();
    }
}