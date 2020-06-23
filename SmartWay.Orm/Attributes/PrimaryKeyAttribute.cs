﻿using SmartWay.Orm.Entity.Constraints;

namespace SmartWay.Orm.Attributes
{
    public class PrimaryKeyAttribute : FieldAttribute
    {
        public PrimaryKeyAttribute()
            : this(KeyScheme.None)
        {
        }

        public PrimaryKeyAttribute(KeyScheme keyScheme)
        {
            KeyScheme = keyScheme;
            IsPrimaryKey = true;
            AllowsNulls = false;
        }

        public KeyScheme KeyScheme { get; }
    }
}