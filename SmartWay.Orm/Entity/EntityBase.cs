using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SmartWay.Orm.Attributes;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity.Constraints;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Repositories;

namespace SmartWay.Orm.Entity
{
    /// <summary>
    ///     Encaspulate common behavior for standard entity
    /// </summary>
    public abstract class EntityBase<TIEntity> : IDistinctableEntity, IPersistableEntity, IEntity, IObservableEntity,
        IEquatable<EntityBase<TIEntity>> where TIEntity : class
    {
        public const long NullId = -1;
        public const string IdColumnName = "id";
        public const string GuidColumnName = "guid";

        private List<PropertyInfo> _dbField;
        private List<PropertyInfo> _dbReference;

        protected EntityBase()
        {
            Id = NullId;
        }

        protected EntityBase(IDistinctableEntity distinctableEntity)
        {
            if (distinctableEntity == null)
            {
                Id = NullId;
                return;
            }

            Id = (long)distinctableEntity.GetPkValue();
        }

        public virtual IRepository<TIEntity> Repository
        {
            get => throw new NotSupportedException(
                "You have to give a repository in order to use Save, Delete or RefreshFromDb.");
            set => throw new NotSupportedException(
                "You have to give a repository in order to use Save, Delete or RefreshFromDb.");
        }

        protected Notifier Notifier { get; } = new Notifier();

        /// <summary>
        ///     Get unique object identifier
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [PrimaryKey(KeyScheme.Identity, FieldName = IdColumnName)]
        public long Id { get; set; }

        /// <summary>
        ///     Get unique object identifier
        /// </summary>
        [JsonProperty(PropertyName = "guid")]
        [Field(FieldName = GuidColumnName, DefaultValue = DefaultValue.RandomGuid, RequireUniqueValue = true,
            AllowsNulls = false, SearchOrder = FieldSearchOrder.Ascending, IsSyncIdentifier = true)]
        public Guid Guid { get; set; }

        /// <summary>
        /// Get primary key column name
        /// </summary>
        /// <returns></returns>
        public string GetPkColumnName()
        {
            return IdColumnName;
        }

        /// <summary>
        /// Get primary key value
        /// </summary>
        /// <returns>Entity primary key value</returns>
        public object GetPkValue()
        {
            return Id;
        }

        public List<PropertyInfo> DbField
        {
            get
            {
                if (_dbField == null) SetDbField();
                return _dbField;
            }
        }

        public List<PropertyInfo> DbReference
        {
            get
            {
                if (_dbReference == null) SetDbField();
                return _dbReference;
            }
        }

        public bool Equals(EntityBase<TIEntity> other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        /// <summary>
        ///     Add new observer
        /// </summary>
        /// <param name="entity">Observer instance to add</param>
        public void Subscribe(IEntityObserver entity)
        {
            Notifier.Subscribe(entity);
        }

        /// <summary>
        ///     Remove specified observer
        /// </summary>
        /// <param name="entity">Observer instance to remove</param>
        public void Unsubscribe(IEntityObserver entity)
        {
            Notifier.Unsubscribe(entity);
        }

        /// <summary>
        ///     Remove all observer
        /// </summary>
        public void UnsubscribeAll()
        {
            Notifier.UnsubscribeAll();
        }

        /// <summary>
        ///     Save current entity in right repository
        /// </summary>
        public virtual void Save()
        {
            Repository.Save(this as TIEntity);
            Notifier.Notify(EntityEvent.Saved, this);
        }

        /// <summary>
        ///     Delete current entity in right repository
        /// </summary>
        public virtual void Delete()
        {
            if (Repository == null)
                throw new NotSupportedException("You have to give a repository in order to use Delete");

            Repository.Delete(this as TIEntity);
            Notifier.Notify(EntityEvent.Deleted, this);
        }

        /// <summary>
        ///     Refresh current instance with db data
        /// </summary>
        public virtual void RefreshFromDb()
        {
            if (Repository == null)
                throw new NotSupportedException("You have to give a repository in order to use RefreshFromDb");

            var dbObject = Repository.GetByPk(Id);
            if (dbObject == null)
                throw new NotSupportedException("Can't refresh from db if object doesn't exist.");

            foreach (var property in DbField)
            {
                var dbValue = property.GetValue(dbObject, null);
                property.SetValue(this, dbValue, null);
            }

            InitializeReferenceCollection();
        }

        protected virtual void InitializeReferenceCollection()
        {
        }

        protected void SetDbField()
        {
            var properties = GetType().GetProperties();

            _dbField = GetDbFields(properties);
            _dbReference = GetReferenceFields(properties);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EntityBase<TIEntity>);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static List<PropertyInfo> GetDbFields<T>()
        {
            var properties = typeof(T).GetProperties();
            return GetDbFields(properties);
        }

        private static List<PropertyInfo> GetDbFields(PropertyInfo[] properties)
        {
            return properties.Where(property => Attribute.IsDefined(property, typeof(FieldAttribute))).ToList();
        }

        public static List<PropertyInfo> GetReferenceFields<T>()
        {
            var properties = typeof(T).GetProperties();
            return GetReferenceFields(properties);
        }

        private static List<PropertyInfo> GetReferenceFields(IEnumerable<PropertyInfo> properties)
        {
            return properties.Where(property => Attribute.IsDefined(property, typeof(ReferenceAttribute))).ToList();
        }
    }
}