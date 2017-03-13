using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Orm.Core.Entity;
using Orm.Core.SqlQueries;

// ReSharper disable UseStringInterpolation
// ReSharper disable ConvertPropertyToExpressionBody

namespace Orm.Core.Filters
{
    /// <summary>
    /// Encapsulate behaviour to manage object value part filter
    /// </summary>
    public class ObjectValue : IFilter
    {
        private const string Null = "NULL";
        private readonly ISqlFactory _sqlFactory;
        private readonly object _value;

        public ObjectValue(ISqlFactory sqlFactory, object value)
        {
            _sqlFactory = sqlFactory;
            _value = value;
        }

        public bool IsNull { get { return _value == null || _value == DBNull.Value; } }

        public string ToStatement(ref List<IDataParameter> @params)
        {
            if (IsNull)
                return Null;

            var asCollection = _value as ICollection;
            if (asCollection == null)
                return FormatParam(_value, @params);

            string result = null;
            foreach (var param in asCollection)
            {
                if (result == null)
                    result = FormatParam(param, @params);
                else 
                    result += ", " + FormatParam(param, @params);
            }
            return result;
        }

        private string FormatParam(object paramToAdd, ICollection<IDataParameter> @params)
        {
            var paramName = string.Format("{0}p{1}", _sqlFactory.ParameterPrefix, @params.Count);
            var param = _sqlFactory.CreateParameter();
            param.ParameterName = paramName;
            param.Value = GetValue(paramToAdd);
            @params.Add(param);
            return paramName;
        }

        private static object GetValue(object paramToAdd)
        {
            var customField = paramToAdd as ICustomSqlField;
            return customField != null ? customField.ToSqlValue() : paramToAdd;
        }
    }
}