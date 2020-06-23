using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql.Queries
{
    public class SetFieldList<TEntity> : List<SetField<TEntity>>, IClause
    {
        public string ToStatement(List<IDataParameter> @params)
        {
            if (Count == 0)
                throw new NotSupportedException("You have to set on field at least.");

            var result = new StringBuilder(" SET");
            for (var i = 0; i < Count; i++)
            {
                if (i != 0)
                    result.Append(",");
                result.Append(" " + this[i].ToStatement(@params));
            }

            return result.ToString();
        }
    }
}