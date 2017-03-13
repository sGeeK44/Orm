using Orm.Core.Interfaces;

namespace Orm.Core.SqlQueries
{
    public class LeftJoin : Join
    {
        private const string JoinClause = "LEFT JOIN";

        public LeftJoin(IEntityInfo entityRef, IEntityInfo entityJoin)
            : base(JoinClause, entityRef, entityJoin) { }
    }
}