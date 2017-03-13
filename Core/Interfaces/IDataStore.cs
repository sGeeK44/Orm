using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Orm.Core.Filters;
using Orm.Core.Replication;
using Orm.Core.SqlQueries;

namespace Orm.Core.Interfaces
{
    public interface IDataStore : IFilterFactory, IDisposable
    {
        event EventHandler<EntityTypeAddedArgs> EntityTypeAdded;
        event EventHandler<EntityInsertArgs> BeforeInsert;
        event EventHandler<EntityInsertArgs> AfterInsert;
        event EventHandler<EntityUpdateArgs> BeforeUpdate;
        event EventHandler<EntityUpdateArgs> AfterUpdate;

        string Name { get; }
        EntityInfoCollection Entities { get; }
        ReplicatorCollection Replicators { get; }

        void AddType<T>();
        void AddType<T>(bool ensureCompatibility);
        void AddType(Type entityType);
        void AddType(Type entityType, bool ensureCompatibility);

        void DiscoverTypes(Assembly containingAssembly);

        void CreateStore();
        void DeleteStore();
        bool StoreExists { get; }
        ISqlFactory SqlFactory { get; }
        bool IsGreenHeath { get; }
        void EnsureCompatibility();

        void Insert(object item);
        void Insert(object item, bool insertReferences);

        IEntityInfo GetEntityInfo(string entityName);
        IEntityInfo[] GetEntityInfo();
        
        T Select<T>(object primaryKey) where T : new();
        object Select(Type objectType, object primaryKey);
        IJoinable<TEntity> Select<TEntity>() where TEntity : class;
        IJoinable<TIEntity> Select<TEntity, TIEntity>() where TEntity : TIEntity where TIEntity : class;
        IEnumerable<TIEntity> Execute<TIEntity>(ISelect<TIEntity> select) where TIEntity : class;
        IEnumerable<object> Select(Type objectType, ICondition condition);
        IEnumerable<object> Execute(ISelect select);

        void Update(object item);
        void Update(object item, bool cascadeUpdates, string fieldName);
        void Update(object item, string fieldName);

        void Delete(object item);
        void Delete(string entityName, object primaryKey);
        void Delete(string entityName, string fieldName, object matchValue);
        void Delete<T>(object primaryKey) where T : new();
        void Delete<T>() where T : new();
        void Delete<T>(string fieldName, object matchValue) where T : new();
        
        int Count<T>();
        int Count(string entityName);
        int Count<T>(ICondition condition);

        bool Contains(object item);

        void FillReferences(object instance);

        void BeginTransaction(IsolationLevel isolationLevel);
        void BeginTransaction();
        void Commit();
        void Rollback();

        void Drop(string entityName);

        void CloseConnections();
    }
}
