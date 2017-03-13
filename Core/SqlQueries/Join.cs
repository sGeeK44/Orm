using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Orm.Core.Interfaces;

// ReSharper disable ConvertPropertyToExpressionBody

namespace Orm.Core.SqlQueries
{
    public class Join : IJoin
    {
        private const string DefaultJoinClause = "JOIN";
        private readonly IEntityInfo _entityRef;
        private readonly IEntityInfo _entityJoin;
        private readonly string _joinClause;

        public Join(IEntityInfo entityRef, IEntityInfo entityJoin)
            : this(DefaultJoinClause, entityRef, entityJoin) { }

        protected Join(string joinClause, IEntityInfo entityRef, IEntityInfo entityJoin)
        {
            _joinClause = joinClause;
            _entityRef = entityRef;
            _entityJoin = entityJoin;
        }

        public string ToStatement()
        {
            var result = new StringBuilder();
            result.Append(BuildJoin());
            result.Append(BuildOn());
            return result.ToString();
        }

        public string ToStatement(out List<IDataParameter> @params)
        {
            throw new NotSupportedException();
        }

        private string BuildJoin()
        {
            return string.Format(" {0} [{1}]", _joinClause, _entityJoin.EntityName);
        }

        private string BuildOn()
        {
            var reference = _entityRef.References.First(_ => _.ReferenceEntityType == _entityJoin.EntityType);
            return string.Format(" ON {0} = {1}", _entityRef.FullyQualifyFieldName(reference.LocalReferenceField),
                                                  _entityJoin.FullyQualifyFieldName(reference.ForeignReferenceField));
        }

        public Type EntityType1 { get { return _entityRef.EntityType; } }
        public Type EntityType2 { get { return _entityJoin.EntityType; } }
    }
}