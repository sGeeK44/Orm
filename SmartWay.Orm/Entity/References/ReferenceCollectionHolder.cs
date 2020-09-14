using System;
using System.Collections.Generic;
using SmartWay.Orm.Repositories;

namespace SmartWay.Orm.Entity.References
{
    /// <summary>
    ///     Wrap behaviour Id and entity object for a reference
    /// </summary>
    /// <typeparam name="TReference">Type of object collection.</typeparam>
    /// <typeparam name="TEntity">Type of entity linked to object collection</typeparam>
    public class ReferenceCollectionHolder<TEntity, TReference> where TEntity : IDistinctableEntity
    {
        private readonly TEntity _entity;
        private readonly IRepository<TReference> _repository;
        private Lazy<List<TReference>> _objectList;

        /// <summary>
        ///     Create a new instance of Reference Collection holder
        /// </summary>
        /// <param name="repository">Repository used when need to get object collection instance by id</param>
        /// <param name="entity">Entity linked</param>
        public ReferenceCollectionHolder(IRepository<TReference> repository, TEntity entity)
        {
            _entity = entity;
            _repository = repository;
            RefreshObjectCollection();
        }

        /// <summary>
        ///     Create a new instance of Reference Collection holder
        /// </summary>
        /// <param name="repoQuery">Function that be used to getting bask objects</param>
        public ReferenceCollectionHolder(Func<List<TReference>> repoQuery)
        {
            _objectList = new Lazy<List<TReference>>(() => repoQuery() ?? new List<TReference>());
        }

        /// <summary>
        ///     Get object collection linked
        /// </summary>
        public List<TReference> ObjectCollection
        {
            get => _objectList.Value;
            set => _objectList = new Lazy<List<TReference>>(new List<TReference>(value));
        }

        private void RefreshObjectCollection()
        {
            _objectList = new Lazy<List<TReference>>(() =>
                _repository.GetAllReference<TEntity>(_entity.GetPkValue()) ?? new List<TReference>());
        }
    }
}