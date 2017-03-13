namespace Orm.Core.Interfaces
{
    public interface ITableBasedStore : IDataStore
    {
        string[] GetTableNames();
        bool TableExists(string tableName);
        void TruncateTable(string tableName);
        void DropTable(string tableName);
    }
}
