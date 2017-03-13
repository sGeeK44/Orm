using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Orm.Core.Attributes;
using Orm.Core.Constants;
using Orm.Core.Filters;
using Orm.Core.Interfaces;
using Orm.Core.SqlQueries;

// ReSharper disable UseStringInterpolation
// ReSharper disable ConvertPropertyToExpressionBody
// ReSharper disable IntroduceOptionalParameters.Local

namespace Orm.Core.SqlStore
{
    public abstract class SqlStoreBase : DataStore, ITableBasedStore, ISqlBasedStore
    {
        private const int CommandCacheMaxLength = 10;
        private IDbConnection _connection;
        private ConnectionBehavior _connectionBehavior;
        private int _connectionCount;
        private readonly List<IDbConnection> _connectionPool;
        private readonly Dictionary<Type, object[]> _referenceCache = new Dictionary<Type, object[]>();
        protected Dictionary<string, IDbCommand> CommandCache = new Dictionary<string, IDbCommand>();


        public int DefaultStringFieldSize { get; set; }
        public int DefaultNumericFieldPrecision { get; set; }
        public int DefaultVarBinaryLength { get; set; }
        protected abstract string AutoIncrementFieldIdentifier { get; }

        public abstract override void CreateStore();
        public abstract override void DeleteStore();
        protected abstract void ValidateTable(IDbConnection connection, IEntityInfo entity);

        public abstract override bool StoreExists { get; }

        protected abstract void GetPrimaryKeyInfo(string entityName, out string indexName, out string columnName);

        public abstract override void OnInsert(object item, bool insertReferences);

        public abstract override void OnUpdate(object item, bool cascadeUpdates, string fieldName);

        public abstract string[] GetTableNames();

        protected abstract IDbCommand GetNewCommandObject();
        protected abstract IDbConnection GetNewConnectionObject();
        protected abstract IDataParameter CreateParameterObject(string parameterName, object parameterValue);

        protected IDbTransaction CurrentTransaction { get; set; }

        public abstract string ConnectionString { get; }

        private readonly object _transactionSyncRoot = new object();
        public int ConnectionPoolSize { get; set; }

        protected SqlStoreBase()
        {
            DefaultStringFieldSize = 200;
            DefaultNumericFieldPrecision = 16;
            DefaultVarBinaryLength = 8000;
            _connectionPool = new List<IDbConnection>();
            ConnectionPoolSize = 20;

            ConnectionBehavior = ConnectionBehavior.Persistent;
        }

        ~SqlStoreBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Derived classes should override this if the underlying engine supports compaction
        /// </summary>
        public virtual void CompactDatabase()
        {
        }

        public int OpenConnectionCount
        {
            get { return _connectionCount; }
        }

        public ConnectionBehavior ConnectionBehavior
        {
            get { return _connectionBehavior; }
            set
            {
                if (_connectionBehavior == value) return;

                lock (_transactionSyncRoot)
                {
                    if (CurrentTransaction != null)
                    {
                        throw new Exception("You cannot change ConnectionBehavior while a Transaction is pending");
                    }

                    _connectionBehavior = _nonTransactionConnectionBehavior = value;
                }
            }
        }

        protected virtual int MaxSizedStringLength
        {
            get { return 4000; }
        }

        protected virtual int MaxSizedBinaryLength
        {
            get { return 8000; }
        }

        protected virtual string ParameterPrefix
        {
            get { return "@"; }
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override void CloseConnections()
        {
            foreach (var connection in _connectionPool)
            {
                if (connection != null && connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            _connectionPool.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                base.Dispose();
                foreach (var connection in _connectionPool)
                {
                    connection.Close();
                    connection.Dispose();
                }
                _connectionPool.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (Debugger.IsAttached) Debugger.Break();
            }
        }

        protected virtual string DefaultDateGenerator
        {
            get { return "GETDATE()"; }
        }

        private IDbConnection GetPoolConnection()
        {
            lock(_connectionPool)
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
                        _connectionPool.Remove(connection);
                    }

                    if (_connectionPool.Count < ConnectionPoolSize)
                    {
                        connection = GetNewConnectionObject();
                        connection.Open();
                        _connectionPool.Add(connection);
                        Interlocked.Increment(ref _connectionCount);
                        Debug.WriteLine("Creating pooled connection");
                        return connection;
                    }

                    // pool is full, we have to wait
                    Thread.Sleep(1000);

                    // TODO: add a timeout?
                } while (connection == null);

                // this should never happen
                return null;
            }
        }

        private static bool Open(IDbConnection connection)
        {
            return Open(connection, false);
        }

        private static bool Open(IDbConnection connection, bool isRetry)
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
                if (isRetry)
                    return false;

                connection.Dispose();

                // retry once
                Thread.Sleep(1000);
                return Open(connection, true);
            }
        }

        public virtual IDbConnection GetConnection(bool maintenance)
        {
            IDbConnection result;
            
            switch (ConnectionBehavior)
            {
                case ConnectionBehavior.AlwaysNew:
                    var connection = GetNewConnectionObject();
                    connection.Open();
                    Interlocked.Increment(ref _connectionCount);
                    result = connection;
                    break;
                case ConnectionBehavior.HoldMaintenance:
                    if (_connection == null)
                    {
                        _connection = GetNewConnectionObject();
                        _connection.Open();
                        OnPersistentConnectionCreated(_connection);
                        Interlocked.Increment(ref _connectionCount);
                    }
                    if (maintenance)
                    {
                        while(!IsFreeForUse(_connection))
                        {
                            Thread.Sleep(1000);
                        }
                        return _connection;
                    }
                    var connection2 = GetPoolConnection();
                    Interlocked.Increment(ref _connectionCount);
                    result = connection2;
                    break;
                case ConnectionBehavior.Persistent:
                    var pooledConnection = GetPoolConnection();
                    result = pooledConnection;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return result;
        }

        private IDbConnection GetFreeConnectionInPool()
        {
            return _connectionPool.FirstOrDefault(IsFreeForUse);
        }

        private static bool IsFreeForUse(IDbConnection connection)
        {
            return connection.State != ConnectionState.Executing
                && connection.State != ConnectionState.Fetching;
        }

        protected virtual void OnPersistentConnectionCreated(IDbConnection connection) { }

        protected void ReleasePersistentConnection()
        {
            if (_connection == null) return;
            try
            {
                var disp = _connection as IDisposable;
                if (disp == null)
                    return;

                // set the global ref to null to prevent recursion
                _connection = null;
                try
                {
                    // make sure it's disposed
                    disp.Dispose();
                }
                catch (ObjectDisposedException) { }
            }
            finally
            {
                _connection = null;
            }
        }


        protected virtual void DoneWithConnection(IDbConnection connection, bool maintenance)
        {
            switch (ConnectionBehavior)
            {
                case ConnectionBehavior.AlwaysNew:
                    connection.Close();
                    connection.Dispose();
                    Interlocked.Decrement(ref _connectionCount);
                    break;
                case ConnectionBehavior.HoldMaintenance:
                    if (maintenance)
                        return;
                    connection.Close();
                    connection.Dispose();
                    Interlocked.Decrement(ref _connectionCount);
                    break;
                case ConnectionBehavior.Persistent:
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        public int ExecuteNonQuery(string sql)
        {
            var connection = GetConnection(false);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.CommandText = sql;
                    command.Connection = connection;
                    command.Transaction = CurrentTransaction;
                    return command.ExecuteNonQuery();
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("SQLStoreBase::ExecuteNonQuery threw: " + ex.Message);
                throw;
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        public object ExecuteScalar(string sql)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    return ExecuteScalarSimulated(sql);
                default:
                    return ExecuteScalarActual(sql);
            }
        }

        private object ExecuteScalarActual(string sql)
        {
            var connection = GetConnection(false);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = sql;
                    command.Transaction = CurrentTransaction;
                    return command.ExecuteScalar();
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        /// <summary>
        /// This is a "simulation" of ExecuteScalar, which is necessary because an actual ExecuteScaler in Mono will fail
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        private object ExecuteScalarSimulated(string sql)
        {
            var connection = GetConnection(false);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = sql;
                    command.Transaction = CurrentTransaction;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader.FieldCount > 0)
                            {
                                return reader[0];
                            }
                        }
                    }
                }

                return null;
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        protected virtual string[] ReservedWords { get { return SqlReserved; } }

        private static readonly string[] SqlReserved = {
            "IDENTITY" ,"ENCRYPTION" ,"ORDER" ,"ADD" ,"END" ,"OUTER" ,"ALL" ,"ERRLVL" ,"OVER" ,"ALTER" ,"ESCAPE" ,"PERCENT" ,"AND" ,"EXCEPT" ,"PLAN" ,"ANY" ,"EXEC" ,"PRECISION" ,"AS" ,"EXECUTE" ,"PRIMARY" ,"ASC",
            "EXISTS" ,"PRINT" ,"AUTHORIZATION" ,"EXIT" ,"PROC" ,"AVG" ,"EXPRESSION" ,"PROCEDURE" ,"BACKUP" ,"FETCH" ,"PUBLIC" ,"BEGIN" ,"FILE" ,"RAISERROR" ,"BETWEEN" ,"FILLFACTOR" ,"READ" ,"BREAK" ,"FOR" ,"READTEXT",
            "BROWSE" ,"FOREIGN" ,"RECONFIGURE" ,"BULK" ,"FREETEXT" ,"BY" ,"FREETEXTTABLE" ,"REPLICATION" ,"CASCADE" ,"FROM" ,"RESTORE" ,"CASE" ,"FULL" ,"RESTRICT" ,"CHECK" ,"FUNCTION" ,"RETURN" ,"CHECKPOINT",
            "GOTO" ,"REVOKE" ,"CLOSE" ,"GRANT" ,"RIGHT" ,"CLUSTERED" ,"GROUP" ,"ROLLBACK" ,"COALESCE" ,"HAVING" ,"ROWCOUNT" ,"COLLATE" ,"HOLDLOCK" ,"ROWGUIDCOL" ,"COLUMN" ,"IDENTITY" ,"RULE",
            "COMMIT" ,"IDENTITY_INSERT" ,"SAVE" ,"COMPUTE" ,"IDENTITYCOL" ,"SCHEMA" ,"CONSTRAINT" ,"IF" ,"SELECT" ,"CONTAINS" ,"IN" ,"SESSION_USER" ,"CONTAINSTABLE" ,"INDEX" ,"SET" ,"CONTINUE" ,"INNER" ,"SETUSER",
            "CONVERT" ,"INSERT" ,"SHUTDOWN" ,"COUNT" ,"INTERSECT" ,"SOME" ,"CREATE" ,"INTO" ,"STATISTICS" ,"CROSS" ,"IS" ,"SUM" ,"CURRENT" ,"JOIN" ,"SYSTEM_USER" ,"CURRENT_DATE" ,"KEY" ,"TABLE" ,"CURRENT_TIME" ,"KILL",
            "TEXTSIZE" ,"CURRENT_TIMESTAMP" ,"LEFT" ,"THEN" ,"CURRENT_USER" ,"LIKE" ,"TO" ,"CURSOR" ,"LINENO" ,"TOP" ,"DATABASE" ,"LOAD" ,"TRAN" ,"DATABASEPASSWORD" ,"MAX" ,"TRANSACTION" ,"DATEADD" ,"MIN" ,"TRIGGER",
            "DATEDIFF" ,"NATIONAL" ,"TRUNCATE" ,"DATENAME" ,"NOCHECK" ,"TSEQUAL" ,"DATEPART" ,"NONCLUSTERED" ,"UNION" ,"DBCC" ,"NOT" ,"UNIQUE" ,"DEALLOCATE", "NULL", "UPDATE", "DECLARE", "NULLIF", "UPDATETEXT",
            "DEFAULT", "OF", "USE", "DELETE", "OFF", "USER", "DENY", "OFFSETS", "VALUES", "DESC", "ON", "VARYING", "DISK", "OPEN", "VIEW", "DISTINCT", "OPENDATASOURCE", "WAITFOR", "DISTRIBUTED", "OPENQUERY", "WHEN", 
            "DOUBLE", "OPENROWSET", "WHERE", "DROP", "OPENXML", "WHILE", "DUMP", "OPTION", "WITH", "ELSE", "OR", "WRITETEXT" 
        };

        protected virtual void CreateTable(IDbConnection connection, IEntityInfo entity)
        {
            var sql = new StringBuilder();

            if (ReservedWords.Contains(entity.EntityName, StringComparer.InvariantCultureIgnoreCase))
            {
                throw new ReservedWordException(entity.EntityName);
            }

            sql.AppendFormat("CREATE TABLE [{0}] (", entity.EntityName);

            var count = entity.Fields.Count;

            foreach (var field in entity.Fields)
            {
                if (ReservedWords.Contains(field.FieldName, StringComparer.InvariantCultureIgnoreCase))
                {
                    throw new ReservedWordException(field.FieldName);
                }

                sql.AppendFormat("{0} {1} {2}",
                    field.FieldName,
                    GetFieldDataTypeString(entity.EntityName, field),
                    GetFieldCreationAttributes(entity.EntityAttribute, field));

                if (--count > 0) sql.Append(", ");
            }

            sql.Append(")");

            Debug.WriteLine(sql);

            
            using (var command = GetNewCommandObject())
            {
                command.CommandText = sql.ToString();
                command.Connection = connection;
                command.Transaction = CurrentTransaction;
                command.ExecuteNonQuery();
            }

            // build the index for primary key if it's not there
            VerifyIndex(entity.EntityName, entity.Fields.KeyField.FieldName, FieldSearchOrder.Ascending);

            // create indexes
            foreach (var field in entity.Fields)
            {
                if (field.SearchOrder != FieldSearchOrder.NotSearchable)
                {
                    VerifyIndex(entity.EntityName, field.FieldName, field.SearchOrder, connection);
                }
            }
        }

        protected virtual string VerifyIndex(string entityName, string fieldName, FieldSearchOrder searchOrder)
        {
            return VerifyIndex(entityName, fieldName, searchOrder, null);
        }

        protected virtual string VerifyIndex(string entityName, string fieldName, FieldSearchOrder searchOrder, IDbConnection connection)
        {
            var localConnection = false;
            if (connection == null)
            {
                localConnection = true;
                connection = GetConnection(true);
            }
            try
            {
                var indexName = string.Format("ORM_IDX_{0}_{1}_{2}", entityName, fieldName,
                    searchOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");

                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;

                    var sql = string.Format("SELECT COUNT(*) FROM information_schema.indexes WHERE INDEX_NAME = '{0}'", indexName);
                    command.CommandText = sql;

                    var i = (int)command.ExecuteScalar();

                    if (i == 0)
                    {
                        sql = string.Format("CREATE INDEX {0} ON [{1}]({2} {3})",
                            indexName,
                            entityName,
                            fieldName,
                            searchOrder == FieldSearchOrder.Descending ? "DESC" : string.Empty);

                        Debug.WriteLine(sql);

                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }

                return indexName;
            }
            finally
            {
                if (localConnection)
                {
                    DoneWithConnection(connection, true);
                }
            }
        }

        protected virtual string GetFieldDataTypeString(string entityName, FieldAttribute field)
        {
            // the SQL RowVersion is a special case
            if (field.IsRowVersion)
            {
                switch (field.DataType)
                {
                    case DbType.UInt64:
                    case DbType.Int64:
                        // no error
                        break;
                    default:
                        throw new FieldDefinitionException(entityName, field.FieldName, "rowversion fields must be an 8-byte data type (Int64 or UInt64)");
                }

                return "rowversion";
            }

            if (field.DataType == DbType.Binary)
            {
                // default to varbinary unless a Length is specifically supplied and it is >= 8000
                if (field.Length >= MaxSizedBinaryLength)
                {
                    return "image";
                }
                // if no length was supplied, default to DefaultVarBinaryLength (8000)
                return string.Format("varbinary({0})", field.Length == 0 ? DefaultVarBinaryLength : field.Length);
            }

            if ((field.DataType == DbType.String) && (field.Length > MaxSizedStringLength))
            {
                return "ntext";
            }

            return field.DataType.ToSqlTypeString();
        }

        protected virtual string GetFieldCreationAttributes(EntityAttribute attribute, FieldAttribute field)
        {
            var sb = new StringBuilder();

            switch (field.DataType)
            {
                case DbType.String:
                case DbType.StringFixedLength:
                    if (field.Length > 0)
                    {
                        if (field.Length <= MaxSizedStringLength)
                        {
                            sb.AppendFormat("({0}) ", field.Length);
                        }
                        // SQLCE uses ntext, which cannot have a size
                        //else
                        //{
                        //    sb.AppendFormat("({0}) ", MaxSizedStringLength);
                        //}
                    }
                    else
                    {
                        sb.AppendFormat("({0}) ", DefaultStringFieldSize);
                    }
                    break;
                case DbType.Decimal:
                    var p = field.Precision == 0 ? DefaultNumericFieldPrecision : field.Precision;
                    sb.AppendFormat("({0},{1}) ", p, field.Scale);
                    break;
            }

            if ((field.DefaultType != DefaultType.None) || (field.DefaultValue != null))
            {
                if (field.DefaultType == DefaultType.CurrentDateTime)
                {
                    // allow an override of the actual default value - if none is provided, use the default SqlDateTimeDefault
                    var value = field.DefaultValue as IDefaultValue;
                    sb.AppendFormat("DEFAULT {0} ", value != null ? value.GetDefaultValue() : DefaultDateGenerator);
                }
                else
                {
                    if (field.DefaultValue is string)
                    {
                        sb.AppendFormat("DEFAULT '{0}' ", field.DefaultValue);
                    }
                    else
                    {
                        sb.AppendFormat("DEFAULT {0} ", field.DefaultValue);
                    }
                }
            }

            if (field.IsPrimaryKey)
            {
                sb.Append("PRIMARY KEY ");

                if (attribute.KeyScheme == KeyScheme.Identity)
                {
                    switch(field.DataType)
                    {
                        case DbType.Int32:
                        case DbType.UInt32:
                        case DbType.Int64:
                        case DbType.UInt64:
                            sb.Append(AutoIncrementFieldIdentifier + " ");
                            break;
                        case DbType.Guid:
                            sb.Append("ROWGUIDCOL ");
                            break;
                        default:
                            throw new FieldDefinitionException(attribute.NameInStore, field.FieldName,
                                string.Format("Data Type '{0}' cannot be marked as an Identity field", field.DataType));
                    }
                }
            }

            if (!field.AllowsNulls)
            {
                sb.Append("NOT NULL ");
            }

            if (field.RequireUniqueValue)
            {
                sb.Append("UNIQUE ");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines if the specified object already exists in the Store (by primary key value)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool Contains(object item)
        {
            var itemType = item.GetType();
            var entityName = Entities.GetNameForType(itemType);

            var keyValue = Entities[entityName].Fields.KeyField.PropertyInfo.GetValue(item, null);

            var existing = Select(itemType, keyValue);

            return existing != null;
        }

        /// <summary>
        /// Retrieves a single entity instance from the DataStore identified by the specified primary key value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public override T Select<T>(object primaryKey)
        {
            return (T)Select(typeof(T), primaryKey);
        }

        public override object Select(Type objectType, object primaryKey)
        {
            var entityName = Entities.GetNameForType(objectType);
            var entity = Entities[entityName];
            var field = entity.Fields.KeyField;
            return Select(objectType, field.FieldName, primaryKey);
        }

        protected virtual object Select(Type objectType, string fieldName, object value)
        {
            var condition = Condition(objectType, fieldName, value, FilterOperator.Equals);
            return Select(objectType, condition).FirstOrDefault();
        }

        public override IJoinable<TEntity> Select<TEntity>()
        {
            return new Select<TEntity, TEntity>(this, Entities);
        }

        public override IJoinable<TIEntity> Select<TEntity, TIEntity>()
        {
            return new Select<TEntity, TIEntity>(this, Entities);
        }

        public override IEnumerable<object> Select(Type objectType)
        {
            return new Select(this, Entities, objectType).Execute();
        }

        public override IEnumerable<object> Select(Type objectType, ICondition condition)
        {
            return new Select(this, Entities, objectType).Where(condition).Execute();
        }

        public override IEnumerable<TIEntity> Execute<TIEntity>(ISelect<TIEntity> select)
        {
            var command = BuildCommand(select);
            IEnumerable<TIEntity> result = null;

            Diagnostics.Measure("Execute command reader", () =>
            {
                result = ExecuteCommandReader<TIEntity>(command, select.Deserialize, select.Offset);
            });
            return result;
        }

        public override IEnumerable<object> Execute(ISelect select)
        {
            var command = BuildCommand(select);
            return ExecuteCommandReader<object>(command, select.Deserialize, select.Offset);
        }

        private IDbCommand BuildCommand(ISqlClause select)
        {
            List<IDataParameter> @params;
            var sql = select.ToStatement(out @params);

            var command = GetNewCommandObject();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            foreach (var param in @params)
            {
                command.Parameters.Add(param);
            }
            return command;
        }

        private IEnumerable<TIEntity> ExecuteCommandReader<TIEntity>(IDbCommand command, Func<IDataReader, IEntityCache, TIEntity> deserialize, int offset)
            where TIEntity : class
        {
            if (UseCommandCache)
            {
                lock (CommandCache)
                {
                    var sql = command.CommandText;
                    if (CommandCache.ContainsKey(sql))
                    {
                        var old = command;
                        command = CommandCache[sql];

                        // use the cached command object, but we must copy over the new command parameter values
                        // or it will use the old ones
                        for (var p = 0; p < command.Parameters.Count; p++)
                        {
                            ((IDbDataParameter)command.Parameters[p]).Value = ((IDbDataParameter)old.Parameters[p]).Value;
                        }
                        old.Dispose();
                    }
                    else
                    {
                        CommandCache.Add(sql, command);

                        // trim the cache so it doesn't grow infinitely
                        if (CommandCache.Count > CommandCacheMaxLength)
                        {
                            CommandCache.Remove(CommandCache.First().Key);
                        }
                    }
                }
            }

            try
            {
                if (UseCommandCache)
                {
                    Monitor.Enter(CommandCache);
                }

                command.Connection = GetConnection(false);
                command.Transaction = CurrentTransaction;

                using (var results = command.ExecuteReader())
                {
                    var currentOffset = 0;
                    var entityCache = new EntityCache();
                    while (results.Read())
                    {
                        if (currentOffset < offset)
                        {
                            currentOffset++;
                            continue;
                        }

                        yield return deserialize(results, entityCache);
                    }
                }
            }
            finally
            {
                if (!UseCommandCache)
                {
                    command.Dispose();
                }

                if (UseCommandCache)
                {
                    Monitor.Exit(CommandCache);
                }

                FlushReferenceTableCache();
                DoneWithConnection(command.Connection, false);
            }
        }

        /// <summary>
        /// Determines if the ORM engine should be allowed to cache commands of not.  If you frequently use the same FilterConditions on a Select call to a single entity, 
        /// using the command cache can improve performance by preventing the underlying SQL Compact Engine from recomputing statistics.
        /// </summary>
        public bool UseCommandCache { get; set; }

        public void ClearCommandCache()
        {
            lock (CommandCache)
            {
                foreach (var cmd in CommandCache)
                {
                    cmd.Value.Dispose();
                }
                CommandCache.Clear();
            }
        }

        protected void CheckPrimaryKeyIndex(string entityName)
        {
            var info = Entities[entityName] as SqlEntityInfo;
            if (info == null || info.PrimaryKeyIndexName != null)
                return;

            string index;
            string column;
            GetPrimaryKeyInfo(entityName, out index, out column);
            info.PrimaryKeyIndexName = index;
            info.PrimaryKeyColumnName = column;
        }

        /// <summary>
        /// Populates the ReferenceField members of the provided entity instance
        /// </summary>
        /// <param name="instance"></param>
        public override void FillReferences(object instance)
        {
            FillReferences(instance, null, null, false);
        }

        protected void FlushReferenceTableCache()
        {
            _referenceCache.Clear();
        }

        protected void DoInsertReferences(object item, string entityName, KeyScheme keyScheme, bool beforeParentInsert)
        {
            // cascade insert any References
            // do this last because we need the PK from above
            foreach (var reference in Entities[entityName].References)
            {
                if (beforeParentInsert && (reference.ReferenceType == ReferenceType.ManyToOne)) // N:1
                {
                    // in an N:1 we need to insert the related item first, so it can get a PK assigned
                    var referenceEntity = reference.PropertyInfo.GetValue(item, null);

                    // is there anything to insert?
                    if (referenceEntity == null) continue;

                    var referenceEntityName = Entities.GetNameForType(reference.ReferenceEntityType);
                    var refPk = Entities[referenceEntityName].Fields.KeyField.PropertyInfo.GetValue(referenceEntity, null);

                    // does the reference entity already exist in the store?
                    var existing = Select(reference.ReferenceEntityType, refPk);

                    if (existing == null)
                    {
                        Insert(referenceEntity);

                        // we then copy the PK of the reference item into the "local" FK field - need to re-query the key
                        refPk = Entities[referenceEntityName].Fields.KeyField.PropertyInfo.GetValue(referenceEntity, null);

                        // set the item key
                        // we already inserted, so we have to do an update
                        // TODO: in the future, we should move this up and do reference inserts first, then back=propagate references
                        Entities[entityName].Fields[reference.ForeignReferenceField].PropertyInfo.SetValue(item, refPk, null);
                    }
                }
                else if(!beforeParentInsert && (reference.ReferenceType == ReferenceType.OneToMany)) // 1:N
                {
                    // cascade insert any References
                    // do this last because we need the PK from above
                    string et = null;

                    var valueArray = reference.PropertyInfo.GetValue(item, null);
                    if (valueArray == null) continue;

                    //entityName = m_entities.GetNameForType(reference.ReferenceEntityType);
                    var fk = Entities[entityName].Fields[reference.ForeignReferenceField].PropertyInfo.GetValue(item, null);

                    // we've already enforced this to be an array when creating the store
                    foreach (var element in (Array)valueArray)
                    {
                        if (et == null)
                        {
                            et = Entities.GetNameForType(element.GetType());
                        }

                        // get the FK value
                        var keyValue = Entities[et].Fields.KeyField.PropertyInfo.GetValue(element, null);

                        var isNew = false;


                        // only do an insert if the value is new (i.e. need to look for existing reference items)
                        // not certain how this will work right now, so for now we ask the caller to know what they're doing
                        switch (keyScheme)
                        {
                            case KeyScheme.Identity:
                                // SQLCE and SQLite start with an ID == 1, so 0 mean "not in DB"
                                isNew = keyValue.Equals(0) || keyValue.Equals(-1);
                                break;
                            case KeyScheme.GUID:
                                // TODO: see if PK field value == null
                                isNew = keyValue.Equals(null);
                                break;
                        }

                        if (isNew)
                        {
                            Entities[et].Fields[reference.ForeignReferenceField].PropertyInfo.SetValue(element, fk, null);
                            Insert(element);
                        }
                    }
                }

            }
        }
        
        protected void FillReferences(object instance, object keyValue, ReferenceAttribute[] fieldsToFill, bool cacheReferenceTable)
        {
            if (instance == null) return;

            var type = instance.GetType();
            var entityName = Entities.GetNameForType(type);

            if (entityName == null)
            {
                AddType(type);
                entityName = Entities.GetNameForType(type);
            }

            if (Entities[entityName].References.Count == 0) return;

            var referenceItems = new Dictionary<ReferenceAttribute, object[]>();

            // query the key if not provided
            if (keyValue == null)
            {
                keyValue = Entities[entityName].Fields.KeyField.PropertyInfo.GetValue(instance, null);
            }

            // populate reference fields
            foreach (var reference in Entities[entityName].References)
            {
                if (fieldsToFill != null)
                {
                    if (!fieldsToFill.Contains(reference))
                    {
                        continue;
                    }
                }

                if (reference.ReferenceType == ReferenceType.ManyToOne)
                {
                    // In a N:1 relation, the local ('instance' coming in here) key is the FK and the remote it the PK.  
                    // We need to read the local FK, so we can go to the reference table and pull the one row with that PK value
                    keyValue = Entities[entityName].Fields[reference.ForeignReferenceField].PropertyInfo.GetValue(instance, null);
                }

                // get the lookup values - until we support filtered selects, this may be very expensive memory-wise
                if (!referenceItems.ContainsKey(reference))
                {
                    object[] refData;
                    if (cacheReferenceTable)
                    {
                        // TODO: ref cache needs to be type->reftype->ref's, not type->refs

                        if (!_referenceCache.ContainsKey(reference.ReferenceEntityType))
                        {
                            refData = Select(reference.ReferenceEntityType).ToArray();
                            _referenceCache.Add(reference.ReferenceEntityType, refData);
                        }
                        else
                        {
                            refData = _referenceCache[reference.ReferenceEntityType];
                        }
                    }
                    else
                    {
                        // FALSE for last parameter to prevent circular reference filling
                        var condition = Condition(reference.ReferenceEntityType, reference.ForeignReferenceField, keyValue, FilterOperator.Equals);
                        refData = Select(reference.ReferenceEntityType, condition).ToArray();
                    }

                    // see if the reference type is known - if not, try to add it automatically
                    var name = Entities.GetNameForType(reference.ReferenceEntityType);
                    if (name == null)
                    {
                        AddType(reference.ReferenceEntityType);
                    }

                    referenceItems.Add(reference, refData.ToArray());
                }

                // get the lookup field
                var childEntityName = Entities.GetNameForType(reference.ReferenceEntityType);

                var children = new List<object>();

                // now look for those that match our pk
                foreach (var child in referenceItems[reference])
                {
                    var childKey = Entities[childEntityName].Fields[reference.ForeignReferenceField].PropertyInfo.GetValue(child, null);

                    // this seems "backward" because childKey may turn out null, 
                    // so doing it backwards (keyValue.Equals instead of childKey.Equals) prevents a null referenceexception
                    // we have to do the conversion becasue SQLite will have one of these as a 32-bit and the other as a 64-bit, and "Equals" will turn out false
                    if (keyValue.Equals(Convert.ChangeType(childKey, keyValue.GetType(), null)))
                    {
                        children.Add(child);
                    }
                }
                var carr = children.ConvertAll(reference.ReferenceEntityType);

                if (reference.PropertyInfo.PropertyType.IsArray)
                {
                    reference.PropertyInfo.SetValue(instance, carr, null);
//                    reference.PropertyInfo.SetValue(instance, Convert.ChangeType(carr, reference.PropertyInfo.PropertyType), null);
                }
                else
                {
                    var enumerator = carr.GetEnumerator();

                    if (enumerator.MoveNext())
                    {
                        reference.PropertyInfo.SetValue(instance, children[0], null);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes all rows from the specified Table
        /// </summary>
        public virtual void TruncateTable(string tableName)
        {
            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = string.Format("DELETE FROM {0}", tableName);
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public virtual bool TableExists(string tableName)
        {
            return GetTableNames().Contains(tableName, StringComparer.InvariantCultureIgnoreCase);
        }

        public virtual void DropTable(string tableName)
        {
            if (!TableExists(tableName)) return;

            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = string.Format("DROP TABLE {0}", tableName);
                    command.ExecuteNonQuery();
                }

                Entities.Remove(tableName);
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }


        /// <summary>
        /// Deletes all entity instances of the specified type from the DataStore
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public override void Delete<T>()
        {
            var t = typeof(T);
            var entityName = Entities.GetNameForType(t);

            if (entityName == null)
            {
                throw new EntityNotFoundException(t);
            }

            // TODO: handle cascade deletes?

            TruncateTable(entityName);
        }

        public override void Delete<T>(string fieldName, object matchValue)
        {
            var entityName = Entities.GetNameForType(typeof(T));

            Delete(entityName, fieldName, matchValue);
        }

        /// <summary>
        /// Deletes entities of a given type where the specified field name matches a specified value
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="fieldName"></param>
        /// <param name="matchValue"></param>
        protected void Delete(Type entityType, string fieldName, object matchValue)
        {
            var entityName = Entities.GetNameForType(entityType);

            Delete(entityName, fieldName, matchValue);
        }

        public override void Delete(string entityName, string fieldName, object matchValue)
        {
            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.Transaction = CurrentTransaction;
                    command.CommandText = string.Format("DELETE FROM {0} WHERE {1} = {2}val", entityName, fieldName, ParameterPrefix);
                    var param = CreateParameterObject(ParameterPrefix + "val", matchValue);
                    command.Parameters.Add(param);
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        /// <summary>
        /// Deletes the specified entity instance from the DataStore
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>
        /// The instance provided must have a valid primary key value
        /// </remarks>
        public override void OnDelete(object item)
        {
            
            var type = item.GetType();
            var entityName = Entities.GetNameForType(type);
            if (entityName == null)
            {
                throw new EntityNotFoundException(type);
            }

            if (Entities[entityName].Fields.KeyField == null)
            {
                throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform a Delete");
            }
            var keyValue = Entities[entityName].Fields.KeyField.PropertyInfo.GetValue(item, null);

            Delete(type, keyValue);
        }

        protected virtual void Delete(Type t, object primaryKey)
        {
            var entityName = Entities.GetNameForType(t);

            // if the entity type hasn't already been registered, try to auto-register
            if (entityName == null)
            {
                AddType(t);
            }

            Delete(entityName, primaryKey);
        }

        public override void Delete(string entityName, object primaryKey)
        {
            if (entityName == null)
            {
                throw new EntityNotFoundException((string) null);
            }

            if (Entities[entityName].Fields.KeyField == null)
            {
                throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform a Delete");
            }

            // handle cascade deletes
            foreach (var reference in Entities[entityName].References)
            {
                if (!reference.CascadeDelete) continue;

                Delete(reference.ReferenceEntityType, reference.ForeignReferenceField, primaryKey);
            }

            var keyFieldName = Entities[entityName].Fields.KeyField.FieldName;
            Delete(entityName, keyFieldName, primaryKey);
        }

        public override int Count(string entityName)
        {
            if (string.IsNullOrEmpty(entityName))
            {
                throw new EntityNotFoundException(entityName);
            }

            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = string.Format("SELECT COUNT(*) FROM {0}", entityName);
                    var count = command.ExecuteScalar();
                    return Convert.ToInt32(count);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        protected override void AfterAddEntityType(Type entityType, bool ensureCompatibility)
        {
            if ((StoreExists) && (ensureCompatibility))
            {
                var connection = GetConnection(true);
                try
                {
                    var name = Entities.GetNameForType(entityType);

                    // this will exist because the caller inserted it
                    var entity = Entities[name];

                    if (!TableExists(name))
                    {
                        CreateTable(connection, entity);
                    }
                    else
                    {
                        ValidateTable(connection, entity);
                    }

                }
                finally
                {
                    DoneWithConnection(connection, true);
                }
            }
        }

        /// <summary>
        /// Ensures that the underlying database tables contain all of the Fields to represent the known entities.
        /// This is useful if you need to add a Field to an existing store.  Just add the Field to the Entity, then 
        /// call EnsureCompatibility to have the field added to the database.
        /// </summary>
        public override void EnsureCompatibility()
        {
            if (!StoreExists)
            {
                CreateStore();
                return;
            }

            var connection = GetConnection(true);
            try
            {
                lock (Entities.SyncRoot)
                {
                    foreach (var entity in Entities)
                    {
                        ValidateTable(connection, entity);
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        private ConnectionBehavior _nonTransactionConnectionBehavior;

        public override void BeginTransaction(IsolationLevel isolationLevel)
        {
            lock (_transactionSyncRoot)
            {
                if (CurrentTransaction != null)
                {
                    throw new InvalidOperationException("Parallel transactions are not supported");
                }

                // we must escalate the connection behavior for the transaction to remain valid
                if (ConnectionBehavior != ConnectionBehavior.Persistent)
                {
                    _nonTransactionConnectionBehavior = ConnectionBehavior;
                    ConnectionBehavior = ConnectionBehavior.Persistent;
                }


                if (_connection == null)
                {
                    // force creation of the persistent connection
                    _connection = GetConnection(false);
                    DoneWithConnection(_connection, false);
                }

                CurrentTransaction = _connection.BeginTransaction(isolationLevel);
            }
        }

        public override void Commit()
        {
            if (CurrentTransaction == null)
            {
                throw new InvalidOperationException();
            }

            lock (_transactionSyncRoot)
            {
                CurrentTransaction.Commit();
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
                // revert connection behavior if we escalated
                ConnectionBehavior = _nonTransactionConnectionBehavior;
            }
        }

        public override void Rollback()
        {
            if (CurrentTransaction == null)
            {
                throw new InvalidOperationException();
            }

            lock (_transactionSyncRoot)
            {
                CurrentTransaction.Rollback();
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
                // revert connection behavior if we escalated
                ConnectionBehavior = _nonTransactionConnectionBehavior;
            }
        }

        protected void UpdateEntityPropertyInfo(string entityName, string fieldName, PropertyInfo pi)
        {
            Entities[entityName].Fields[fieldName].PropertyInfo = pi;
        }

        private IDbConnection _readerConnection;

        public void CloseReader()
        {
            if (_readerConnection != null)
            {
                _readerConnection.Close();
                _readerConnection.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        /// <remarks>You <b>MUST</b> call CloseReader after calling this method to prevent a leak</remarks>
        public virtual IDataReader ExecuteReader(string sql)
        {
            IDbConnection connection;

            if (ConnectionBehavior != ConnectionBehavior.Persistent)
            {
                _readerConnection = GetNewConnectionObject();
                _readerConnection.Open();
                connection = _readerConnection;
            }
            else
            {
                connection = GetConnection(false);
            }

            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.CommandText = sql;
                    command.Connection = connection;
                    command.Transaction = CurrentTransaction;

                    var reader = command.ExecuteReader(CommandBehavior.Default);
                    return reader;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SQLStoreBase::ExecuteReader threw: " + ex.Message);
                throw;
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        public override void Drop(string entityName)
        {
            var command = string.Format("DROP TABLE '{0}'", entityName);
            ExecuteNonQuery(command);

            Entities.Remove(entityName);
        }
    }
}
