using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql.Queries
{
    public class Delete : ISelectable
    {
        public string SelectStatement()
        {
            return "DELETE";
        }
    }
}