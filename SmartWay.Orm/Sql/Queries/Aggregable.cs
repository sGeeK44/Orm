using System.Linq;
using System.Text;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql.Queries
{
    public class Aggregable : ISelectable
    {
        private IFilter[] _aggregableFunction;
        private ColumnValue[] _columns;

        private Aggregable()
        {
        }

        private Aggregable(IFilter aggregableFunction, params ColumnValue[] columns)
        {
            _aggregableFunction = new[] {aggregableFunction};
            _columns = columns;
        }

        public string SelectStatement()
        {
            var statement = new StringBuilder("SELECT ");

            for (var i = 0; i < _aggregableFunction.Length; i++)
            {
                var aggregableFilter = _aggregableFunction[i];

                if (i != 0)
                    statement.Append(", ");

                statement.Append(aggregableFilter.ToStatement(null));
            }

            if (_columns == null || _columns.Length == 0)
                return statement.ToString();

            foreach (var column in _columns)
            {
                statement.Append(", ");
                statement.Append(column.ToSelectStatement());
            }

            return statement.ToString();
        }

        public static Aggregable CreateTableCount(params ColumnValue[] columns)
        {
            return new Aggregable(new WildCardCount(), columns);
        }

        public static Aggregable CreateColumnCount(ColumnValue column)
        {
            return new Aggregable(column.ToCountedColumn());
        }

        public static Aggregable CreateSum(params ColumnValue[] columns)
        {
            var result = new Aggregable();
            result._aggregableFunction = columns.Select(_ => _.ToSumColumn()).ToArray();
            result._columns = new ColumnValue[0];
            return result;
        }
    }
}