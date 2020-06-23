using System;
using System.Text;

namespace SmartWay.Orm.Entity.Fields
{
    public abstract class FieldProperties
    {
        /// <summary>
        ///     Get type of field property
        /// </summary>
        public Type PropertyType { get; set; }

        /// <summary>
        ///     Add default value for null value if specified.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        ///     Add not Null constraint on field in database if value equal false.
        ///     Default value is true.
        /// </summary>
        public bool AllowsNulls { get; set; }

        /// <summary>
        ///     Indicate if property can be setted or managed by sgbd
        /// </summary>
        public virtual bool IsSettable => true;

        /// <summary>
        ///     Return string that repesent sql type definition
        /// </summary>
        /// <returns>Sql type definition</returns>
        public string GetDefinition()
        {
            return GetDataTypeDefinition();
        }

        public abstract string GetDataTypeDefinition();

        public virtual void GetFieldCreationAttributes(StringBuilder definition)
        {
        }

        public string GetFieldCreationAttributes()
        {
            var sb = new StringBuilder();

            GetFieldCreationAttributes(sb);

            if (DefaultValue != null) sb.AppendFormat(" DEFAULT {0}", GetDefaultValueFieldCreation());

            if (!AllowsNulls) sb.Append(" NOT NULL");

            return sb.ToString();
        }

        public abstract string PrimaryKeyConstraint(string constraintName, string fieldName);

        protected virtual string GetDefaultValueFieldCreation()
        {
            return $"{DefaultValue}";
        }

        public virtual string GetIdentity()
        {
            throw new DefinitionException($"Data Type '{GetType()}' cannot be marked as an Identity field");
        }

        public virtual object Convert(object value)
        {
            if (value == DBNull.Value)
                return null;

            if (!PropertyType.IsNullable())
                return ConvertValue(PropertyType, value);

            var args = PropertyType.GetGenericArguments();
            if (args.Length != 1)
                throw new NotSupportedException(
                    $"Converter doesn't support this type of nullable. Type:{PropertyType}");

            if (value == null)
                return null;

            return ConvertValue(args[0], value);
        }

        protected virtual object ConvertValue(Type propType, object value)
        {
            if (propType.IsEnum)
                return Enum.ToObject(propType, value);
            if (value is IConvertible)
                return System.Convert.ChangeType(value, propType, null);

            return value;
        }

        public virtual object ToSqlValue(object instanceValue, out bool needToUpdateInstance)
        {
            needToUpdateInstance = false;
            if (instanceValue == null)
                instanceValue = DBNull.Value;

            if (instanceValue == DBNull.Value && DefaultValue != null)
            {
                instanceValue = DefaultValue;
                needToUpdateInstance = true;
            }

            return instanceValue;
        }
    }
}