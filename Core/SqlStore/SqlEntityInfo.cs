using System.Collections.Generic;

namespace Orm.Core.SqlStore
{
    public class SqlEntityInfo : EntityInfo
    {
        public SqlEntityInfo()
        {
            PrimaryKeyIndexName = null;
        }

        public string PrimaryKeyIndexName { get; set; }
        public string PrimaryKeyColumnName { get; set; }
        public List<string> IndexNames { get; set; }
    }
}
