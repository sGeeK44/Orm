using NUnit.Framework;
using Orm.Core;
using Orm.Core.Constants;

namespace Orm.SqlCe.UnitTests
{
    [TestFixture]
    public class ExtensionTests
    {
        public TestContext TestContext { get; set; }

        [Test]
        public void ParseToIndexInfoTest()
        {
            // CREATE INDEX idx_Foo ON TableX (ColumnA, ColumnB, ColumnC) ASC
            // CREATE INDEX idx_Foo ON TableX (ColumnA, ColumnB) DESC
            // CREATE INDEX idx_Foo ON TableX (ColumnA) DESC
            // CREATE INDEX idx_Foo ON TableX (ColumnA)

            var sql = "CREATE INDEX  idx_Foo ON TableX(ColumnA, ColumnB, ColumnC)  ASC";
            var info = sql.ParseToIndexInfo();

            Assert.AreEqual("idx_Foo", info.IndexName);
            Assert.AreEqual(false, info.IsUnique);
            Assert.AreEqual("TableX", info.TableName);
            Assert.AreEqual(3, info.Fields.Length);
            Assert.IsTrue(info.IsComposite);
            Assert.AreEqual(FieldSearchOrder.Ascending, info.SearchOrder);

            sql = "CREATE UNIQUE INDEX  idx_Foo ON TableX(ColumnA, ColumnB, ColumnC)  ASC";
            info = sql.ParseToIndexInfo();

            Assert.AreEqual("idx_Foo", info.IndexName);
            Assert.AreEqual(true, info.IsUnique);
            Assert.AreEqual("TableX", info.TableName);
            Assert.AreEqual(3, info.Fields.Length);
            Assert.IsTrue(info.IsComposite);
            Assert.AreEqual(FieldSearchOrder.Ascending, info.SearchOrder);

            sql = "CREATE INDEX idx_Foo ON TableX (ColumnA, ColumnB)  DESC";
            info = sql.ParseToIndexInfo();

            Assert.AreEqual("idx_Foo", info.IndexName);
            Assert.AreEqual("TableX", info.TableName);
            Assert.AreEqual(2, info.Fields.Length);
            Assert.IsTrue(info.IsComposite);
            Assert.AreEqual(FieldSearchOrder.Descending, info.SearchOrder);

            sql = "create index idx_Foo on TableX (ColumnA)  DESC";
            info = sql.ParseToIndexInfo();

            Assert.AreEqual("idx_Foo", info.IndexName);
            Assert.AreEqual("TableX", info.TableName);
            Assert.AreEqual(1, info.Fields.Length);
            Assert.IsFalse(info.IsComposite);
            Assert.AreEqual(FieldSearchOrder.Descending, info.SearchOrder);

            sql = "create index idx_Foo on TableX (ColumnA)";
            info = sql.ParseToIndexInfo();

            Assert.AreEqual("idx_Foo", info.IndexName);
            Assert.AreEqual("TableX", info.TableName);
            Assert.AreEqual(1, info.Fields.Length);
            Assert.IsFalse(info.IsComposite);
            Assert.AreEqual(FieldSearchOrder.Ascending, info.SearchOrder);
        }
    }

}
