using System.Data;
using SmartWay.Orm.Entity.Constraints;
using SmartWay.Orm.Sql;
using SmartWay.Orm.Sql.Schema;

namespace SmartWay.Orm.Sqlite
{
    public class SqliteSchemaChecker : SchemaChecker
    {
        public SqliteSchemaChecker(ISqlDataStore sqlDataStore)
            : base(sqlDataStore)
        {
        }


        public TableDefinition GetTableFormat(string entityName)
        {
            var sql = $"PRAGMA table_info([{entityName}])";

            using var reader = SqlDataStore.ExecuteReader(sql);
            {
                TableDefinition tableFormat = null;
                while (reader.Read())
                {
                    if (tableFormat == null)
                        tableFormat = new TableDefinition(entityName);

                    var columnFormat = GetColumnFormat(reader);
                    tableFormat.AddColumn(columnFormat);
                }

                return tableFormat;
            }
        }

        private static ColumnDefinition GetColumnFormat(IDataReader reader)
        {
            return new ColumnDefinition
            {
                Ordinal = reader.GetInt32(0),
                ColumnName = reader.GetString(1),
                DbType = reader.GetString(2),
                IsNullable = !reader.GetBoolean(3)
            };
        }

        protected override bool IsPrimaryKeyExist(PrimaryKey primaryKey)
        {
            return IsPrimaryKeyExist(primaryKey.Entity.GetNameInStore());
        }

        public bool IsPrimaryKeyExist(string entityName)
        {
            var connection = SqlDataStore.GetReadConnection();
            using var command = connection.CreateCommand();
            var sql = $"PRAGMA table_info([{entityName}])";
            command.CommandText = sql;

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                    if (reader.GetBoolean(5))
                        return true;
            }

            return false;
        }

        protected override bool IsForeignKeyExist(ForeignKey foreignKey)
        {
            return IsForeignKeyExist(foreignKey.Entity.GetNameInStore(), foreignKey.ForeignEntityInfo.GetNameInStore());
        }

        public bool IsForeignKeyExist(string entityName, string entityRef)
        {
            var connection = SqlDataStore.GetReadConnection();
            using var command = connection.CreateCommand();
            var sql = $"PRAGMA foreign_key_list([{entityName}])";
            command.CommandText = sql;

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.GetString(2) != entityRef)
                        continue;

                    return true;
                }
            }

            return false;
        }

        protected override bool IsIndexExist(Index index)
        {
            return IsIndexExist(index.GetNameInStore()) != 0;
        }

        public int IsIndexExist(string indexName)
        {
            var connection = SqlDataStore.GetReadConnection();
            using var command = connection.CreateCommand();
            var sql = $"PRAGMA index_info({indexName})";
            command.CommandText = sql;

            using var reader = command.ExecuteReader();
            var count = 0;
            while (reader.Read()) count++;
            return count;
        }

        protected override void CreatePrimaryKey(PrimaryKey primaryKey)
        {
            // Not supported by sqlite
            // https://www.sqlite.org/omitted.html
        }

        protected override void CreateForeignKey(ForeignKey foreignKey)
        {
            // Not supported by sqlite
            // https://www.sqlite.org/omitted.html
        }
    }
}