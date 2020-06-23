using System.Collections.Generic;
using System.Data;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Sql;
using SmartWay.Orm.Sql.Schema;

namespace SmartWay.Orm.Queries
{
    public interface ISqlFactory
    {
        string ParameterPrefix { get; }
        IDataParameter CreateParameter();
        IDataParameter CreateParameter(string paramName, object value);
        IDataParameter CreateParameter(string paramName, ISqlConverter customField);
        IFieldPropertyFactory CreateFieldPropertyFactory();
        string AddParam(object paramToAdd, ICollection<IDataParameter> @params);
        IDbCommand CreateCommand();
        IDbAccessStrategy CreateDbAccessStrategy(ISqlDataStore sqlDataStore);
        ISchemaChecker CreateSchemaChecker(ISqlDataStore sqlDataStore);
    }
}