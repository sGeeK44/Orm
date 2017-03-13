using Orm.Core.Attributes;

namespace Orm.Core.Entity
{
    /// <summary>
    /// Encaspulate common behavior for standard entity
    /// </summary>
    public abstract class EntityBase : IDistinctableEntity
    {
        public const long NullId = -1;
        public const string IdColumnName = "id";

        protected EntityBase()
        {
            Id = NullId;
        }

        /// <summary>
        /// Get unique object identifier
        /// </summary>
        [Field(FieldName = IdColumnName, IsPrimaryKey = true, AllowsNulls = false)]
        public long Id { get; set; }
    }
}
