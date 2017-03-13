using Orm.Core.Repositories;

namespace Orm.Core.Entity
{
    /// <summary>
    /// Wrap behaviour Id and entity object for a reference
    /// </summary>
    /// <typeparam name="TReference"></typeparam>
    public class NullableReferenceHolder<TReference> : ReferenceHolder<TReference> where TReference : class, IDistinctableEntity
    {
        private long? _id;

        /// <summary>
        /// Create a new instance of Reference holder
        /// </summary>
        /// <param name="repository">Repository used when need to get object instance by id</param>
        public NullableReferenceHolder(IRepository<TReference> repository)
            : base(repository) { }

        /// <summary>
        /// Get or set new reference Id
        /// </summary>
        public long? Id
        {
            get { return _id; }
            set
            {
                _id = value;
                SetObject(_id);
            }
        }

        protected override void SetId(TReference value)
        {
            _id = value != null ? value.Id : (long?)null;
        }
    }
}