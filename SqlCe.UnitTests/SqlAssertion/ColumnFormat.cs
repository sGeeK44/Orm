using System.Data;
using NUnit.Framework;

namespace Orm.SqlCe.UnitTests.SqlAssertion
{
    public class ColumnFormat
    {
        private readonly string _columnName;
        private readonly int _ordinal;
        private readonly bool _isNullable;
        private readonly string _dbType;

        public ColumnFormat(string columnName, int ordinal, bool isNullable, string dbType)
        {
            _columnName = columnName;
            _ordinal = ordinal;
            _isNullable = isNullable;
            _dbType = dbType;
        }

        public void AssertFormat(IDataReader reader)
        {
            var values = new object[reader.FieldCount];
            reader.Read();
            reader.GetValues(values);
            Assert.AreEqual(_columnName, values[0]);
            Assert.AreEqual(_ordinal, values[1]);
            Assert.AreEqual(_isNullable ? "YES" : "NO", values[2]);
            Assert.AreEqual(_dbType, values[3]);
        }
    }
}