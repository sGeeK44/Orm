using SmartWay.Orm.Repositories;

namespace SmartWay.Orm.Entity.References
{
    /// <summary>
    ///     Wrap behaviour Id and entity object for a reference
    /// </summary>
    /// <typeparam name="TReference"></typeparam>
    /// <typeparam name="TPk">Type of primary key field</typeparam>
    public class ReferenceHolder<TReference, TPk> where TReference : class, IDistinctableEntity
    {
        private readonly IRepository<TReference> _repository;
        private TPk _id;
        private Lazy<TReference> _object;

        /// <summary>
        ///     Create a new instance of Reference holder
        /// </summary>
        /// <param name="repository">Repository used when need to get object instance by id</param>
        public ReferenceHolder(IRepository<TReference> repository)
        {
            _repository = repository;
            Set(null);
        }

        /// <summary>
        ///     Get or set new reference Id
        /// </summary>
        public TPk Id
        {
            get
            {
                if (_object.IsLoaded)
                    return (TPk)_object.Value?.GetPkValue();

                return _id;
            }
            set => Set(value);
        }

        protected void Set(TPk id)
        {
            _id = id;
            if (_id == null)
            {
                Set(null);
                return;
            }

            _object = new Lazy<TReference>(() => _repository?.GetByPk(id));
        }

        /// <summary>
        ///     Get or set new reference object
        /// </summary>
        public TReference Object
        {
            get => _object.Value;
            set => Set(value);
        }

        protected void Set(TReference value)
        {
            _object = new Lazy<TReference>(value);
            if (value == null)
            {
                _id = default;
                return;
            }
            _id = (TPk)value.GetPkValue() ?? default;
        }
    }
}