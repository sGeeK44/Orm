using System;

namespace Orm.Core.SqlQueries
{
    public interface IJoin : ISqlClause
    {
        /// <summary>
        /// get type of first entity involve
        /// </summary>
        Type EntityType1 { get; }

        /// <summary>
        /// get type of second entity involve
        /// </summary>
        Type EntityType2 { get; }
    }
}