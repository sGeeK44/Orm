using System.Collections.Generic;
using System.Data;

namespace Orm.Core.SqlQueries
{
    public interface ISqlClause
    {
        string ToStatement();
        string ToStatement(out List<IDataParameter> @params);
    }
}