using System;
using JetBrains.Annotations;

namespace SmartWay.Orm.Attributes
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Property)]
    public class EntityFieldAttribute : Attribute
    {
    }
}