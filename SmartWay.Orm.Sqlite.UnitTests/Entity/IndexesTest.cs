using NUnit.Framework;
using SmartWay.Orm.Sql;
using SmartWay.Orm.Testkit;
using SmartWay.Orm.Testkit.Entities;

namespace SmartWay.Orm.Sqlite.UnitTests.Entity
{
    [TestFixture]
    public class IndexesTest : DatastoreForTest
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            SchemaChecker = new SqliteSchemaChecker(DataStore);
        }

        [TearDown]
        public override void CleanUp()
        {
            base.CleanUp();
        }

        private SqliteSchemaChecker SchemaChecker { get; set; }

        protected override void AddTypes()
        {
            DataStore.AddType<IndexedClass>();
        }

        protected override ISqlDataStore CreateStore(string datasource)
        {
            return SqliteFactory.CreateStore(datasource);
        }

        private void AssertIndex(string indexName, int countFieldInvolve)
        {
            var fieldInvolve = SchemaChecker.IsIndexExist(indexName);
            Assert.AreEqual(fieldInvolve, countFieldInvolve);
        }

        [Test]
        public void CreateTable_WithOneIndexAttribute_ShouldCreateIndex()
        {
            DataStore.Insert(new IndexedClass {Unique = "ee", Searchable = "e", SearchableAndUnique = "zz"});

            AssertIndex("ORM_IDX_IndexedClass_MonIndex_ASC", 4);
            AssertIndex("ORM_IDX_IndexedClass_Searchable_ASC", 1);
            AssertIndex("ORM_IDX_IndexedClass_Unique_ASC", 1);
            AssertIndex("ORM_IDX_IndexedClass_SearchableAndUnique_ASC", 1);
        }
    }
}