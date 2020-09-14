using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;
using SmartWay.Orm.Entity.Constraints;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Sql;

namespace SmartWay.Orm.Sqlite
{
    public class SqliteAccessStrategy : IDbAccessStrategy
    {
        private readonly ISqlDataStore _datastore;

        public SqliteAccessStrategy(ISqlDataStore dataStore)
        {
            _datastore = dataStore;
        }

        public object SelectByPrimayKey(Type objectType, PrimaryKey primaryKey, object value)
        {
            var condition = _datastore.Condition(objectType, primaryKey.FieldName, value, FilterOperator.Equals);
            return _datastore.Select(objectType).Where(condition).GetValues().FirstOrDefault();
        }

        public void Insert(object item)
        {
            var itemType = item.GetType();
            var entityName = _datastore.Entities.GetNameForType(itemType);

            if (entityName == null) throw new EntityNotFoundException(item.GetType());

            using var command = ToInsertCommand(_datastore.Entities[entityName], item);
            OrmDebug.Info(command.CommandText);
            command.ExecuteNonQuery();

            // did we have an identity field?  If so, we need to update that value in the item
            var primaryKey = _datastore.Entities[entityName].PrimaryKey;
            if (primaryKey == null)
                return;

            if (primaryKey.KeyScheme == KeyScheme.Identity)
            {
                var id = GetIdentity(primaryKey);
                primaryKey.SetEntityValue(item, id);
            }

            _datastore.Cache?.Cache(item, primaryKey.GetEntityValue(item));
        }

        public void Update(object item)
        {
            var itemType = item.GetType();

            var entityName = _datastore.Entities.GetNameForType(itemType);

            if (entityName == null) throw new EntityNotFoundException(itemType);

            if (_datastore.Entities[entityName].PrimaryKey == null)
                throw new PrimaryKeyRequiredException(
                    "A primary key is required on an Entity in order to perform Updates");

            using var command = ToUpdateCommand(_datastore.Entities[entityName], item);
            OrmDebug.Info(command.CommandText);
            command.ExecuteNonQuery();
        }

        public void Delete(object item)
        {
            var itemType = item.GetType();

            var entityName = _datastore.Entities.GetNameForType(itemType);

            if (entityName == null) throw new EntityNotFoundException(itemType);

            if (_datastore.Entities[entityName].PrimaryKey == null)
                throw new PrimaryKeyRequiredException(
                    "A primary key is required on an Entity in order to perform delete");

            using var command = ToDeleteCommand(_datastore.Entities[entityName], item);
            OrmDebug.Info(command.CommandText);
            command.ExecuteNonQuery();
        }

        public string[] GetTableNames()
        {
            var connection = _datastore.GetReadConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
            OrmDebug.Info(command.CommandText);

            using var reader = command.ExecuteReader();
            var names = new List<string>();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                if (name == "sqlite_sequence")
                    continue;

                names.Add(name);
            }

            return names.ToArray();
        }

        public void ValidateTable(IEntityInfo entity)
        {
        }

        public string CreateTable(IEntityInfo entity)
        {
            var sql = new StringBuilder();
            sql.AppendFormat("CREATE TABLE [{0}] (", entity.GetNameInStore());

            var first = true;

            foreach (var field in entity.Fields)
            {
                if (first)
                    first = false;
                else
                    sql.Append(", ");

                sql.AppendFormat(field.GetFieldDefinition());
            }

            foreach (var field in entity.Fields)
            {
                var constraint = field.GetFieldConstraint();
                if (string.IsNullOrEmpty(constraint))
                    continue;

                sql.AppendFormat(", {0}", constraint);
            }

            sql.Append(")");

            OrmDebug.Info(sql.ToString());
            return sql.ToString();
        }

        public string GetFieldDefinition(Field field)
        {
            return $"[{field.FieldName}] {field.GetFieldDefinition()}";
        }

        private IDbCommand ToInsertCommand(IEntityInfo entity, object item)
        {
            const string sqlCommandText = "INSERT INTO [{0}] ({1}) VALUES ({2})";
            StringBuilder key = null;
            StringBuilder value = null;
            var @params = new List<IDataParameter>();

            var sqlFactory = _datastore.SqlFactory;
            foreach (var field in entity.Fields)
            {
                if (field.IsPrimaryKey && ((PrimaryKey) field).KeyScheme == KeyScheme.Identity)
                    continue;

                var fieldValue = field.ToSqlValue(item);
                var paramName = sqlFactory.AddParam(fieldValue, @params);
                key = AddValue(key, field.ToInsertStatement());
                value = AddValue(value, paramName);
            }

            var connection = _datastore.GetWriteConnection();
            var insert = connection.CreateCommand();
            insert.CommandText = string.Format(sqlCommandText, entity.GetNameInStore(), key, value);
            SetCommandParam(@params, insert);
            return insert;
        }

        public IDbCommand ToUpdateCommand(IEntityInfo entity, object item)
        {
            const string sqlCommandText = "UPDATE [{0}] SET {1} WHERE {2}";
            StringBuilder value = null;
            StringBuilder where = null;
            var @params = new List<IDataParameter>();

            var sqlFactory = _datastore.SqlFactory;
            foreach (var field in entity.Fields)
            {
                var fieldValue = field.ToSqlValue(item);
                var paramName = sqlFactory.AddParam(fieldValue, @params);
                if (field.IsPrimaryKey)
                {
                    where = AddWhere(where, $"{field.FieldName} = {paramName}");
                    continue;
                }

                value = AddValue(value, $"{field.FieldName} = {paramName}");
            }

            var connection = _datastore.GetWriteConnection();
            var update = connection.CreateCommand();
            update.CommandText = string.Format(sqlCommandText, entity.GetNameInStore(), value, where);
            SetCommandParam(@params, update);
            return update;
        }

        public IDbCommand ToDeleteCommand(IEntityInfo entity, object item)
        {
            const string sqlCommandText = "DELETE FROM [{0}] WHERE {1}";
            StringBuilder where = null;
            var @params = new List<IDataParameter>();

            var sqlFactory = _datastore.SqlFactory;
            foreach (var field in entity.Fields)
            {
                var fieldValue = field.ToSqlValue(item);
                var paramName = sqlFactory.AddParam(fieldValue, @params);
                if (!field.IsPrimaryKey)
                    continue;

                where = AddWhere(where, $"{field.FieldName} = {paramName}");
            }

            var connection = _datastore.GetWriteConnection();
            var delete = connection.CreateCommand();
            delete.CommandText = string.Format(sqlCommandText, entity.GetNameInStore(), where);
            SetCommandParam(@params, delete);
            return delete;
        }

        private int GetIdentity(PrimaryKey primaryKey)
        {
            var connection = _datastore.GetReadConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                $"select seq from sqlite_sequence where name=\"{primaryKey.Entity.GetNameInStore()}\"";
            var id = command.ExecuteScalar();
            return Convert.ToInt32(id);
        }

        private static void SetCommandParam(IEnumerable<IDataParameter> @params, IDbCommand insert)
        {
            foreach (var param in @params) insert.Parameters.Add(param);
        }

        private static StringBuilder AddWhere(StringBuilder list, string paramName)
        {
            return Join(list, paramName, "AND");
        }

        private static StringBuilder AddValue(StringBuilder list, string valueToAdd)
        {
            return Join(list, valueToAdd, ",");
        }

        private static StringBuilder Join(StringBuilder list, string valueToAdd, string join)
        {
            if (list == null)
                return new StringBuilder(valueToAdd);

            list.AppendFormat("{0} {1}", join, valueToAdd);
            return list;
        }
    }
}