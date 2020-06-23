using NUnit.Framework;
using SmartWay.Orm.Sql;
using SmartWay.Orm.Testkit;
using SmartWay.Orm.Testkit.Entities;

namespace SmartWay.Orm.Sqlite.UnitTests.Entity
{
    [TestFixture]
    public class PrimaryKeyTest : DatastoreForTest
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        public override void CleanUp()
        {
            base.CleanUp();
        }

        protected override ISqlDataStore CreateStore(string datasource)
        {
            return SqliteFactory.CreateStore(datasource);
        }

        protected override void AddTypes()
        {
            DataStore.AddType<Book>();
        }

        [Test]
        public void CreateTable_WithOnePrimaryKeyAttribute_ShouldCreateConstraint()
        {
            var schemaChecker = new SqliteSchemaChecker(DataStore);

            Assert.IsTrue(schemaChecker.IsPrimaryKeyExist("Book"));
        }
    }
}