using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql.Queries
{
    public class Updatable<TIEntity> : IClause, IUpdatable<TIEntity> where TIEntity : class
    {
        private const string UnsupportedSelectClauseUse = "Can not select values on update query";
        private const string UnsupportedDeleteClauseUse = "Can not delete value on update query";
        private const string UnsupportedOrderClauseUse = "Can not order values on update query";
        private const string UnsupportedAggregateClauseUse = "Can not aggregate values on update query";

        private readonly IDataStore _datastore;
        private readonly string _entityName;
        private readonly SetFieldList<TIEntity> _setFieldList;
        private IClause _where;

        public Updatable(IDataStore datastore)
        {
            _datastore = datastore;
            _where = new Where();
            _setFieldList = new SetFieldList<TIEntity>();
            _entityName = datastore.Entities.GetNameForType(typeof(TIEntity));

            if (string.IsNullOrEmpty(_entityName))
                throw new NotSupportedException($"Entity type must be added in datastore. Type:{typeof(TIEntity)}.");
        }

        public string ToStatement(List<IDataParameter> @params)
        {
            var result = new StringBuilder();
            result.Append($"UPDATE [{_entityName}]");
            result.Append(_setFieldList.ToStatement(@params));
            result.Append(_where.ToStatement(@params));
            var sql = result.Append(";").ToString();
            OrmDebug.Trace(sql);
            return sql;
        }

        public IUpdatable<TIEntity> Set(string columnName, object value)
        {
            _setFieldList.Add(new SetField<TIEntity>(_datastore, columnName, value));
            return this;
        }

        public IEnumerable<TIEntity> GetValues()
        {
            throw new NotSupportedException(UnsupportedSelectClauseUse);
        }

        public IEnumerable<TIEntity> Top(int quantity)
        {
            throw new NotSupportedException(UnsupportedSelectClauseUse);
        }

        public int Count()
        {
            throw new NotSupportedException(UnsupportedSelectClauseUse);
        }

        public int Delete()
        {
            throw new NotSupportedException(UnsupportedDeleteClauseUse);
        }

        public int Update()
        {
            return _datastore.ExecuteNonQuery(this);
        }

        public IEnumerable<AggregableResultRow> Count(params ColumnValue[] columns)
        {
            throw new NotSupportedException(UnsupportedSelectClauseUse);
        }

        public IEnumerable<AggregableResultRow> Sum(params ColumnValue[] columns)
        {
            throw new NotSupportedException(UnsupportedSelectClauseUse);
        }

        public IOrderedQuery<TIEntity> ThenBy(ColumnValue field)
        {
            throw new NotSupportedException(UnsupportedOrderClauseUse);
        }

        public IOrderedQuery<TIEntity> ThenByDesc(ColumnValue field)
        {
            throw new NotSupportedException(UnsupportedOrderClauseUse);
        }

        public IOrderedQuery<TIEntity> OrderBy(ColumnValue field)
        {
            throw new NotSupportedException(UnsupportedOrderClauseUse);
        }

        public IOrderedQuery<TIEntity> OrderByDesc(ColumnValue field)
        {
            throw new NotSupportedException(UnsupportedOrderClauseUse);
        }

        public IOrderableQuery<TIEntity> GroupBy(params ColumnValue[] columns)
        {
            throw new NotSupportedException(UnsupportedAggregateClauseUse);
        }

        public IAggragableQuery<TIEntity> Where(IFilter filter)
        {
            _where = new Where(filter);
            return this;
        }
    }
}