using System.Data;
using Orm.Core.SqlStore;

namespace Orm.Core.Interfaces
{
    public interface ISqlBasedStore : IDataStore
    {
        ConnectionBehavior ConnectionBehavior { get; set; }
        string[] GetTableNames();
        int ExecuteNonQuery(string sql);
        object ExecuteScalar(string sql);
        IDataReader ExecuteReader(string sql);
        void CloseReader();
        void CompactDatabase();
    }
}
