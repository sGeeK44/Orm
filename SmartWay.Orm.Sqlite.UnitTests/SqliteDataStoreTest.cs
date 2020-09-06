using System;
using System.Data;
using System.Linq;
using NFluent;
using NUnit.Framework;
using SmartWay.Orm.Attributes;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity.Constraints;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Sql;
using SmartWay.Orm.Sql.Schema;
using SmartWay.Orm.Testkit;
using SmartWay.Orm.Testkit.Entities;


// ReSharper disable RedundantStringFormatCall

namespace SmartWay.Orm.Sqlite.UnitTests
{
    [TestFixture]
    public class SqliteDataStoreTest : DatastoreForTest
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

        protected override void AddTypes()
        {
            DataStore.AddType<TestItem>();
            DataStore.AddType<TestTable>();
            DataStore.AddType<Author>();
            DataStore.AddType<Book>();
            DataStore.AddType<BookVersion>();
        }

        protected override ISqlDataStore CreateStore(string datasource)
        {
            return SqliteFactory.CreateStore(datasource);
        }
    }

    public class Insert : SqliteDataStoreTest
    {
        [Entity]
        private class NullableNumeric
        {
            [Field] public decimal? Value { get; set; }
        }

        protected override void AddTypes()
        {
            base.AddTypes();
            DataStore.AddType<NullableNumeric>();
        }

        [Test]
        public void EntityWithAllTypeOfField()
        {
            var itemA = new TestItem("ItemA")
            {
                UUID = new Guid("A383CDBF-4791-4B9F-9DD4-6286108388BE"),
                ITest = 5,
                Address = "xxxx",
                FTest = 3.14F,
                DBTest = 1.4D,
                DETest = 2.678M,
                TestDate = new DateTime(2016, 12, 14, 11, 38, 14, 10),
                EnumField = TestEnum.ValueB
            };

            DataStore.Insert(itemA);

            var count = 0;
            var sql = $"SELECT * FROM TestItem WHERE Id = '{itemA.Id}'";
            using (var reader = DataStore.ExecuteReader(sql))
            {
                while (reader.Read())
                {
                    Assert.AreEqual(itemA.Id, reader.GetInt32(0));
                    Assert.AreEqual(itemA.Name, reader.GetString(1));
                    Assert.AreEqual(itemA.UUID, reader.GetGuid(2));
                    Assert.AreEqual(itemA.ITest, reader.GetInt32(3));
                    Assert.AreEqual(itemA.Address, reader.GetString(4));
                    Assert.AreEqual(itemA.FTest, reader.GetFloat(5));
                    Assert.AreEqual(itemA.DBTest, reader.GetDouble(6));
                    Assert.AreEqual(2.68M, reader.GetDecimal(7));
                    Assert.IsTrue(reader.IsDBNull(8));
                    Assert.AreEqual(itemA.TestDate, reader.GetDateTime(9));
                    Assert.AreEqual((int) itemA.EnumField, reader.GetInt32(13));
                    count++;
                }
            }

            Assert.AreEqual(1, count);
        }

        [Test]
        public void NullNumeric()
        {
            var item = new NullableNumeric();

            DataStore.Insert(item);

            var sql = string.Format("SELECT * FROM NullableNumeric");
            using var reader = DataStore.ExecuteReader(sql);
            while (reader.Read()) Assert.AreEqual(DBNull.Value, reader.GetValue(0));
        }
    }

    public class Update : SqliteDataStoreTest
    {
        [Test]
        public void EntityWithAllTypeOfField()
        {
            DataStore.TruncateTable("TestItem");
            var itemA = new TestItem("ItemA");
            DataStore.Insert(itemA);
            itemA.TestDate = DateTime.Now;

            DataStore.Update(itemA);

            var checkItem = DataStore.Select<TestItem>().GetValues().First();
            Assert.AreEqual(itemA.TestDate.Second, checkItem.TestDate.Second);
        }
    }

    public class CreateOrUpdateStore : SqliteDataStoreTest
    {
        [Test]
        public void LateAddEntity()
        {
            DataStore.AddType<LateAddItem>();

            DataStore.CreateOrUpdateStore();
            DataStore.Insert(new LateAddItem());

            var expected = new TableDefinition("LateAddItem");
            expected.AddColumn("Id", 0, false, "INTEGER");
            expected.AddColumn("Name", 1, true, "TEXT");

            var schemaChecker = new SqliteSchemaChecker(DataStore);
            var current = schemaChecker.GetTableFormat("LateAddItem");

            Assert.AreEqual(expected, current);
        }
    }

    public class BeginTransaction : SqliteDataStoreTest
    {
        [Test]
        public void CommitTransaction()
        {
            var testEntity = new TestItem();
            DataStore.Insert(testEntity);

            DataStore.BeginTransaction();
            DataStore.Delete(testEntity);
            Assert.AreEqual(0, DataStore.Select<TestItem>().Count());

            DataStore.Insert(new TestItem());
            DataStore.Insert(new TestItem());
            Assert.AreEqual(2, DataStore.Select<TestItem>().Count());

            DataStore.Commit();
            Assert.AreEqual(2, DataStore.Select<TestItem>().Count());
        }
    }

    public class EndToEnd : SqliteDataStoreTest
    {
        [Test]
        public void SimpleCrudTest()
        {
            var afterInsert = false;
            var afterUpdate = false;
            var afterDelete = false;

            DataStore.AddType<TestItem>();
            DataStore.CreateOrUpdateStore();

            DataStore.AfterInsert += delegate { afterInsert = true; };
            DataStore.AfterUpdate += delegate { afterUpdate = true; };
            DataStore.AfterDelete += delegate { afterDelete = true; };

            var itemA = new TestItem("ItemA")
            {
                UUID = Guid.NewGuid(),
                ITest = 5,
                FTest = 3.14F,
                DBTest = 1.4D,
                DETest = 2.678M
            };

            var itemB = new TestItem("ItemB");
            var itemC = new TestItem("ItemC");

            // INSERT
            DataStore.Insert(itemA);
            Assert.IsTrue(afterInsert, "AfterInsert never fired");

            DataStore.Insert(itemB);
            DataStore.Insert(itemC);

            // COUNT
            var count = DataStore.Select<TestItem>().Count();
            Assert.AreEqual(3, count);

            // SELECT
            var items = DataStore.Select<TestItem>().GetValues();
            Assert.AreEqual(3, items.Count());

            var condition = DataStore.Condition<TestItem>("Name", itemB.Name, FilterOperator.Equals);
            var item = DataStore.Select<TestItem, TestItem>().Where(condition).GetValues().First();
            Assert.IsTrue(item.Equals(itemB));

            item = DataStore.Select<TestItem>(3);
            Assert.IsTrue(item.Equals(itemC));

            // FETCH

            // UPDATE
            itemC.Name = "NewItem";
            itemC.Address = "Changed Address";
            itemC.BigString = "little string";

            // test rollback
            DataStore.BeginTransaction();
            DataStore.Update(itemC);
            item = DataStore.Select<TestItem>(3);
            Assert.IsTrue(item.Name == itemC.Name);
            DataStore.Rollback();

            item = DataStore.Select<TestItem>(3);
            Assert.IsTrue(item.Name != itemC.Name);

            // test commit
            DataStore.BeginTransaction(IsolationLevel.Unspecified);
            DataStore.Update(itemC);
            DataStore.Commit();

            Assert.IsTrue(afterUpdate, "AfterUpdate never fired");

            condition = DataStore.Condition<TestItem>("Name", "ItemC", FilterOperator.Equals);
            item = DataStore.Select<TestItem, TestItem>().Where(condition).GetValues().FirstOrDefault();
            Assert.IsNull(item);

            condition = DataStore.Condition<TestItem>("Name", itemC.Name, FilterOperator.Equals);
            item = DataStore.Select<TestItem, TestItem>().Where(condition).GetValues().First();
            Assert.IsTrue(item.Equals(itemC));

            // DELETE
            DataStore.Delete(itemA);
            Assert.IsTrue(afterDelete, "AfterDelete never fired");

            condition = DataStore.Condition<TestItem>("Name", itemA.Name, FilterOperator.Equals);
            item = DataStore.Select<TestItem, TestItem>().Where(condition).GetValues().FirstOrDefault();
            Assert.IsNull(item);

            // COUNT
            count = DataStore.Select<TestItem>().Count();
            Assert.AreEqual(2, count);

            // this will create the table in newer versions of ORM
            DataStore.AddType<LateAddItem>();

            var newitems = DataStore.Select<LateAddItem>();
            Assert.IsNotNull(newitems);
        }
    }

    public class Select : SqliteDataStoreTest
    {
        [Test]
        public void ConditionWithNullObjectValue()
        {
            var withExpiryDateFilter = DataStore.Condition<Book>(Book.AuthorIdColName, null, FilterOperator.NotEqual);
            var result = withExpiryDateFilter.ToStatement(null);

            Assert.AreEqual("[Book].[AuthorId] IS NOT NULL", result);
        }

        [Test]
        public void EntityWithEnum()
        {
            var testRow = new TestTable {EnumField = TestEnum.ValueB};
            DataStore.Insert(testRow);

            testRow.EnumField = TestEnum.ValueC;
            DataStore.Update(testRow);

            var testFromDatastore = DataStore.Select<TestTable>().GetValues().First();

            Assert.AreEqual(testRow.EnumField, testFromDatastore.EnumField);
        }

        [Test]
        public void GroupBy()
        {
            var book1 = new Book();
            DataStore.Insert(book1);
            var book1Version1 = new BookVersion {BookId = book1.Id};
            DataStore.Insert(book1Version1);

            var book2 = new Book();
            DataStore.Insert(book2);
            var book2Version1 = new BookVersion {BookId = book2.Id};
            DataStore.Insert(book2Version1);
            var book2Version2 = new BookVersion {BookId = book2.Id};
            DataStore.Insert(book2Version2);

            var bookId = DataStore.GetColumn<BookVersion>(BookVersion.BookIdColName);
            var bookUsage = DataStore.Select<BookVersion>()
                .GroupBy(bookId)
                .OrderBy(bookId)
                .Count(bookId)
                .ToList();

            Assert.AreEqual(2, bookUsage.Count);

            Assert.AreEqual(2, bookUsage[0].ColumnValues.Count);
            Assert.AreEqual(1, bookUsage[0].ColumnValues["count"]);
            Assert.AreEqual(book1.Id, bookUsage[0].ColumnValues[bookId.AliasFiledName]);

            Assert.AreEqual(2, bookUsage[1].ColumnValues.Count);
            Assert.AreEqual(2, bookUsage[1].ColumnValues["count"]);
            Assert.AreEqual(book2.Id, bookUsage[1].ColumnValues[bookId.AliasFiledName]);
        }

        [Test]
        public void JoinManyToOneRelation()
        {
            var author = new Author();
            DataStore.Insert(author);
            var book = new Book {AuthorId = author.Id};
            DataStore.Insert(book);
            var bookVersion1 = new BookVersion {BookId = book.Id};
            var bookVersion2 = new BookVersion {BookId = book.Id};
            DataStore.Insert(bookVersion1);
            DataStore.Insert(bookVersion2);

            var bookVersionList = DataStore.Select<BookVersion>()
                .Join<BookVersion, Book>()
                .Join<Book, Author>()
                .GetValues()
                .ToList();

            Assert.AreEqual(2, bookVersionList.Count);
            Assert.IsNotNull(bookVersionList[0].Book);
            Assert.AreSame(bookVersionList[0].Book, bookVersionList[1].Book);
            Assert.IsNotNull(bookVersionList[0].Book.Author);
            Assert.AreSame(bookVersionList[0].Book.Author, bookVersionList[1].Book.Author);
        }

        [Test]
        public void LeftJoinOneToManyRelation()
        {
            var book1 = new Book();
            DataStore.Insert(book1);
            var book1Version1 = new BookVersion {BookId = book1.Id};
            DataStore.Insert(book1Version1);

            var book2Version1 = new BookVersion();
            DataStore.Insert(book2Version1);


            var bookVersionList = DataStore.Select<BookVersion>()
                .LeftJoin<BookVersion, Book>()
                .GetValues()
                .ToList();

            Assert.AreEqual(2, bookVersionList.Count);
            Assert.AreEqual(book1.Id, bookVersionList[0].BookId);
            Assert.IsNotNull(bookVersionList[0].Book);
            Assert.AreEqual(null, bookVersionList[1].BookId);
            Assert.IsNull(bookVersionList[1].Book);
        }
    }

    [TestFixture]
    public class FilterTest : SqliteDataStoreTest
    {
        [Test]
        public void LambdaExpression()
        {
            var entity = new Book();
            DataStore.Insert(entity);

            var result = DataStore.Select<Book>().Where(_ => _.Id == 1).GetValues().ToList();

            Check.That(result).Contains(entity);
        }
    }

    [TestFixture]
    public class GuidPrimaryKey : DatastoreForTest
    {
        /// <summary>
        ///     Encaspulate common behavior for standard entity
        /// </summary>
        [Entity]
        public class Entity
        {
            public const string IdColumnName = "id";

            /// <summary>
            ///     Get unique object identifier
            /// </summary>
            [PrimaryKey(KeyScheme.GUID, FieldName = IdColumnName, DefaultValue = DefaultValue.RandomGuid)]
            public Guid Id { get; set; }
        }

        protected override void AddTypes()
        {
            DataStore.AddType<Entity>();
        }

        protected override ISqlDataStore CreateStore(string datasource)
        {
            return SqliteFactory.CreateStore(datasource);
        }

        [Test]
        public void InsertEntity()
        {
            var entity = new Entity();

            DataStore.Insert(entity);

            Check.That(entity.Id).IsNotEqualTo(Guid.Empty);
        }
    }
}