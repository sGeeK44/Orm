using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SmartWay.Orm.Caches;
using SmartWay.Orm.Entity.Constraints;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Queries;
using SmartWay.Orm.Sql.Queries;
using SmartWay.Orm.Sql.Schema;
using Index = SmartWay.Orm.Entity.Constraints.Index;

namespace SmartWay.Orm.Sql
{
    public class SqlDataStore : DataStore, ISqlDataStore
    {
        private static readonly string[] SqlReserved =
        {
            "IDENTITY", "ENCRYPTION", "ORDER", "ADD", "END", "OUTER", "ALL", "ERRLVL", "OVER", "ALTER", "ESCAPE",
            "PERCENT", "AND", "EXCEPT", "PLAN", "ANY", "EXEC", "PRECISION", "AS", "EXECUTE", "PRIMARY", "ASC",
            "EXISTS", "PRINT", "AUTHORIZATION", "EXIT", "PROC", "AVG", "EXPRESSION", "PROCEDURE", "BACKUP", "FETCH",
            "PUBLIC", "BEGIN", "FILE", "RAISERROR", "BETWEEN", "FILLFACTOR", "READ", "BREAK", "FOR", "READTEXT",
            "BROWSE", "FOREIGN", "RECONFIGURE", "BULK", "FREETEXT", "BY", "FREETEXTTABLE", "REPLICATION", "CASCADE",
            "FROM", "RESTORE", "CASE", "FULL", "RESTRICT", "CHECK", "FUNCTION", "RETURN", "CHECKPOINT",
            "GOTO", "REVOKE", "CLOSE", "GRANT", "RIGHT", "CLUSTERED", "GROUP", "ROLLBACK", "COALESCE", "HAVING",
            "ROWCOUNT", "COLLATE", "HOLDLOCK", "ROWGUIDCOL", "COLUMN", "IDENTITY", "RULE",
            "COMMIT", "IDENTITY_INSERT", "SAVE", "COMPUTE", "IDENTITYCOL", "SCHEMA", "CONSTRAINT", "IF", "SELECT",
            "CONTAINS", "IN", "SESSION_USER", "CONTAINSTABLE", "SET", "CONTINUE", "INNER", "SETUSER",
            "CONVERT", "INSERT", "SHUTDOWN", "COUNT", "INTERSECT", "SOME", "CREATE", "INTO", "STATISTICS", "CROSS",
            "IS", "SUM", "CURRENT", "JOIN", "SYSTEM_USER", "CURRENT_DATE", "TABLE", "CURRENT_TIME", "KILL",
            "TEXTSIZE", "CURRENT_TIMESTAMP", "LEFT", "THEN", "CURRENT_USER", "LIKE", "TO", "CURSOR", "LINENO", "TOP",
            "DATABASE", "LOAD", "TRAN", "DATABASEPASSWORD", "MAX", "TRANSACTION", "DATEADD", "MIN", "TRIGGER",
            "DATEDIFF", "NATIONAL", "TRUNCATE", "DATENAME", "NOCHECK", "TSEQUAL", "DATEPART", "NONCLUSTERED", "UNION",
            "DBCC", "NOT", "DEALLOCATE", "NULL", "UPDATE", "DECLARE", "NULLIF", "UPDATETEXT",
            "DEFAULT", "OF", "USE", "DELETE", "OFF", "USER", "DENY", "OFFSETS", "VALUES", "DESC", "ON", "VARYING",
            "DISK", "OPEN", "VIEW", "DISTINCT", "OPENDATASOURCE", "WAITFOR", "DISTRIBUTED", "OPENQUERY", "WHEN",
            "DOUBLE", "OPENROWSET", "WHERE", "DROP", "OPENXML", "WHILE", "DUMP", "OPTION", "WITH", "ELSE", "OR",
            "WRITETEXT"
        };

        private readonly IDbAccessStrategy _dbAccessStrategy;
        private readonly IDbEngine _dbEngine;
        private readonly IConnectionPool _readConnectionPool;
        private readonly ISchemaChecker _schemaChecker;
        private readonly ISqlFactory _sqlFactory;
        private readonly object _transactionSyncRoot = new object();
        private readonly IConnectionPool _writeConnectionPool;

        public SqlDataStore(IDbEngine dbEngine, ISqlFactory sqlFactory)
        {
            _dbEngine = dbEngine;
            _sqlFactory = sqlFactory;
            _dbAccessStrategy = _sqlFactory.CreateDbAccessStrategy(this);
            FieldPropertyFactory = _sqlFactory.CreateFieldPropertyFactory();
            _schemaChecker = _sqlFactory.CreateSchemaChecker(this);
            _readConnectionPool = new ConnectionPool(dbEngine) {ConnectionPoolSize = 20};
            _writeConnectionPool = new ConnectionPool(dbEngine) {ConnectionPoolSize = 1};
        }

        public override IFieldPropertyFactory FieldPropertyFactory { get; }

        public override string Name => _dbEngine.Name;

        protected string[] ReservedWords => SqlReserved;


        public IDbTransaction CurrentTransaction { get; set; }

        public override ISqlFactory SqlFactory => _sqlFactory;
        public override IEntityCache Cache { get; set; }

        public override bool StoreExists => _dbEngine.DatabaseExists;

        public void Compact()
        {
            _dbEngine.Compact();
        }

        public void Shrink()
        {
            _dbEngine.Shrink();
        }

        public void Optimize()
        {
            Compact();
            Shrink();
        }

        public void DropAllIndexes(IDbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT TABLE_NAME, INDEX_NAME FROM information_schema.indexes where primary_key = 'FALSE'";
            var transaction = connection.BeginTransaction();
            command.Transaction = transaction;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read()) DropIndex(connection, transaction, reader.GetString(0), reader.GetString(1));
            }

            transaction.Commit();
        }

        public void DropIndex(IDbConnection connection, IDbTransaction transaction, string tableName, string indexname)
        {
            if (!IndexExists(connection, transaction, tableName, indexname))
                return;

            using var command = connection.CreateCommand();
            command.CommandText = $"DROP INDEX [{tableName}].{indexname}";
            command.Transaction = transaction;
            command.ExecuteNonQuery();
        }

        public bool IndexExists(IDbConnection connection, IDbTransaction transaction, string tableName,
            string indexname)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                $"SELECT COUNT(*) FROM information_schema.indexes WHERE TABLE_NAME = '{tableName}' AND index_name = \'{indexname}\'";
            command.Transaction = transaction;
            return (int) command.ExecuteScalar() != 0;
        }

        /// <summary>
        ///     Deletes the underlying DataStore
        /// </summary>
        public override void DeleteStore()
        {
            _dbEngine.DeleteDatabase();
        }

        /// <summary>
        ///     Creates the underlying DataStore
        /// </summary>
        public override void CreateStore()
        {
            _dbEngine.CreateDatabase();

            var connection = GetWriteConnection();
            foreach (var entity in Entities) CreateTable(connection, entity);
        }

        public override void CloseConnections()
        {
            _readConnectionPool.CloseConnections();
            _writeConnectionPool.CloseConnections();
        }

        public IDbConnection GetReadConnection()
        {
            return CurrentTransaction != null ? CurrentTransaction.Connection : _readConnectionPool.GetConnection();
        }

        public IDbConnection GetWriteConnection()
        {
            return CurrentTransaction != null ? CurrentTransaction.Connection : _writeConnectionPool.GetConnection();
        }

        public int ExecuteNonQuery(string sql)
        {
            using var command = SqlFactory.CreateCommand();
            command.CommandText = sql;
            return ExecuteNonQuery(command);
        }

        public int ExecuteNonQuery(IDbCommand command)
        {
            var connection = GetWriteConnection();
            command.Connection = connection;
            command.Transaction = CurrentTransaction;
            return command.ExecuteNonQuery();
        }

        public object ExecuteScalar(string sql)
        {
            using var command = SqlFactory.CreateCommand();
            command.CommandText = sql;
            command.Transaction = CurrentTransaction;
            return ExecuteScalar(command);
        }

        public void CreateTable(IDbConnection connection, IEntityInfo entity)
        {
            var sql = _dbAccessStrategy.CreateTable(entity);
            using (var command = SqlFactory.CreateCommand())
            {
                command.CommandText = sql;
                command.Connection = connection;
                command.Transaction = CurrentTransaction;
                command.ExecuteNonQuery();
            }

            VerifiyPrimaryKey(entity.PrimaryKey);

            foreach (var foreignKey in entity.ForeignKeys) VerifyForeignKey(foreignKey);

            foreach (var index in entity.Indexes) VerifyIndex(index);
        }

        public void VerifiyPrimaryKey(PrimaryKey primaryKey)
        {
            _schemaChecker.VerifyPrimaryKey(primaryKey);
        }

        public void VerifyForeignKey(ForeignKey foreignKey)
        {
            _schemaChecker.VerifyForeignKey(foreignKey);
        }

        public void VerifyIndex(Index index)
        {
            _schemaChecker.VerifyIndex(index);
        }

        /// <summary>
        ///     Retrieves a single entity instance from the DataStore identified by the specified primary key value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public override T Select<T>(object primaryKey)
        {
            var objectType = typeof(T);
            var entityName = Entities.GetNameForType(objectType);
            var entity = Entities[entityName];
            var field = entity.PrimaryKey;
            return (T) _dbAccessStrategy.SelectByPrimayKey(objectType, field, primaryKey);
        }

        public override IJoinable<TEntity> Select<TEntity>()
        {
            return new Selectable<TEntity>(this, Entities);
        }

        public override IJoinable<TIEntity> Select<TEntity, TIEntity>()
        {
            return new Selectable<TIEntity>(this, Entities, typeof(TEntity));
        }

        public override IJoinable<object> Select(Type objectType)
        {
            return new Selectable<object>(this, Entities, objectType);
        }

        public override IEnumerable<TIEntity> ExecuteQuery<TIEntity>(IClause sqlClause, IEntityBuilder<TIEntity> select)
        {
            var command = BuildCommand(sqlClause);
            return ExecuteReader(command, select);
        }

        public override IEnumerable<AggregableResultRow> ExecuteQuery(IClause sqlAggregateClause)
        {
            var command = BuildCommand(sqlAggregateClause);
            // ReSharper disable once RedundantTypeArgumentsOfMethod // Need for wince.Proj
            return ExecuteCommandReader(command);
        }

        public override IUpdatable<TEntity> Set<TEntity>(string columnName, object value)
        {
            return new Updatable<TEntity>(this).Set(columnName, value);
        }

        public override int ExecuteNonQuery(IClause sqlClause)
        {
            using var command = BuildCommand(sqlClause);
            return ExecuteNonQuery(command);
        }

        public override int ExecuteScalar(IClause sqlClause)
        {
            using var command = BuildCommand(sqlClause);
            var count = ExecuteScalar(command);
            return Convert.ToInt32(count);
        }

        public IEnumerable<TIEntity> ExecuteReader<TIEntity>(IDbCommand command, IEntityBuilder<TIEntity> builder)
            where TIEntity : class
        {
            builder.EntityCache = Cache ?? new EntitiesCache();
            // Need for VS2008 compilation
            // ReSharper disable once RedundantTypeArgumentsOfMethod
            return ExecuteCommandReader<TIEntity>(command, builder.Deserialize, builder.Offset);
        }

        /// <summary>
        ///     Deletes all rows from the specified Table
        /// </summary>
        public void TruncateTable(string tableName)
        {
            var connection = GetWriteConnection();
            using var command = SqlFactory.CreateCommand();
            command.Connection = connection;
            command.CommandText = $"DELETE FROM {tableName}";
            command.ExecuteNonQuery();
        }

        public bool TableExists(string tableName)
        {
            var tables = _dbAccessStrategy.GetTableNames();
            return tables.Contains(tableName, StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        ///     Ensures that the underlying database tables contain all of the Fields to represent the known entities.
        ///     This is useful if you need to add a Field to an existing store.  Just add the Field to the Entity, then
        ///     call EnsureCompatibility to have the field added to the database.
        /// </summary>
        public override void EnsureCompatibility()
        {
            if (!StoreExists)
            {
                CreateStore();
                return;
            }

            var connection = GetWriteConnection();
            foreach (var entity in Entities) EnsureCompatibility(connection, entity);
        }

        public void BeginTransaction()
        {
            BeginTransaction(IsolationLevel.Unspecified);
        }

        public void BeginTransaction(IsolationLevel isolationLevel)
        {
            lock (_transactionSyncRoot)
            {
                if (CurrentTransaction != null)
                    throw new InvalidOperationException("Parallel transactions are not supported");

                var connection = GetWriteConnection();
                CurrentTransaction = connection.BeginTransaction(isolationLevel);
            }
        }

        public void Commit()
        {
            if (CurrentTransaction == null) throw new InvalidOperationException();

            lock (_transactionSyncRoot)
            {
                CurrentTransaction.Commit();
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
            }
        }

        public void Rollback()
        {
            if (CurrentTransaction == null) throw new InvalidOperationException();

            lock (_transactionSyncRoot)
            {
                CurrentTransaction.Rollback();
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        /// <remarks>You <b>MUST</b> call CloseReader after calling this method to prevent a leak</remarks>
        public virtual IDataReader ExecuteReader(string sql)
        {
            try
            {
                var connection = GetReadConnection();
                var command = SqlFactory.CreateCommand();
                command.CommandText = sql;
                command.Connection = connection;
                command.Transaction = CurrentTransaction;

                var reader = command.ExecuteReader(CommandBehavior.Default);
                return reader;
            }
            catch (Exception ex)
            {
                OrmDebug.Trace("SQLStoreBase::ExecuteReader threw: " + ex.Message);
                throw;
            }
        }

        ~SqlDataStore()
        {
            Dispose();
        }

        public void ValidateTable(IEntityInfo entity)
        {
            _dbAccessStrategy.ValidateTable(entity);
        }

        protected override void ReleaseManagedResources()
        {
            base.ReleaseManagedResources();
            _readConnectionPool.Dispose();
            _writeConnectionPool.Dispose();
        }

        public object ExecuteScalar(IDbCommand command)
        {
            var connection = GetReadConnection();
            command.Connection = connection;
            command.Transaction = CurrentTransaction;
            return command.ExecuteScalar();
        }

        protected override void OnInsert(object item)
        {
            _dbAccessStrategy.Insert(item);
        }

        protected override void OnUpdate(object item)
        {
            _dbAccessStrategy.Update(item);
        }

        protected override void OnDelete(object item)
        {
            _dbAccessStrategy.Delete(item);
        }

        private IEnumerable<AggregableResultRow> ExecuteCommandReader(IDbCommand command)
        {
            try
            {
                command.Connection = GetReadConnection();
                command.Transaction = CurrentTransaction;
                DisplayDebug(command);

                using var results = command.ExecuteReader();
                while (results.Read()) yield return new AggregableResultRow(results);
            }
            finally
            {
                command.Dispose();
            }
        }

        private static void DisplayDebug(IDbCommand command)
        {
#if DEBUG
            OrmDebug.Trace(command.CommandText);
            foreach (IDbDataParameter commandParameter in command.Parameters)
            {
                string value;
                if (commandParameter.Value == DBNull.Value)
                    value = "NULL";
                else if (commandParameter.Value is DateTime)
                    value = ((DateTime) commandParameter.Value).ToString("MM/dd/yyyy HH:mm:ss.fff");
                else if (commandParameter.Value is byte[] byteArrayValue)
                    value = Encoding.UTF8.GetString(byteArrayValue, 0, byteArrayValue.Length);
                else
                    value = commandParameter.Value.ToString();

                OrmDebug.Trace($"\t{commandParameter.ParameterName}:{value}");
            }
#endif
        }

        private IDbCommand BuildCommand(IClause sqlClause)
        {
            var @params = new List<IDataParameter>();
            var sql = sqlClause.ToStatement(@params);

            var command = SqlFactory.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            foreach (var param in @params) command.Parameters.Add(param);
            return command;
        }

        private IEnumerable<TIEntity> ExecuteCommandReader<TIEntity>(IDbCommand command,
            Func<IDataReader, TIEntity> deserialize, int offset)
            where TIEntity : class
        {
            try
            {
                command.Connection = GetReadConnection();
                command.Transaction = CurrentTransaction;
                DisplayDebug(command);

                using var results = command.ExecuteReader();
                var currentOffset = 0;

                while (results.Read())
                {
                    if (currentOffset < offset)
                    {
                        currentOffset++;
                        continue;
                    }

                    yield return deserialize(results);
                }
            }
            finally
            {
                command.Dispose();
            }
        }

        /// <summary>
        ///     Ensures that the underlying database contain table with all Fields to represent specified entity.
        ///     This is useful if you need to add a Field to an existing store.  Just add the Field to the Entity, then
        ///     call EnsureCompatibility to have the field added to the database.
        /// </summary>
        protected override void EnsureCompatibility(Type entityType)
        {
            if (!StoreExists)
                return;

            var connection = GetWriteConnection();
            var name = Entities.GetNameForType(entityType);

            EnsureCompatibility(connection, Entities[name]);
        }

        private void EnsureCompatibility(IDbConnection connection, IEntityInfo entity)
        {
            var tableName = entity.GetNameInStore();
            if (!TableExists(tableName))
                CreateTable(connection, entity);
            else
                ValidateTable(entity);
        }
    }
}