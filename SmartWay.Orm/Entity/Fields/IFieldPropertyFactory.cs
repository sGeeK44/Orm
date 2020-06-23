using System;
using SmartWay.Orm.Attributes;

namespace SmartWay.Orm.Entity.Fields
{
    public interface IFieldPropertyFactory
    {
        /// <summary>
        ///     Create field property for specified property type with field attribute specification
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fieldAttribute"></param>
        /// <returns></returns>
        FieldProperties Create(Type type, FieldAttribute fieldAttribute);
    }
}