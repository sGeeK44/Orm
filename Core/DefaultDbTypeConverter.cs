using System;
using System.Data;
using System.Reflection;

namespace Orm.Core
{
    /// <summary>
    /// Implement default behaviour used by datastore
    /// </summary>
    public class DefaultDbTypeConverter : IDbTypeConverter
    {
        /// <summary>
        /// Convert specified .Net type to sql db type
        /// </summary>
        /// <param name="type">.Net type to converter</param>
        /// <returns>DbType equivalent</returns>
        public DbType ToDbType(Type type)
        {
            var trueType = GetTrueType(type);

            if (trueType == typeof(String))
                return DbType.String;
            if (trueType == typeof(Boolean))
                return DbType.Boolean;
            if (trueType == typeof(Int16))
                return DbType.Int16;
            if (trueType == typeof(UInt16))
                return DbType.UInt16;
            if (trueType == typeof(Int32))
                return DbType.Int32;
            if (trueType == typeof(UInt32))
                return DbType.UInt32;
            if (trueType == typeof(DateTime))
                return DbType.DateTime;
            if (trueType == typeof(TimeSpan))
                return DbType.Time;
            if (trueType == typeof(Single))
                return DbType.Single;
            if (trueType == typeof(Decimal))
                return DbType.Decimal;
            if (trueType == typeof(Double))
                return DbType.Double;
            if (trueType == typeof(Int64))
                return DbType.Int64;
            if (trueType == typeof(UInt64))
                return DbType.UInt64;
            if (trueType == typeof(Byte))
                return DbType.Byte;
            if (trueType == typeof(Char))
                return DbType.Byte;
            if (trueType == typeof(Guid))
                return DbType.Guid;
            if (trueType == typeof(Byte[]))
                return DbType.Binary;

            // everything else is an "object" and requires a custom serializer/deserializer
            return  DbType.Object;
        }

        private Type GetTrueType(Type type)
        {
            var result = type.IsNullable()
                       ? type.GetGenericArguments()[0]
                       : type;

            return result.IsEnum ? GetEnumUnderlyingType(result) : result;
        }

        public Type GetEnumUnderlyingType(Type type)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields == null || fields.Length != 1)
            {
                throw new ArgumentException(string.Format("Unable to extract underlying type of enum."), "type");
            }
            return fields[0].FieldType;
        }
    }
}