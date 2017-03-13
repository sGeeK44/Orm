using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Orm.Core.Filters;
using Orm.Core.Interfaces;

// ReSharper disable UseStringInterpolation
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable ConvertPropertyToExpressionBody

namespace Orm.Core.SqlQueries
{
    public class Select<TEntity, TIEntity> : Select, IJoinable<TIEntity>, ISelect<TIEntity>
        where TEntity : TIEntity
        where TIEntity : class
    {
        public Select(IDataStore datastore, EntityInfoCollection entities)
            : base(datastore, entities, typeof(TEntity)) { }

        TIEntity ISelect<TIEntity>.Deserialize(IDataReader results, IEntityCache entityCache)
        {
            return Deserialize(results, entityCache) as TIEntity;
        }

        public new IOrderableSqlQuery<TIEntity> Where(ISqlClause sqlPartClause)
        {
            base.Where(sqlPartClause);
            return this;
        }

        public new IEnumerable<TIEntity> Execute()
        {
            return Datastore.Execute((ISelect<TIEntity>)this);
        }

        public new ISqlQuery<TIEntity> OrderBy(params ColumnValue[] fields)
        {
            base.OrderBy(fields);
            return this;
        }

        public new IOrderableSqlQuery<TIEntity> Where(IFilter filter)
        {
            base.Where(filter);
            return this;
        }

        public new IJoinable<TIEntity> Join<TEntity1, TEntity2>()
        {
            base.Join<TEntity1, TEntity2>();
            return this;
        }

        public new IJoinable<TIEntity> LeftJoin<TEntity1, TEntity2>()
        {
            base.LeftJoin<TEntity1, TEntity2>();
            return this;
        }
    }
    
    public class Select : ISelect, IJoinable
    {
        private readonly List<IJoin> _joinList = new List<IJoin>();
        private ISqlClause _where;
        private readonly OrderBy _orderBy;
        private readonly IEntityInfo _entity;
        private readonly Dictionary<string, IEntityInfo> _entityInvolve;

        public Select(IDataStore datastore, EntityInfoCollection entities, Type entityInvolve)
        {
            Datastore = datastore;
            Entities = entities;
            _where = new Where();
            _orderBy = new OrderBy();
            _entityInvolve = new Dictionary<string, IEntityInfo>();
            _entity = AddInvolveEntity(entityInvolve);
        }

        protected IDataStore Datastore { get; set; }
        private EntityInfoCollection Entities { get; set; }

        public IOrderableSqlQuery Where(IFilter filter)
        {
            return Where(new Where(filter));
        }

        public IOrderableSqlQuery Where(ISqlClause sqlPartClause)
        {
            _where = sqlPartClause ?? new Where();
            return this;
        }

        public ISqlQuery OrderBy(params ColumnValue[] fields)
        {
            _orderBy.SetFields(fields);
            return this;
        }

        public IJoinable Join<TEntity1, TEntity2>()
        {
            var entityRef = AddInvolveEntity<TEntity1>();
            var entityJoin = AddInvolveEntity<TEntity2>();
            _joinList.Add(new Join(entityRef, entityJoin));
            return this;
        }

        public IJoinable LeftJoin<TEntity1, TEntity2>()
        {
            var entityRef = AddInvolveEntity<TEntity1>();
            var entityJoin = AddInvolveEntity<TEntity2>();
            _joinList.Add(new LeftJoin(entityRef, entityJoin));
            return this;
        }

        public IEnumerable<object> Execute()
        {
            return Datastore.Execute(this);
        }

        public string ToStatement()
        {
            throw new NotSupportedException();
        }

        public string ToStatement(out List<IDataParameter> @params)
        {
            var result = new StringBuilder();
            result.Append(SelectStatement());
            result.Append(FromStatement());
            result.Append(JoinStatement());
            result.Append(_where.ToStatement(out @params));
            result.Append(_orderBy.ToStatement());
            var sql = result.Append(";").ToString();
            Debug.WriteLine(sql);
            return sql;
        }

        public int Offset { get; set; }

        public object Deserialize(IDataReader results, IEntityCache entityCache)
        {
            var serializer = Entity.GetSerializer();
            serializer.UseFullName = true;
            serializer.EntityCache = entityCache;
            var item = serializer.Deserialize(results);
            foreach (var join in _joinList)
            {
                serializer.FillReference(join.EntityType2, item, results);
            }
            return item;
        }

        public IEntityInfo Entity { get { return _entity; } }

        private string SelectStatement()
        {
            var result = new StringBuilder("SELECT ");
            var entityInvolves = _entityInvolve.Values.ToList();
            for (var index = 0; index < entityInvolves.Count; index++)
            {
                var entity = entityInvolves[index];
                result.Append(GetSelectFieldList(entity, index == 0));
            }
            return result.ToString();
        }

        protected string FromStatement()
        {
            return string.Format(" FROM [{0}]", _entity.EntityName);
        }

        protected string JoinStatement()
        {
            return _joinList.Aggregate(string.Empty, (current, t) => current + (t.ToStatement()));
        }

        private static string GetSelectFieldList(IEntityInfo entity, bool isFirstEntity)
        {
            var result = new StringBuilder();
            for (var i = 0; i < entity.Fields.Count; i++)
            {
                var field = entity.Fields[i];
                if (!isFirstEntity || i != 0) result.Append(", ");
                result.Append(field.FullFieldName + " AS " + field.AliasFieldName);
            }
            return result.ToString();
        }

        private IEntityInfo AddInvolveEntity<TEntity>()
        {
            return AddInvolveEntity(typeof(TEntity));
        }

        private IEntityInfo AddInvolveEntity(Type entityType)
        {
            var entityName = Entities.GetNameForType(entityType);
            var entity = Entities[entityName];
            if (!_entityInvolve.ContainsKey(entityName))
                _entityInvolve.Add(entityName, entity);
            return entity;
        }
    }
}