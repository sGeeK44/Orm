using System.Data;
using System.Data.SqlServerCe;
using Orm.Core.SqlQueries;

namespace Orm.SqlCe
{
    public class SqlCeFactory : ISqlFactory
    {
        public IDataParameter CreateParameter()
        {
            return new SqlCeParameter();
        }

        public string ParameterPrefix
        {
            get { return "@"; }
        }
    }
}
