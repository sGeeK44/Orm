using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Orm.Core.Attributes;
using Orm.Core.Filters;
using Orm.Core.Interfaces;
using Orm.Core.Replication;
using Orm.Core.SqlQueries;

// ReSharper disable UseNullPropagation
// ReSharper disable ConvertPropertyToExpressionBody
// ReSharper disable UseNameofExpression
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace Orm.Core
{
    public abstract class DataStore : DisposableBase, IDataStore
    {
        private readonly Dictionary<Type, ConstructorInfo> _ctorCache = new Dictionary<Type, ConstructorInfo>();
        private FilterFactory _filterFactory;

        public event EventHandler<EntityTypeAddedArgs> EntityTypeAdded;

        public ReplicatorCollection Replicators { get; private set; }

        public abstract void CloseConnections();
        public abstract IJoinable<TEntity> Select<TEntity>() where TEntity : class;
        public abstract IJoinable<TIEntity> Select<TEntity, TIEntity>() where TEntity : TIEntity where TIEntity : class;
        public abstract IEnumerable<TIEntity> Execute<TIEntity>(ISelect<TIEntity> select) where TIEntity : class;
        public abstract IEnumerable<object> Select(Type objectType, ICondition condition);
        public abstract IEnumerable<object> Execute(ISelect select);

        // TODO: maybe move these to another object since they're more "admin" related?
        public abstract void CreateStore();
        public abstract void DeleteStore();
        public abstract bool StoreExists { get; }
        public abstract ISqlFactory SqlFactory { get; }
        public abstract bool IsGreenHeath { get; }
        public abstract void EnsureCompatibility();

        public event EventHandler<EntityInsertArgs> BeforeInsert;
        public event EventHandler<EntityInsertArgs> AfterInsert;

        public abstract void OnInsert(object item, bool insertReferences);
        
        public abstract IEnumerable<object> Select(Type objectType);
        public abstract T Select<T>(object primaryKey) where T : new();
        public abstract object Select(Type objectType, object primaryKey);
        public abstract void Drop(string entityName);

        public event EventHandler<EntityUpdateArgs> BeforeUpdate;
        public event EventHandler<EntityUpdateArgs> AfterUpdate;
        public abstract void OnUpdate(object item, bool cascadeUpdates, string fieldName);

        public event EventHandler<EntityDeleteArgs> BeforeDelete;
        public event EventHandler<EntityDeleteArgs> AfterDelete;
        public abstract void OnDelete(object item);

        /// <summary>
        /// Return <b>true</b> if you want the ORM to retry the operation.  Usefule for server unavailable-type errors
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public virtual bool IsRecoverableError(Exception ex)
        {
            return false;
        }

        public bool TracingEnabled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <remarks>This method does <b>not</b> Fire the Before/AfterDelete events</remarks>
        public abstract void Delete<T>() where T : new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="matchValue"></param>
        /// <remarks>This method does <b>not</b> Fire the Before/AfterDelete events</remarks>
        public abstract void Delete<T>(string fieldName, object matchValue) where T : new();

        public abstract void Delete(string entityName, object primaryKey);
        public abstract void Delete(string entityName, string fieldName, object matchValue);

        public abstract void FillReferences(object instance);

        public abstract int Count(string entityName);
        public abstract int Count<T>(ICondition condition);
        public abstract bool Contains(object item);

        protected DataStore()
        {
            TracingEnabled = false;
            Entities = new EntityInfoCollection();
            Replicators = new ReplicatorCollection(this);
            DbTypeConverter = new DefaultDbTypeConverter();
        }

        private FilterFactory FilterFactory
        {
            get
            {
                if (_filterFactory != null)
                    return _filterFactory;
                return _filterFactory = new FilterFactory(SqlFactory, Entities);
            }
        }

        /// <summary>
        /// Returns the number of instances of the given type in the DataStore
        /// </summary>
        /// <typeparam name="T">Entity type to count</typeparam>
        /// <returns>The number of instances in the store</returns>
        public int Count<T>()
        {
            var t = typeof(T);
            string entityName = Entities.GetNameForType(t);

            return Count(entityName);
        }

        public virtual string Name
        {
            get { return "Unanamed Data Store"; }
        }

        public void Delete(object item)
        {
            var name = Entities.GetNameForType(item.GetType());
            OnBeforeDelete(name, item);
            OnDelete(item);
            OnAfterDelete(name, item);
        }

        /// <summary>
        /// Deletes the specified entity instance from the DataStore
        /// </summary>
        /// <param name="primaryKey"></param>
        /// <remarks>
        /// The instance provided must have a valid primary key value
        /// </remarks>
        public void Delete<T>(object primaryKey)
            where T : new()
        {
            var item = Select<T>(primaryKey);

            if (item != null)
            {
                Delete(item);
            }
        }

        public virtual void OnBeforeDelete(string entityName, object item)
        {
            var handler = BeforeDelete;
            if (handler != null)
            {
                handler(this, new EntityDeleteArgs(entityName, item));
            }
        }

        public virtual void OnAfterDelete(string entityName, object item)
        {
            var handler = AfterDelete;
            if (handler != null)
            {
                handler(this, new EntityDeleteArgs(entityName, item));
            }
        }

        /// <summary>
        /// Updates the backing DataStore with the values in the specified entity instance
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>
        /// The instance provided must have a valid primary key value
        /// </remarks>
        public void Update(object item)
        {
            Update(item, false, null);
        }

        public void Update(object item, string fieldName)
        {
            Update(item, false, fieldName);
        }

        public void Update(object item, bool cascadeUpdates, string fieldName)
        {
            var name = Entities.GetNameForType(item.GetType());
            OnBeforeUpdate(name, item, cascadeUpdates, fieldName);
            OnUpdate(item, cascadeUpdates, fieldName);
            OnAfterUpdate(name, item, cascadeUpdates, fieldName);
        }

        public virtual void OnBeforeUpdate(string entityName, object item, bool cascadeUpdates, string fieldName) 
        {
            var handler = BeforeUpdate;
            if (handler != null)
            {
                handler(this, new EntityUpdateArgs(entityName, item, cascadeUpdates, fieldName));
            }
        }

        public virtual void OnAfterUpdate(string entityName, object item, bool cascadeUpdates, string fieldName)
        {
            var handler = AfterUpdate;
            if (handler != null)
            {
                handler(this, new EntityUpdateArgs(entityName, item, cascadeUpdates, fieldName));
            }
        }

        public void Insert(object item, bool insertReferences)
        {
            Insert(item, insertReferences, false);
        }

        internal void Insert(object item, bool insertReferences, bool recoveryInsert)
        {
            if (item == null) throw new ArgumentNullException("item");
            
            var name = Entities.GetNameForType(item.GetType());

            OnBeforeInsert(name, item, insertReferences);
            OnInsert(item, insertReferences);
            OnAfterInsert(name, item, insertReferences);
        }

        public virtual void OnBeforeInsert(string entityName, object item, bool insertReferences)
        {
            var handler = BeforeInsert;
            if (handler != null)
            {
                handler(this, new EntityInsertArgs(entityName, item, insertReferences));
            }
        }

        public virtual void OnAfterInsert(string entityName, object item, bool insertReferences)
        {
            var handler = AfterInsert;
            if (handler != null)
            {
                handler(this, new EntityInsertArgs(entityName, item, insertReferences));
            }
        }

        public EntityInfoCollection Entities { get; private set; }

        public IEntityInfo GetEntityInfo(string entityName)
        {
            return Entities[entityName];
        }

        public IEntityInfo[] GetEntityInfo()
        {
            return Entities.ToArray();
        }

        public void AddType<T>()
        {
            AddType<T>(true);
        }

        public void AddType<T>(bool ensureCompatibility)
        {
            AddType(typeof(T), ensureCompatibility);
        }

        public void AddType(Type entityType)
        {
            AddType(entityType, true);
        }

        protected void RegisterEntityInfo(IEntityInfo info)
        {
            lock (Entities)
            {
                Entities.Add(info);
            }
        }

        public void AddType(Type entityType, bool ensureCompatibility)
        {
            var map = EntityInfo.Create(Entities, entityType, DbTypeConverter);
            AfterAddEntityType(entityType, ensureCompatibility);
            RaiseEntityTypeAdded(map);
        }

        private void RaiseEntityTypeAdded(IEntityInfo map)
        {
            var handler = EntityTypeAdded;
            if (handler != null)
            {
                var info = Entities[map.EntityAttribute.NameInStore];
                var args = new EntityTypeAddedArgs(info);
                handler(this, args);
            }
        }

        protected IDbTypeConverter DbTypeConverter { get; set; }
        
        protected virtual void AfterAddEntityType(Type entityType, bool ensureCompatibility)
        {
        }

        public void DiscoverTypes(Assembly containingAssembly)
        {
            var entities = from t in containingAssembly.GetTypes()
                           where t.GetCustomAttributes(true).FirstOrDefault(a => a.GetType() == typeof(EntityAttribute)) != null
                           select t;

            foreach (var entity in entities)
            {
                // the interface has already been verified by our LINQ
                AddType(entity, false);
            }
        }

        protected void AddFieldToEntity(EntityInfo entity, FieldAttribute field)
        {
            entity.Fields.Add(field);
        }

        public void Insert(object item)
        {
            // TODO: should this default to true or false?
            // right now it is false since we don't look for duplicate references
            Insert(item, false);
        }

        protected internal ConstructorInfo GetConstructorForType(Type objectType)
        {
            if (_ctorCache.ContainsKey(objectType))
            {
                return _ctorCache[objectType];
            }

            var ctor = objectType.GetConstructor(new Type[] { });
            _ctorCache.Add(objectType, ctor);
            return ctor;
        }

        protected void SetInstanceValue(FieldAttribute field, object instance, object value)
        {
            // use Convert where we can to help ensure conversions (uint->int and the like)
            if (field.PropertyInfo.PropertyType == typeof(int))
            {
                field.PropertyInfo.SetValue(instance, Convert.ToInt32(value), null);
            }
            else if (field.PropertyInfo.PropertyType == typeof(uint))
            {
                field.PropertyInfo.SetValue(instance, Convert.ToUInt32(value), null);
            }
            else if (field.PropertyInfo.PropertyType == typeof(short))
            {
                field.PropertyInfo.SetValue(instance, Convert.ToInt16(value), null);
            }
            else if (field.PropertyInfo.PropertyType == typeof(ushort))
            {
                field.PropertyInfo.SetValue(instance, Convert.ToUInt16(value), null);
            }
            else if (field.PropertyInfo.PropertyType == typeof(long))
            {
                field.PropertyInfo.SetValue(instance, Convert.ToInt64(value), null);
            }
            else if (field.PropertyInfo.PropertyType == typeof(ulong))
            {
                field.PropertyInfo.SetValue(instance, Convert.ToUInt64(value), null);
            }
            else if (field.PropertyInfo.PropertyType == typeof(float))
            {
                field.PropertyInfo.SetValue(instance, Convert.ToSingle(value), null);
            }
            else if (field.PropertyInfo.PropertyType == typeof(double))
            {
                field.PropertyInfo.SetValue(instance, Convert.ToDouble(value), null);
            }
            else
            {
                field.PropertyInfo.SetValue(instance, value, null);
            }
        }

        protected object GetInstanceValue(FieldAttribute field, object instance)
        {
            var value = field.PropertyInfo.GetValue(instance, null);

            if (value is TimeSpan)
            {
                return ((TimeSpan)value).Ticks;
            }

            if (value == null) return DBNull.Value;

            return value;
        }

        public virtual void BeginTransaction()
        {
            BeginTransaction(IsolationLevel.Unspecified);
        }

        public virtual void BeginTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException("Transactions are not supported by this provider");
        }

        public virtual void Commit()
        {
            throw new NotSupportedException("Transactions are not supported by this provider");
        }

        public virtual void Rollback()
        {
            throw new NotSupportedException("Transactions are not supported by this provider");
        }

        /// <summary>
        /// Create new condition
        /// </summary>
        /// <typeparam name="TEntity">Type of entity associated to column name</typeparam>
        /// <param name="columnName">Column Name involve</param>
        /// <param name="value">Value to compare to column name</param>
        /// <param name="filterOperator">Operator to apply</param>
        /// <returns>New build condition</returns>
        public ICondition Condition<TEntity>(string columnName, object value, FilterOperator filterOperator)
        {
            return FilterFactory.Condition<TEntity>(columnName, value, filterOperator);
        }

        /// <summary>
        /// Create new condition
        /// </summary>
        /// <param name="entityType">Type of entity associated to column name</param>
        /// <param name="columnName">Column Name involve</param>
        /// <param name="value">Value to compare to column name</param>
        /// <param name="filterOperator">Operator to apply</param>
        /// <returns>New build condition</returns>
        public ICondition Condition(Type entityType, string columnName, object value, FilterOperator filterOperator)
        {
            return FilterFactory.Condition(entityType, columnName, value, filterOperator);
        }

        /// <summary>
        /// Create new condition
        /// </summary>
        /// <typeparam name="TEntity1">Type of entity associated to column name 1</typeparam>
        /// <typeparam name="TEntity2">Type of entity associated to column name 2</typeparam>
        /// <param name="columnName1">First column Name involve</param>
        /// <param name="columnName2">Second column Name to compare</param>
        /// <param name="filterOperator">Operator to apply</param>
        /// <returns>New build condition</returns>
        public ICondition Condition<TEntity1, TEntity2>(string columnName1, string columnName2, FilterOperator filterOperator)
        {
            return FilterFactory.Condition<TEntity1, TEntity2>(columnName1, columnName2, filterOperator);
        }

        /// <summary>
        /// Create new condition
        /// </summary>
        /// <param name="entityType1">Type of entity associated to column name 1</param>
        /// <param name="entityType2">Type of entity associated to column name 2</param>
        /// <param name="columnName1">First column Name involve</param>
        /// <param name="columnName2">Second column Name to compare</param>
        /// <param name="filterOperator">Operator to apply</param>
        /// <returns>New build condition</returns>
        public ICondition Condition(Type entityType1, Type entityType2, string columnName1, string columnName2, FilterOperator filterOperator)
        {
            return FilterFactory.Condition(entityType1, entityType2, columnName1, columnName2, filterOperator);
        }

        /// <summary>
        /// Create new condition
        /// </summary>
        /// <typeparam name="TEntity">Type of entity on wich condition should be applied</typeparam>
        /// <param name="columnName">Column Name involve</param>
        /// <param name="scalarValue">Scalar operation value</param>
        /// <param name="filterOperator">Operator to apply</param>
        /// <returns>New build condition</returns>
        public ICondition Condition<TEntity>(string columnName, IFilter scalarValue, FilterOperator filterOperator)
        {
            return FilterFactory.Condition<TEntity>(columnName, scalarValue, filterOperator);
        }

        /// <summary>
        /// Create new condition
        /// </summary>
        /// <param name="leftPart">First paert involve in filter</param>
        /// <param name="value">Value to compare to column name</param>
        /// <param name="filterOperator">Operator to apply</param>
        /// <returns>New build condition</returns>
        public ICondition Condition(IFilter leftPart, object value, FilterOperator filterOperator)
        {
            return FilterFactory.Condition(leftPart, value, filterOperator);
        }

        /// <summary>
        /// Create a new ScalarValue wich will add specified date on specified column
        /// </summary>
        /// <typeparam name="TEntity">Type of entity on wich scalar operation should be applied</typeparam>
        /// <param name="date">Date to add on each column value</param>
        /// <param name="columnName">Column Name involve</param>
        /// <returns>New scalar operation</returns>
        public IFilter AddDay<TEntity>(DateTime date, string columnName)
        {
            return FilterFactory.AddDay<TEntity>(date, columnName);
        }

        /// <summary>
        /// Create a new ScalarValue wich will apply scalar operator on specified column
        /// </summary>
        /// <typeparam name="TEntity1">Type of entity associated to column name 1</typeparam>
        /// <typeparam name="TEntity2">Type of entity associated to column name 2</typeparam>
        /// <param name="columnName1">First column Name involve</param>
        /// <param name="columnName2">Second column Name involve</param>
        /// <param name="scalarOperator">Operator to apply between two field</param>
        /// <returns>New scalar operation</returns>
        public IFilter Scalar<TEntity1, TEntity2>(string columnName1, string columnName2, ScalarOperator scalarOperator)
        {
            return FilterFactory.Scalar<TEntity1, TEntity2>(columnName1, columnName2, scalarOperator);
        }

        /// <summary>
        /// Create a new ScalarValue wich will apply scalar operator on specified column
        /// </summary>
        /// <param name="entityType1">Type of entity associated to column name 1</param>
        /// <param name="entityType2">Type of entity associated to column name 2</param>
        /// <param name="columnName1">First column Name involve</param>
        /// <param name="columnName2">Second column Name involve</param>
        /// <param name="scalarOperator">Operator to apply between two field</param>
        /// <returns>New scalar operation</returns>
        public IFilter Scalar(Type entityType1, Type entityType2, string columnName1, string columnName2, ScalarOperator scalarOperator)
        {
            return FilterFactory.Scalar(entityType1, entityType2, columnName1, columnName2, scalarOperator);
        }

        /// <summary>
        /// Create a new column wich will be used on a request
        /// </summary>
        /// <typeparam name="TEntity">Type of entity owner</typeparam>
        /// <param name="columnName">Column Name</param>
        /// <returns>New column value statement</returns>
        public ColumnValue GetColumn<TEntity>(string columnName)
        {
            return FilterFactory.GetColumn<TEntity>(columnName);
        }
    }
}
