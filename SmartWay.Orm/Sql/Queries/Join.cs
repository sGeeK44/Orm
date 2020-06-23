using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql.Queries
{
    public class Join : IJoin
    {
        private const string DefaultJoinClause = "JOIN";
        private readonly IEntityInfo _entityJoin;
        private readonly IEntityInfo _entityRef;
        private readonly IFilter _filter;
        private readonly string _joinClause;

        public Join(IEntityInfo entityRef, IEntityInfo entityJoin)
            : this(DefaultJoinClause, entityRef, entityJoin)
        {
        }

        public Join(IEntityInfo entitRef, IEntityInfo entityJoin, IFilter filter)
            : this(DefaultJoinClause, entitRef, entityJoin)
        {
            _filter = filter;
        }

        public Join(string joinClause, IEntityInfo entityRef, IEntityInfo entityJoin, IFilter filter)
            : this(joinClause, entityRef, entityJoin)
        {
            _filter = filter;
        }

        protected Join(string joinClause, IEntityInfo entityRef, IEntityInfo entityJoin)
        {
            _joinClause = joinClause;
            _entityRef = entityRef;
            _entityJoin = entityJoin;
        }

        public string ToStatement(List<IDataParameter> @params)
        {
            var result = new StringBuilder();
            result.Append(BuildJoin());
            result.Append(BuildOn(@params));
            return result.ToString();
        }

        public Type EntityType1 => _entityRef.EntityType;
        public Type EntityType2 => _entityJoin.EntityType;

        public string ToStatement()
        {
            throw new NotImplementedException();
        }

        private string BuildJoin()
        {
            return $"{_joinClause} [{_entityJoin.GetNameInStore()}]";
        }

        private string BuildOn(List<IDataParameter> @params)
        {
            var result = "ON ";
            if (_filter != null)
                return result + _filter.ToStatement(@params);

            var reference = _entityRef.References.First(_ => _.ReferenceEntityType == _entityJoin.EntityType);
            var localField = _entityRef.Fields.First(_ => _.FieldName == reference.LocalReferenceField);
            var foreignField = _entityJoin.Fields.First(_ => _.FieldName == reference.ForeignReferenceField);
            return $" ON {localField.FullFieldName} = {foreignField.FullFieldName}";
        }
    }
}