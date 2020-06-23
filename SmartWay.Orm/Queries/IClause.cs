using System.Collections.Generic;
using System.Data;

namespace SmartWay.Orm.Queries
{
    public interface IClause
    {
        string ToStatement(List<IDataParameter> @params);
    }
}