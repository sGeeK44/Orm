using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Queries;
using SmartWay.Orm.Sql;
using SmartWay.Orm.Sql.Schema;
using SmartWay.Orm.Sqlite.Fields;

namespace SmartWay.Orm.Sqlite
{
    public class SqliteFactory : ISqlFactory
    {
        public string ParameterPrefix => "@";

        public IDataParameter CreateParameter()
        {
            return new SqliteParameter();
        }

        public IDataParameter CreateParameter(string paramName, object value)
        {
            if (value is Guid)
                value = value.ToString().ToLower();

            return new SqliteParameter
            {
                ParameterName = paramName,
                Value = value ?? DBNull.Value
            };
        }

        public IDataParameter CreateParameter(string paramName, ISqlConverter customField)
        {
            return new SqliteParameter
            {
                ParameterName = paramName,
                Value = customField.ToSqlValue()
            };
        }

        public IFieldPropertyFactory CreateFieldPropertyFactory()
        {
            return new FieldPropertyFactory();
        }

        public string AddParam(object paramToAdd, ICollection<IDataParameter> @params)
        {
            var paramName = $"{ParameterPrefix}p{@params.Count}";

            var customField = paramToAdd as ISqlConverter;
            var param = customField != null
                ? CreateParameter(paramName, customField)
                : CreateParameter(paramName, paramToAdd);
            @params.Add(param);
            return paramName;
        }

        public IDbCommand CreateCommand()
        {
            return new SqliteCommand();
        }

        public IDbAccessStrategy CreateDbAccessStrategy(ISqlDataStore sqlDataStore)
        {
            return new SqliteAccessStrategy(sqlDataStore);
        }

        public ISchemaChecker CreateSchemaChecker(ISqlDataStore sqlDataStore)
        {
            return new SqliteSchemaChecker(sqlDataStore);
        }

        public static ISqlDataStore CreateStore(string datasource)
        {
            return CreateStore(datasource, null);
        }

        public static ISqlDataStore CreateStore(string datasource, string password)
        {
            return new SqlDataStore(new SqliteDbEngine(datasource, password), new SqliteFactory());
        }
    }
}