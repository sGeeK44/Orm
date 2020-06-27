using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql.Queries
{
    public class QuerySet
    {
        private readonly IDataStore _datastore;
        private readonly GroupBy _groupBy;
        private readonly OrderBy _orderBy;
        private IClause _where;

        internal QuerySet(IDataStore datastore, EntityInfoCollection entities, Type entityInvolve)
        {
            _datastore = datastore;
            _where = new Where();
            _orderBy = new OrderBy();
            _groupBy = new GroupBy();
            Entities = entities;
            EntityInvolve = new Dictionary<string, IEntityInfo>();
            Entity = AddInvolveEntity(entityInvolve);
            JoinList = new List<IJoin>();
        }

        public IEntityInfo Entity { get; }

        public List<IJoin> JoinList { get; }

        public EntityInfoCollection Entities { get; set; }

        public Dictionary<string, IEntityInfo> EntityInvolve { get; }

        public void Where(IFilter filter)
        {
            _where = new Where(filter);
        }

        public void Where<TIEntity>(Expression<Func<TIEntity, bool>> predicate) where TIEntity : class
        {
            var filterBuilder = new FilterBuilder<TIEntity>(_datastore.Entities, new FilterFactory(_datastore), predicate);
            _where = filterBuilder.Build();
        }

        public void Join<TEntity1, TEntity2>()
        {
            var entityRef = AddInvolveEntity<TEntity1>();
            var entityJoin = AddInvolveEntity<TEntity2>();
            JoinList.Add(new Join(entityRef, entityJoin));
        }

        public void Join(ICondition condition)
        {
            var entityRef = AddInvolveEntity(condition.Entity1.EntityType);
            var entityJoin = AddInvolveEntity(condition.Entity2.EntityType);
            JoinList.Add(new Join(entityRef, entityJoin, condition));
        }

        public void LeftJoin<TEntity1, TEntity2>()
        {
            var entityRef = AddInvolveEntity<TEntity1>();
            var entityJoin = AddInvolveEntity<TEntity2>();
            JoinList.Add(new LeftJoin(entityRef, entityJoin));
        }

        public void LeftJoin(ICondition condition)
        {
            var entityRef = AddInvolveEntity(condition.Entity1.EntityType);
            var entityJoin = AddInvolveEntity(condition.Entity2.EntityType);
            JoinList.Add(new LeftJoin(entityRef, entityJoin, condition));
        }

        public string ToStatement(List<IDataParameter> @params)
        {
            var result = new StringBuilder();
            result.Append(FromStatement());
            result.Append(JoinStatement(@params));
            result.Append(_where.ToStatement(@params));
            result.Append(_groupBy.ToStatement());
            result.Append(_orderBy.ToStatement());
            var sql = result.Append(";").ToString();
            OrmDebug.Trace(sql);
            return sql;
        }

        protected string FromStatement()
        {
            return $"FROM [{Entity.GetNameInStore()}]";
        }

        protected string JoinStatement(List<IDataParameter> @params)
        {
            return JoinList.Aggregate(string.Empty, (current, join) => current + " " + join.ToStatement(@params));
        }

        private IEntityInfo AddInvolveEntity<TEntity>()
        {
            return AddInvolveEntity(typeof(TEntity));
        }

        private IEntityInfo AddInvolveEntity(Type entityType)
        {
            var entityName = Entities.GetNameForType(entityType);
            if (string.IsNullOrEmpty(entityName))
                throw new NotSupportedException($"Entity type must be added in datastore. Type:{entityType}.");

            var entity = Entities[entityName];
            if (!EntityInvolve.ContainsKey(entityName))
                EntityInvolve.Add(entityName, entity);
            return entity;
        }

        public void AddOrderBy(ColumnValue field)
        {
            _orderBy.AddField(field);
        }

        public void AddOrderByDesc(ColumnValue field)
        {
            _orderBy.AddFieldDesc(field);
        }

        public void AddGroupBy(ColumnValue[] columns)
        {
            _groupBy.AddColumn(columns);
        }
    }
}