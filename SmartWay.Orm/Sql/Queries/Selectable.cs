using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql.Queries
{
    public class Selectable<TIEntity> : IClause, IJoinable<TIEntity> where TIEntity : class
    {
        public Selectable(IDataStore datastore, EntityInfoCollection entities)
        {
            Datastore = datastore;
            Query = new QuerySet(datastore, entities, typeof(TIEntity));
        }

        public Selectable(IDataStore datastore, EntityInfoCollection entities, Type entityInvolve)
        {
            Datastore = datastore;
            Query = new QuerySet(datastore, entities, entityInvolve);
        }

        private IDataStore Datastore { get; }
        private QuerySet Query { get; }
        private ISelectable SelectStatement { get; set; }

        public IEntityInfo Entity => Query.Entity;

        public List<IJoin> JoinList => Query.JoinList;

        public EntityInfoCollection Entities => Query.Entities;

        public Dictionary<string, IEntityInfo> EntityInvolve => Query.EntityInvolve;

        public string ToStatement(List<IDataParameter> @params)
        {
            var result = new StringBuilder();
            if (SelectStatement != null)
            {
                result.Append(SelectStatement.SelectStatement());
                result.Append(" ");
            }

            result.Append(Query.ToStatement(@params));
            return result.ToString();
        }

        public IJoinable<TIEntity> Join<TEntity1, TEntity2>()
        {
            Query.Join<TEntity1, TEntity2>();
            return this;
        }

        public IJoinable<TIEntity> Join(ICondition condition)
        {
            Query.Join(condition);
            return this;
        }

        public IJoinable<TIEntity> LeftJoin<TEntity1, TEntity2>()
        {
            Query.LeftJoin<TEntity1, TEntity2>();
            return this;
        }

        public IJoinable<TIEntity> LeftJoin(ICondition condition)
        {
            Query.LeftJoin(condition);
            return this;
        }

        public IAggragableQuery<TIEntity> Where(IFilter filter)
        {
            Query.Where(filter);
            return this;
        }

        public IAggragableQuery<TIEntity> Where(Expression<Func<TIEntity, bool>> predicate)
        {
            Query.Where(predicate);
            return this;
        }

        public IOrderableQuery<TIEntity> GroupBy(params ColumnValue[] columns)
        {
            Query.AddGroupBy(columns);
            return this;
        }

        public IOrderedQuery<TIEntity> OrderBy(ColumnValue field)
        {
            Query.AddOrderBy(field);
            return this;
        }

        public IOrderedQuery<TIEntity> OrderByDesc(ColumnValue field)
        {
            Query.AddOrderByDesc(field);
            return this;
        }

        public IOrderedQuery<TIEntity> ThenBy(ColumnValue field)
        {
            Query.AddOrderBy(field);
            return this;
        }

        public IOrderedQuery<TIEntity> ThenByDesc(ColumnValue field)
        {
            Query.AddOrderByDesc(field);
            return this;
        }

        public IEnumerable<TIEntity> GetValues()
        {
            var select = new Select<TIEntity>(this);
            SelectStatement = select;
            return Datastore.ExecuteQuery(this, select);
        }

        public IEnumerable<TIEntity> Top(int quantity)
        {
            var select = new SelectTop<TIEntity>(this, quantity);
            SelectStatement = select;
            return Datastore.ExecuteQuery(this, select);
        }

        public int Count()
        {
            var countedEntity = Query.Entity;
            var columnValue = Datastore.ToColumnValue(countedEntity, countedEntity.PrimaryKey.FieldName);
            SelectStatement = Aggregable.CreateColumnCount(columnValue);
            return Datastore.ExecuteScalar(this);
        }

        public int Delete()
        {
            SelectStatement = new Delete();
            return Datastore.ExecuteNonQuery(this);
        }

        public int Update()
        {
            throw new NotSupportedException("Can not perform update on selectable query.");
        }

        public IEnumerable<AggregableResultRow> Count(params ColumnValue[] columns)
        {
            SelectStatement = Aggregable.CreateTableCount(columns);
            return Datastore.ExecuteQuery(this);
        }

        public IEnumerable<AggregableResultRow> Sum(params ColumnValue[] columns)
        {
            SelectStatement = Aggregable.CreateSum(columns);
            return Datastore.ExecuteQuery(this);
        }
    }
}