using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Orm.Core.Attributes;
using Orm.Core.Constants;
using Orm.Core.Interfaces;

namespace Orm.Core
{
    public class SqlIndexInfo
    {
        public string IndexName { get; set; }
        public string TableName { get; set; }
        public string[] Fields { get; set; }
        public FieldSearchOrder SearchOrder { get; set; }
        public bool IsUnique { get; set; }

        public bool IsComposite
        {
            get { return Fields.Length > 1; }
        }
    }

    public static class Extensions
    {
        private const string FullPattern = @"(?i)CREATE *(UNIQUE)? *INDEX *([^ ]*) *ON *([^( ]*) *\(([^)]*)\) *(ASC|DESC)?";

        public static SqlIndexInfo ParseToIndexInfo(this string sql)
        {
            var regex = new Regex(FullPattern);

            var parsed = regex.Match(sql);
            if (!parsed.Success)
                throw new ArgumentException("String is not valid CREATE INDEX SQL");

            var info = new SqlIndexInfo();
            info.IsUnique = !string.IsNullOrEmpty(parsed.Groups[1].ToString());
            info.IndexName = parsed.Groups[2].ToString().Trim(' ', '[', ']');
            info.TableName = parsed.Groups[3].ToString().Trim(' ', '[', ']');
            info.Fields =
                parsed.Groups[4].ToString()
                      .Split(',')
                      .Where(_ => !string.IsNullOrEmpty(_))
                      .Select(_ => _.Replace("[", string.Empty).Replace("]", string.Empty).Trim())
                      .ToArray();
            if (parsed.Groups[5].Success)
                info.SearchOrder = parsed.Groups[5].ToString() == "ASC"
                    ? FieldSearchOrder.Ascending
                    : FieldSearchOrder.Descending;
            else
                info.SearchOrder = FieldSearchOrder.Ascending;

            return info;
        }

        public static T[] ConvertAll<T>(this Array input)
        {
            var output = new T[input.Length];

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = (T)input.GetValue(i);
            }

            return output;
        }

        public static Array ConvertAll(this List<object> input, Type targetType)
        {
            var output = Array.CreateInstance(targetType, input.Count);
            
            for (int i = 0; i < output.Length; i++)
            {
                output.SetValue(Convert.ChangeType(input[i], targetType, null), i);
            }

            return output;
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Type ToManagedType(this DbType type)
        {
            return type.ToManagedType(false);
        }

        public static Type ToManagedType(this DbType type, bool isNullable)
        {
            switch (type)
            {
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                case DbType.String:
                case DbType.StringFixedLength:
                    return typeof(string);
                case DbType.Boolean:
                    return isNullable ? typeof(bool?) : typeof(bool);
                case DbType.Int16:
                    return isNullable ? typeof(short?) : typeof(short);
                case DbType.UInt16:
                    return isNullable ? typeof(ushort?) : typeof(ushort);
                case DbType.Int32:
                    return isNullable ? typeof(int?) : typeof(int);
                case DbType.UInt32:
                    return isNullable ? typeof(uint?) : typeof(uint);
                case DbType.Time:
                    return isNullable ? typeof(TimeSpan?) : typeof(TimeSpan);
                case DbType.DateTime:
                    return isNullable ? typeof(DateTime?) : typeof(DateTime);
                case DbType.Decimal:
                    return isNullable ? typeof(decimal?) : typeof(decimal);
                case DbType.Single:
                    return isNullable ? typeof(float?) : typeof(float);
                case DbType.Double:
                    return isNullable ? typeof(double?) : typeof(double);
                case DbType.Int64:
                    return isNullable ? typeof(long?) : typeof(long);
                case DbType.UInt64:
                    return isNullable ? typeof(ulong?) : typeof(ulong);
                case DbType.Byte:
                    return isNullable ? typeof(byte?) : typeof(byte);
                case DbType.Guid:
                    return isNullable ? typeof(Guid?) : typeof(Guid);
                case DbType.Binary:
                    return typeof(byte[]);
                default:
                    if (Debugger.IsAttached) Debugger.Break();
                    throw new NotSupportedException();
            }
        }

        public static DbType ParseToDbType(this string dbTypeName)
        {
            return ParseToDbType(dbTypeName, false);
        }

        // SQLite uses 'integer' for 64-bit, SQL COmpact uses it for 32-bit
        public static DbType ParseToDbType(this string dbTypeName, bool integerIs64bit)
        {
            var test = dbTypeName.ToLower();

            switch (test)
            {
                case "time":
                    return DbType.Time;
                case "datetime":
                    return DbType.DateTime;
                case "bigint":
                case "rowversion":
                    return DbType.Int64;
                case "id":
                case "int":
                case "integer":
                    if (integerIs64bit)
                    {
                        return DbType.Int64;
                    }
                    return DbType.Int32;
                case "smallint":
                    return DbType.Int16;
                case "string":
                case "ntext":
                case "nvarchar":
                case "varchar":
                case "text":
                    return DbType.String;
                case "nchar":
                    return DbType.StringFixedLength;
                case "bit":
                case "bool":
                case "boolean":
                    return DbType.Boolean;
                case "tinyint":
                    return DbType.Byte;
                case "numeric":
                case "money":
                case "decimal":
                    return DbType.Decimal;
                case "real":
                case "single":
                    return DbType.Single;
                case "float":
                case "double":
                    return DbType.Double;
                case "uniqueidentifier":
                    return DbType.Guid;
                case "image":
                case "binary":
                case "varbinary":
                case "blob":
                    return DbType.Binary;
                default:
                    // if case it has a length suffix
                    if (test.StartsWith("nvarchar") || test.StartsWith("nchar"))
                    {
                        return DbType.StringFixedLength;
                    }

                    throw new NotSupportedException(
                        string.Format("Unable to determine convert string '{0}' to DbType", dbTypeName));
            }
        }

        public static string ToSqlTypeString(this DbType type)
        {
            switch (type)
            {
                case DbType.DateTime:
                    return "datetime";
                case DbType.Time:
                    return "time";
                case DbType.Int64:
                case DbType.UInt64:
                    return "bigint";
                case DbType.Int32:
                case DbType.UInt32:
                    return "integer";
                case DbType.Int16:
                case DbType.UInt16:
                    return "smallint";
                case DbType.String:
                    return "nvarchar";
                case DbType.StringFixedLength:
                    return "nchar";
                case DbType.Boolean:
                    return "bit";
                case DbType.Object:
                    return "image";
                case DbType.Byte:
                    return "tinyint";
                case DbType.Decimal:
                    return "numeric";
                case DbType.Single:
                    return "real";
                case DbType.Double:
                    return "float";
                case DbType.Guid:
                    return "uniqueidentifier";
                case DbType.Binary:
                    return "image";
                default:
                    throw new NotSupportedException(
                        string.Format("Unable to determine convert DbType '{0}' to string", type.ToString()));
            }
        }

        public static bool UnderlyingTypeIs<T>(this Type checkType)
        {
            if ((checkType.IsGenericType) && (checkType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))))
            {
                return Nullable.GetUnderlyingType(checkType).Equals(typeof(T));
            }
            else
            {
                return checkType.Equals(typeof(T));
            }
        }

        public static void BulkInsert(this IDataStore store, IEnumerable<object> items)
        {
            store.BulkInsert(items, false);
        }

        public static void BulkInsert(this IDataStore store, IEnumerable<object> items, bool insertReferences)
        {
            foreach (var i in items)
            {
                store.Insert(i, insertReferences);
            }
        }

        public static void CreateOrUpdateStore(this IDataStore store)
        {
            if(store.StoreExists)
            {
                store.EnsureCompatibility();
            }
            else
            {
                store.CreateStore();
            }
        }

        public static string GenerateHash(this ReferenceAttribute r)
        {
            var hash = string.Format("{0}{1}{2}", r.PropertyInfo.Name, r.ReferenceEntityType.Name, r.ForeignReferenceField);
            return hash;
        }
    }
}
