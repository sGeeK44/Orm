using System.Collections.Generic;
using NUnit.Framework;
using Orm.Core.Interfaces;

namespace Orm.SqlCe.UnitTests.SqlAssertion
{
    public class TableFormat
    {
        public static void AssertFormat(ISqlBasedStore store, IEnumerable<ColumnFormat> columns)
        {
            var sql = string.Format("SELECT COLUMN_NAME, ORDINAL_POSITION, IS_NULLABLE, DATA_TYPE FROM information_schema.columns WHERE TABLE_NAME = '{0}'", "LateAddItem");

            using (var reader = store.ExecuteReader(sql))
            {
                foreach (var columnFormat in columns)
                {
                    columnFormat.AssertFormat(reader);
                }

                // Assertion no more column
                Assert.IsFalse(reader.Read());
            }
        }
    }
}