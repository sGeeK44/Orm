using System.Data;

namespace Orm.Core.SqlQueries
{
    public interface ISqlFactory
    {
        IDataParameter CreateParameter();
        string ParameterPrefix { get; }
    }
}