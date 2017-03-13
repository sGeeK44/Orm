using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Orm.Core;
using Orm.Core.Attributes;
using Orm.Core.Constants;
using Orm.Core.Entity;
using Orm.Core.Filters;
using Orm.Core.Interfaces;
using Orm.Core.SqlQueries;
using Orm.Core.SqlStore;

// ReSharper disable UseNullPropagation

// ReSharper disable ConvertPropertyToExpressionBody
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UseStringInterpolation

namespace Orm.SqlCe
{
    public class SqlCeDataStore : SqlStoreBase
    {
        private string _connectionString;
        private int _maxSize = 128; // Max Database Size defaults to 128MB
        private readonly SqlCeFactory _sqlFactory;
        private readonly Dictionary<string, Dictionary<string, int>> _entityOrdinal;

        private string Password { get; set; }

        public string FileName { get; protected set; }

        protected SqlCeDataStore()
        {
            _entityOrdinal = new Dictionary<string, Dictionary<string, int>>();
            UseCommandCache = true;
            _sqlFactory = new SqlCeFactory();
        }

        public SqlCeDataStore(string fileName)
            : this(fileName, null)
        {
        }

        public SqlCeDataStore(string fileName, string password)
            : this()
        {
            FileName = fileName;
            Password = password;
        }

        public override bool StoreExists
        {
            get { return File.Exists(FileName); }
        }

        public override ISqlFactory SqlFactory
        {
            get { return _sqlFactory; }
        }

        public override bool IsGreenHeath { get { return CheckConnection(true); } }

        private bool CheckConnection(bool firstTry)
        {
            try
            {
                var connection = GetConnection(false);
                DoneWithConnection(connection, false);
                return true;
            }
            catch (Exception)
            {
                if (!firstTry)
                    return false;

                TryToRepairDb();
                return CheckConnection(false);
            }
        }

        private void TryToRepairDb()
        {
            try
            {
                var engine = new SqlCeEngine(ConnectionString);
#if WindowsCE
                engine.Repair(null, RepairOption.RecoverCorruptedRows);
#else
                engine.Repair(null, RepairOption.RecoverAllPossibleRows);
#endif
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public override string Name
        {
            get { return FileName; }
        }

        protected override IDbCommand GetNewCommandObject()
        {
            return new SqlCeCommand();
        }

        protected override string AutoIncrementFieldIdentifier
        {
            get { return "IDENTITY"; }
        }

        /// <summary>
        /// Deletes the underlying DataStore
        /// </summary>
        public override void DeleteStore()
        {
            File.Delete(FileName);
        }

        /// <summary>
        /// Creates the underlying DataStore
        /// </summary>
        public override void CreateStore()
        {
            if (StoreExists)
            {
                throw new StoreAlreadyExistsException();
            }

            // create the file
            using (var engine = new SqlCeEngine(ConnectionString))
            {
                engine.CreateDatabase();
            }

            var connection = GetConnection(true);
            try
            {
                foreach (var entity in Entities)
                {
                    CreateTable(connection, entity);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
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
                foreach (var entity in Entities)
                {
                    ValidateTable(connection, entity);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public override void CompactDatabase()
        {
            using (var engine = new SqlCeEngine())
            {
                engine.Compact(ConnectionString);
            }
        }

        protected override IDataParameter CreateParameterObject(string parameterName, object parameterValue)
        {
            return new SqlCeParameter(parameterName, parameterValue);
        }

        protected override IDbConnection GetNewConnectionObject()
        {
            return new SqlCeConnection(ConnectionString);
        }

        public override int Count<T>(ICondition condition)
        {
            var t = typeof(T);
            var entityName = Entities.GetNameForType(t);

            if (entityName == null)
            {
                throw new EntityNotFoundException(t);
            }

            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    var @params = new List<IDataParameter>();
                    command.CommandText = string.Format("SELECT COUNT(*) FROM [{0}] WHERE {1}", entityName, condition.ToStatement(ref @params));
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.Connection = connection as SqlCeConnection;
                    foreach (var param in @params)
                    {
                        command.Parameters.Add(param);
                    }
                    Debug.WriteLine(command.CommandText);
                    return (int) command.ExecuteScalar();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        protected override object Select(Type objectType, string fieldName, object value)
        {
            var entityName = Entities.GetNameForType(objectType);
            var indexName = string.Format("ORM_IDX_{0}_{1}_ASC", entityName, fieldName);
            
            var command = new SqlCeCommand
            {
                CommandText = entityName,
                CommandType = CommandType.TableDirect,
                IndexName = indexName,
                Connection = GetConnection(false) as SqlCeConnection,
                Transaction = CurrentTransaction as SqlCeTransaction
            };

            try
            {
                using (var results = command.ExecuteReader())
                {

                    if (!results.Seek(DbSeekOptions.FirstEqual, value))
                        return null;

                    results.Read();
                    var entity = Entities[entityName];
                    var serializer = entity.GetSerializer();
                    serializer.UseFullName = false;
                    serializer.EntityCache = null;
                    return serializer.Deserialize(results);
                }
            }
            finally
            {
                command.Dispose();
                DoneWithConnection(command.Connection, false);
            }
        }

        /// <summary>
        /// Inserts the provided entity instance into the underlying data store.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="insertReferences"></param>
        /// <remarks>
        /// If the entity has an identity field, calling Insert will populate that field with the identity vale vefore returning
        /// </remarks>
        public override void OnInsert(object item, bool insertReferences)
        {
            var itemType = item.GetType();
            var entityName = Entities.GetNameForType(itemType);

            var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;

            if (entityName == null)
            {
                throw new EntityNotFoundException(item.GetType());
            }

            if (insertReferences)
            {
                DoInsertReferences(item, entityName, keyScheme, true);
            }

            // we'll use table direct for inserts - no point in getting the query parser involved in this
            var connection = GetConnection(false);
            try
            {
                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection as SqlCeConnection;
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;

                    using (var results = command.ExecuteResultSet(ResultSetOptions.Updatable))
                    {
                        var record = results.CreateRecord();

                        FieldAttribute identity;
                        FillEntity(record.SetValue, entityName, item, out identity);

                        results.Insert(record);

                        // did we have an identity field?  If so, we need to update that value in the item
                        if (identity != null)
                        {
                            var id = GetIdentity(connection);
                            SetInstanceValue(identity, item, id);
                        }

                        if (insertReferences)
                        {
                            DoInsertReferences(item, entityName, keyScheme, false);
                        }
                    }

                    command.Dispose();
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        public override void OnUpdate(object item, bool cascadeUpdates, string fieldName)
        {
            object keyValue;
            var itemType = item.GetType();

            var entityName = Entities.GetNameForType(itemType);

            if (entityName == null)
            {
                throw new EntityNotFoundException(itemType);
            }

            if (Entities[entityName].Fields.KeyField == null)
            {
                throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform Updates");
            }

            var connection = GetConnection(false);
            try
            {
                CheckPrimaryKeyIndex(entityName);

                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection as SqlCeConnection;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;
                    command.IndexName = ((SqlEntityInfo)Entities[entityName]).PrimaryKeyIndexName;
                    command.Transaction = CurrentTransaction as SqlCeTransaction;

                    using (var results = command.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable))
                    {
                        keyValue = GetKeyValue(Entities[entityName].Fields.KeyField, item);

                        // seek on the PK
                        var found = results.Seek(DbSeekOptions.BeforeEqual, keyValue);

                        if (!found)
                        {
                            // TODO: the PK value has changed - we need to store the original value in the entity or diallow this kind of change
                            throw new RecordNotFoundException("Cannot locate a record with the provided primary key.  You cannot update a primary key value through the Update method");
                        }

                        results.Read();
                        FieldAttribute id;
                        FillEntity(results.SetValue, entityName, item, out id);

                        results.Update();
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }

            if (cascadeUpdates)
            {
                // TODO: move this into the base DataStore class as it's not SqlCe-specific
                foreach (var reference in Entities[entityName].References)
                {
                    var itemList = reference.PropertyInfo.GetValue(item, null) as Array;
                    if (itemList != null)
                    {
                        foreach (var refItem in itemList)
                        {
                            var foreignKey = refItem.GetType().GetProperty(reference.ForeignReferenceField, BindingFlags.Instance | BindingFlags.Public);
                            foreignKey.SetValue(refItem, keyValue, null);

                            if (!Contains(refItem))
                            {
                                Insert(refItem, false);
                            }
                            else
                            {
                                Update(refItem, true, fieldName);
                            }
                        }
                    }
                }
            }
        }

        protected override void Delete(Type t, object primaryKey)
        {
            var entityName = Entities.GetNameForType(t);

            if (entityName == null)
            {
                throw new EntityNotFoundException(t);
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

            var connection = GetConnection(false);
            try
            {
                CheckPrimaryKeyIndex(entityName);

                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection as SqlCeConnection;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;
                    command.IndexName = ((SqlEntityInfo)Entities[entityName]).PrimaryKeyIndexName;
                    command.Transaction = CurrentTransaction as SqlCeTransaction;

                    using (var results = command.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable))
                    {

                        // seek on the PK
                        var found = results.Seek(DbSeekOptions.BeforeEqual, primaryKey);

                        if (!found)
                        {
                            throw new RecordNotFoundException("Cannot locate a record with the provided primary key.  Unable to delete the item");
                        }

                        results.Read();
                        results.Delete();
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        private static object GetKeyValue(FieldAttribute field, object item)
        {
            return field.PropertyInfo.GetValue(item, null);
        }

        private void FillEntity(Action<int, object> setter, string entityName, object item, out FieldAttribute identity)
        {
            // The reason for this somewhat convoluted Action parameter is that while the SqlCeUpdateableRecord (from Insert) 
            // and SqlCeResultSet (from Update) both contain a SetValue method, they don't share it on any common
            // interface.  using an Action allows us to share this code anyway.
            identity = null;

            var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;
            var fieldsOrdinal = GetOrdinalsField(entityName);

            foreach (var field in Entities[entityName].Fields)
            {
                var fieldOrdinal = fieldsOrdinal[field.FieldName];
                if (field.IsPrimaryKey)
                {
                    switch(keyScheme)
                    {
                        case KeyScheme.Identity:
                            identity = field;
                            break;
                        case KeyScheme.GUID:
                            var value = GetInstanceValue(field, item);
                            if (value.Equals(Guid.Empty))
                            {
                                value = Guid.NewGuid();
                                SetInstanceValue(field, item, value);
                            }
                            setter(fieldOrdinal, value);
                            break;
                    }
                }
                else if (typeof(ICustomSqlField).IsAssignableFrom(field.PropertyInfo.PropertyType))
                {
                    var iv = GetInstanceValue(field, item);

                    if (iv != null && iv != DBNull.Value)
                        iv = ((ICustomSqlField)iv).ToSqlValue();

                    if (iv == DBNull.Value && field.DefaultValue != null)
                    {
                        iv = field.DefaultValue;
                    }

                    setter(fieldOrdinal, iv);
                }
                else if (field.DataType == DbType.Object)
                {
                    // get serializer
                    var serializer = Entities[entityName].GetSerializer();
                    var propValue = field.PropertyInfo.GetValue(item, null);
                    var value = serializer.SerializeObjectField(field.FieldName, propValue);
                    setter(fieldOrdinal, value ?? DBNull.Value);
                }
                else if (field.DataType == DbType.DateTime)
                {
                    var dtValue = GetInstanceValue(field, item);

                    if (dtValue.Equals(DateTime.MinValue))
                    {
                        if ((!field.AllowsNulls) && (field.DefaultType != DefaultType.CurrentDateTime))
                        {
                            dtValue = SqlDateTime.MinValue;
                            setter(fieldOrdinal, dtValue);
                        }
                    }
                    else
                    {
                        setter(fieldOrdinal, dtValue);
                    }

                }
                else if (field.IsRowVersion)
                {
                    // read-only, so do nothing
                }
                else
                {
                    var iv = GetInstanceValue(field, item);
                    
                    if((iv == DBNull.Value) && (field.DefaultValue != null))
                    {
                        iv = field.DefaultValue;
                    }

                    setter(fieldOrdinal, iv);
                }
            }
        }

        private int GetIdentity(IDbConnection connection)
        {
            using (var command = new SqlCeCommand("SELECT @@IDENTITY", connection as SqlCeConnection))
            {
                command.Transaction = CurrentTransaction as SqlCeTransaction;
                var id = command.ExecuteScalar();
                return Convert.ToInt32(id);
            }
        }

        protected override void GetPrimaryKeyInfo(string entityName, out string indexName, out string columnName)
        {
            indexName = string.Empty;
            columnName = string.Empty;

            var connection = GetConnection(true);
            try
            {
                var sql = string.Format("SELECT INDEX_NAME FROM information_schema.indexes WHERE (TABLE_NAME = '{0}') AND (PRIMARY_KEY = 1)", entityName);

                using (var command = GetNewCommandObject())
                {
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.CommandText = sql;
                    command.Connection = connection;
                    indexName = command.ExecuteScalar() as string;
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }
        
        public int MaxDatabaseSizeInMb
        {
            get { return _maxSize; }
            set
            {
                // min of 128MB
                if (value < 128) throw new ArgumentOutOfRangeException();
                // max of 4GB
                if (value > 4096) throw new ArgumentOutOfRangeException();
                _maxSize = value;
            }
        }

        public override string ConnectionString
        {
            get
            {
                if (_connectionString == null)
                {
                    _connectionString = string.Format("Data Source={0};Persist Security Info=False;Max Database Size={1};", FileName, MaxDatabaseSizeInMb);

                    if (!string.IsNullOrEmpty(Password))
                    {
                        _connectionString += string.Format("Password={0};", Password);
                    }
                }
                return _connectionString;
            }
        }

        protected void ValidateIndex(IDbConnection connection, string indexName, string tableName, string fieldName, bool ascending)
        {
            var valid = false;

            var sql = string.Format("SELECT INDEX_NAME FROM information_schema.indexes WHERE (TABLE_NAME = '{0}') AND (COLUMN_NAME = '{1}')", tableName, fieldName);

            using (var command = new SqlCeCommand(sql, connection as SqlCeConnection))
            {
                command.Transaction = CurrentTransaction as SqlCeTransaction;
                var name = command.ExecuteScalar() as string;

                if (String.Compare(name, indexName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    valid = true;
                }

                if (!valid)
                {
                    sql = string.Format("CREATE INDEX {0} ON {1}({2} {3})",
                        indexName,
                        tableName,
                        fieldName,
                        ascending ? "ASC" : "DESC");

                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        public override string[] GetTableNames()
        {
            var names = new List<string>();

            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.Connection = connection;
                    const string sql = "SELECT table_name FROM information_schema.tables";
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            names.Add(reader.GetString(0));
                        }
                    }

                    return names.ToArray();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public override bool TableExists(string tableName)
        {
            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.Connection = connection;
                    var sql = string.Format("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{0}'", tableName);
                    command.CommandText = sql;
                    var count = Convert.ToInt32(command.ExecuteScalar());

                    return (count > 0);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        protected override void ValidateTable(IDbConnection connection, IEntityInfo entity)
        {
            // first make sure the table exists
            if (!TableExists(entity.EntityAttribute.NameInStore))
            {
                CreateTable(connection, entity);
                return;
            }

            using (var command = new SqlCeCommand())
            {
                command.Transaction = CurrentTransaction as SqlCeTransaction;
                command.Connection = connection as SqlCeConnection;

                foreach (var field in entity.Fields)
                {
                    if (ReservedWords.Contains(field.FieldName, StringComparer.InvariantCultureIgnoreCase))
                    {
                        throw new ReservedWordException(field.FieldName);
                    }

                    // yes, I realize hard-coded ordinals are not a good practice, but the SQL isn't changing, it's method specific
                    var sql = string.Format("SELECT column_name, "  // 0
                          + "data_type, "                       // 1
                          + "character_maximum_length, "        // 2
                          + "numeric_precision, "               // 3
                          + "numeric_scale, "                   // 4
                          + "is_nullable "
                          + "FROM information_schema.columns "
                          + "WHERE (table_name = '{0}' AND column_name = '{1}')",
                          entity.EntityAttribute.NameInStore, field.FieldName);

                    command.CommandText = sql;

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            // field doesn't exist - we must create it
                            var alter = new StringBuilder(string.Format("ALTER TABLE [{0}] ", entity.EntityAttribute.NameInStore));
                            alter.Append(string.Format("ADD [{0}] {1} {2}",
                                field.FieldName,
                                GetFieldDataTypeString(entity.EntityName, field),
                                GetFieldCreationAttributes(entity.EntityAttribute, field)));

                            using (var altercmd = new SqlCeCommand(alter.ToString(), connection as SqlCeConnection))
                            {
                                altercmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                VerifyIndex(entity.EntityName, entity.Fields.KeyField.FieldName, FieldSearchOrder.Ascending);
            }
        }

        private Dictionary<string, int> GetOrdinalsField(string entityName)
        {
            if (_entityOrdinal.ContainsKey(entityName))
                return _entityOrdinal[entityName];

            var ordinal = new Dictionary<string, int>();
            var connection = GetConnection(true);
            try
            {
                using (var command = new SqlCeCommand())
                {
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.Connection = connection as SqlCeConnection;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;

                    using (var reader = command.ExecuteReader())
                    {
                        foreach (var field in Entities[entityName].Fields)
                        {
                            ordinal.Add(field.FieldName, reader.GetOrdinal(field.FieldName));
                        }
                    }
                    _entityOrdinal.Add(entityName, ordinal);
                    command.Dispose();
                    return ordinal;
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

    }
}
