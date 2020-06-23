using System;
using SmartWay.Orm.Entity.Constraints;
using SmartWay.Orm.Interfaces;

namespace SmartWay.Orm.Sql
{
    public interface IDbAccessStrategy
    {
        /// <summary>
        ///     Get entity in datastore for specified primary key
        /// </summary>
        /// <param name="objectType">Type of object searched</param>
        /// <param name="primaryKey">Primary key field of entity searched</param>
        /// <param name="value">Primary key value</param>
        /// <returns></returns>
        object SelectByPrimayKey(Type objectType, PrimaryKey primaryKey, object value);

        /// <summary>
        ///     Inserts the provided entity instance into the underlying data store.
        /// </summary>
        void Insert(object item);

        /// <summary>
        ///     Update the provided entity instance into the underlying data store.
        /// </summary>
        void Update(object item);

        /// <summary>
        ///     Delete the provided entity instance into the underlying data store.
        /// </summary>
        void Delete(object item);

        /// <summary>
        ///     Get all existing table in specified datastore
        /// </summary>
        /// <returns>All table name</returns>
        string[] GetTableNames();

        /// <summary>
        ///     Ensure table is well setted for specified entity
        /// </summary>
        /// <param name="entity">Entity involve</param>
        void ValidateTable(IEntityInfo entity);

        /// <summary>
        ///     Build create sql order to create new table for specified entity
        /// </summary>
        /// <param name="entity">Entity to store</param>
        /// <returns>Sql order</returns>
        string CreateTable(IEntityInfo entity);
    }
}