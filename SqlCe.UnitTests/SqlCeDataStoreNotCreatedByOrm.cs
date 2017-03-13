using System.Linq;
using NUnit.Framework;
using Orm.SqlCe.UnitTests.Entities;

// ReSharper disable UseStringInterpolation
// ReSharper disable RedundantStringFormatCall

namespace Orm.SqlCe.UnitTests
{
    [TestFixture]
    public class SqlCeDataStoreNotCreatedByOrm
    {
        private SqlCeDataStore _mStore;

        [SetUp]
        public void BuildCleanDb()
        {
            _mStore = new SqlCeDataStore("UserManualCreation.sdf");
            _mStore.TruncateTable("Book");
            _mStore.AddType<Book>();
        }

        [TearDown]
        public void CleanUp()
        {
            _mStore.Dispose();
        }

        [Test]
        public void SimpleCrud_OrdinalnotOrdered_ShouldInsertUpdateInRightColumn()
        {
            var itemA = new Book
            {
                AuthorId = 1,
                Title = "Livre A"
            };

            // INSERT
            _mStore.Insert(itemA);

            // SELECT
            var items = _mStore.Select<Book>().Execute().ToList();
            Assert.AreEqual(1, items.Count);

            var item = items.First();
            Assert.AreEqual(1, item.AuthorId);
            Assert.AreEqual(itemA.Title, item.Title);
        }
    }
}
