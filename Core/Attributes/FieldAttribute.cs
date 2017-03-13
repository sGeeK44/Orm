using System;
using System.Data;
using System.Reflection;
using Orm.Core.Constants;
using Orm.Core.Interfaces;

namespace Orm.Core.Attributes
{
    public class FieldAttribute : EntityFieldAttribute, ICloneable
    {
        private DbType _type;
        
        public FieldAttribute()
        {
            // set up defaults
            AllowsNulls = true;
            IsPrimaryKey = false;
            SearchOrder = FieldSearchOrder.NotSearchable;
            RequireUniqueValue = false;
            IsRowVersion = false;
        }

        public FieldAttribute(string name, DbType type, bool isPrimaryKey)
            : this()
        {
            FieldName = name;
            DataType = type;
            IsPrimaryKey = isPrimaryKey;
        }

        public object Clone()
        {
            return new FieldAttribute(FieldName, DataType, IsPrimaryKey)
            {
                Length = Length,
                Precision = Precision,
                Scale = Scale,
                AllowsNulls = AllowsNulls,
                RequireUniqueValue = RequireUniqueValue,
                SearchOrder = SearchOrder,
                DefaultType = DefaultType,
                IsRowVersion = IsRowVersion,
                PropertyInfo = PropertyInfo, // this might not be valid
                DataTypeIsValid = DataTypeIsValid
            };
        }

        public string FieldName { get; set; }
        public int Length { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool AllowsNulls { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool RequireUniqueValue { get; set; }
        public FieldSearchOrder SearchOrder { get; set; }

        public DefaultType DefaultType { get; set; }
        public object DefaultValue { get; set; }
        
        private bool? _isTimeSpan;

        public bool IsTimespan
        {
            get 
            {
                if(!_isTimeSpan.HasValue)
                {
                    _isTimeSpan = PropertyInfo.PropertyType.UnderlyingTypeIs<TimeSpan>();
                }
                return _isTimeSpan.Value;
            }
        }

        /// <summary>
        /// rowversion or timestamp time for Sql Server
        /// </summary>
        public bool IsRowVersion { get; set; }
 
        public PropertyInfo PropertyInfo { get; internal set; }
        internal bool DataTypeIsValid { get; private set; }

        public DbType DataType 
        {
            get { return _type; }
            set
            {
                _type = value;
                DataTypeIsValid = true;
            }
        }

        /// <summary>
        /// Get Entity linked
        /// </summary>
        public IEntityInfo Entity { get; internal set; }

        /// <summary>
        /// Get fully qualified name (TableName.FieldName)
        /// </summary>
        public string FullFieldName { get; set; }

        /// <summary>
        /// Get fully qualified name valid for column name select (TableNameFieldName)
        /// </summary>
        public string AliasFieldName { get; set; }

        public override string ToString()
        {
            return FieldName;
        }
    }
}
