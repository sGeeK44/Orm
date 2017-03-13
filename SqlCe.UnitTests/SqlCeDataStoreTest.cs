using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using Orm.Core;
using Orm.Core.SqlStore;
using Orm.SqlCe.UnitTests.Entities;
using Orm.SqlCe.UnitTests.SqlAssertion;

// ReSharper disable UseStringInterpolation
// ReSharper disable RedundantStringFormatCall

namespace Orm.SqlCe.UnitTests
{
    [TestFixture]
    public class SqlCeDataStoreTest
    {
        private SqlCeDataStore _store;

        [SetUp]
        public void BuildCleanDb()
        {
            _store = new SqlCeDataStore(TestContext.CurrentContext.Test.Name + ".sdf");

            if (_store.StoreExists)
            {
                _store.DeleteStore();
            }
            _store.CreateStore();

            _store.AddType<TestItem>();
            _store.AddType<TestTable>();
            _store.AddType<Book>();
            _store.AddType<BookVersion>();
        }

        [TearDown]
        public void CleanUp()
        {
            _store.Dispose();
        }

        [Test]
        public void Insert_EntityWithDateTime_ShouldPersistFieldWithSamePrecision()
        {
            _store.TruncateTable("TestItem");
            var itemA = new TestItem("ItemA");

            _store.Insert(itemA);

            var checkItem = _store.Select<TestItem>().Execute().FirstOrDefault();
            Assert.AreEqual(itemA.TestDate, checkItem.TestDate);
        }

        [Test]
        public void Update_EntityWithDateTime_ShouldPersistFieldWithSamePrecision()
        {
            _store.TruncateTable("TestItem");
            var itemA = new TestItem("ItemA");
            _store.Insert(itemA);
            itemA.TestDate = DateTime.Now;

            _store.Update(itemA);

            var checkItem = _store.Select<TestItem>().Execute().FirstOrDefault();
            Assert.AreEqual(itemA.TestDate.Second, checkItem.TestDate.Second);
        }

        [Test]
        public void Insert_EntityWithAllTypeOfField_ShouldInsertInDb()
        {
            _store.ConnectionBehavior = ConnectionBehavior.Persistent;

            var itemA = new TestItem("ItemA")
            {
                UUID = Guid.NewGuid(),
                ITest = 5,
                Address = "xxxx",
                FTest = 3.14F,
                DBTest = 1.4D,
                DETest = 2.678M,
                TestDate = new DateTime(2016, 12, 14, 11, 38, 14, 10)
            };

            _store.Insert(itemA);

            var count = 0;
            var sql = string.Format("SELECT * FROM TestItem WHERE Id = '{0}'", itemA.Id);
            using (var reader = _store.ExecuteReader(sql))
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
                    count++;
                }
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void CreateOrUpdateStore_LateAddEntity_ShouldUpdateDb()
        {
            _store.AddType<LateAddItem>();

            _store.CreateOrUpdateStore();

            var columns = new List<ColumnFormat>
            {
                new ColumnFormat("Id", 1, false, "int"),
                new ColumnFormat("Name", 2, true, "nvarchar")
            };
            TableFormat.AssertFormat(_store, columns);
        }

        [Test]
        public void BeginTransaction_ShouldDeleteAndInsertAfterCommit()
        {
            var testEntity = new TestItem();
            _store.Insert(testEntity);

            _store.BeginTransaction();
            _store.Delete<TestItem>(testEntity.Id);
            Assert.AreEqual(1, _store.Count<TestItem>());

            _store.Insert(new TestItem());
            _store.Insert(new TestItem());
            _store.Commit();
            Assert.AreEqual(2, _store.Count<TestItem>());
        }

        [Test]
        public void SimpleCrudTest()
        {
            bool beforeInsert = false;
            bool afterInsert = false;
            bool beforeUpdate = false;
            bool afterUpdate = false;
            bool beforeDelete = false;
            bool afterDelete = false;

            _store.AddType<TestItem>();
            _store.CreateOrUpdateStore();

            _store.BeforeInsert += delegate
            {
                beforeInsert = true;
            };
            _store.AfterInsert += delegate
            {
                afterInsert = true;
            };
            _store.BeforeUpdate += delegate
            {
                beforeUpdate = true;
            };
            _store.AfterUpdate += delegate
            {
                afterUpdate = true;
            };
            _store.BeforeDelete += delegate
            {
                beforeDelete = true;
            };
            _store.AfterDelete += delegate
            {
                afterDelete = true;
            };
            
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
            _store.Insert(itemA);
            Assert.IsTrue(beforeInsert, "BeforeInsert never fired");
            Assert.IsTrue(afterInsert, "AfterInsert never fired");

            _store.Insert(itemB);
            _store.Insert(itemC);

            // COUNT
            var count = _store.Count<TestItem>();
            Assert.AreEqual(3, count);

            // SELECT
            var items = _store.Select<TestItem>().Execute();
            Assert.AreEqual(3, items.Count());

            var condition = _store.Condition<TestItem>("Name", itemB.Name, FilterOperator.Equals);
            var item = _store.Select<TestItem, TestItem>().Where(condition).Execute().FirstOrDefault();
            Assert.IsTrue(item.Equals(itemB));

            item = _store.Select<TestItem>(3);
            Assert.IsTrue(item.Equals(itemC));

            // FETCH

            // UPDATE
            itemC.Name = "NewItem";
            itemC.Address = "Changed Address";
            itemC.BigString = "little string";

            // test rollback
            _store.BeginTransaction();
            _store.Update(itemC);
            item = _store.Select<TestItem>(3);
            Assert.IsTrue(item.Name == itemC.Name);
            _store.Rollback();

            item = _store.Select<TestItem>(3);
            Assert.IsTrue(item.Name != itemC.Name);

            // test commit
            _store.BeginTransaction(IsolationLevel.Unspecified);
            _store.Update(itemC);
            _store.Commit();

            Assert.IsTrue(beforeUpdate, "BeforeUpdate never fired");
            Assert.IsTrue(afterUpdate, "AfterUpdate never fired");

            condition = _store.Condition<TestItem>("Name", "ItemC", FilterOperator.Equals);
            item = _store.Select<TestItem, TestItem>().Where(condition).Execute().FirstOrDefault();
            Assert.IsNull(item);

            condition = _store.Condition<TestItem>("Name", itemC.Name, FilterOperator.Equals);
            item = _store.Select<TestItem, TestItem>().Where(condition).Execute().FirstOrDefault();
            Assert.IsTrue(item.Equals(itemC));

            // CONTAINS
            var exists = _store.Contains(itemA);
            Assert.IsTrue(exists);

            // DELETE
            _store.Delete(itemA);
            Assert.IsTrue(beforeDelete, "BeforeDelete never fired");
            Assert.IsTrue(afterDelete, "AfterDelete never fired");
            
            condition = _store.Condition<TestItem>("Name", itemA.Name, FilterOperator.Equals);
            item = _store.Select<TestItem, TestItem>().Where(condition).Execute().FirstOrDefault();
            Assert.IsNull(item);

            // CONTAINS
            exists = _store.Contains(itemA);
            Assert.IsFalse(exists);

            // COUNT
            count = _store.Count<TestItem>();
            Assert.AreEqual(2, count);

            // this will create the table in newer versions of ORM
            _store.AddType<LateAddItem>();

            var newitems = _store.Select<LateAddItem>();
            Assert.IsNotNull(newitems);
        }

        [Test]
        public void Insert_EntityWithEnum_ShouldReturnEnumValue()
        {
            var testRow = new TestTable { EnumField = TestEnum.ValueB };

            _store.Insert(testRow);

            var existing = _store.Select<TestTable>().Execute().First();
            Assert.AreEqual(existing.EnumField, testRow.EnumField);
        }

        [Test]
        public void InsertSelect_EntityWithEnum_ShouldReturnEnumValue()
        {
            var testRow = new TestTable { EnumField = TestEnum.ValueB };
            _store.Insert(testRow);

            testRow.EnumField = TestEnum.ValueC;
            _store.Update(testRow);

            var testFromDatastore = _store.Select<TestTable>().Execute().First();

            Assert.AreEqual(testRow.EnumField, testFromDatastore.EnumField);
        }

        [Test]
        public void SelectJoin_ManyToOneRelation_RelationShouldSharedSameObject()
        {
            var book = new Book();
            _store.Insert(book);
            var bookVersion1 = new BookVersion { BookId = book.Id };
            var bookVersion2 = new BookVersion { BookId = book.Id };
            _store.Insert(bookVersion1);
            _store.Insert(bookVersion2);
            
            var bookVersionList = _store.Select<BookVersion>()
                                         .Join<BookVersion, Book>()
                                         .Execute()
                                         .ToList();

            Assert.AreEqual(2, bookVersionList.Count);
            Assert.AreSame(bookVersionList[0].Book, bookVersionList[1].Book);
        }

        [Test]
        public void SelectLeftJoin_OneToManyRelation_NullRelationShouldBeNull()
        {
            var book1 = new Book();
            _store.Insert(book1);
            var book1Version1 = new BookVersion { BookId = book1.Id };
            _store.Insert(book1Version1);

            var book2Version1 = new BookVersion();
            _store.Insert(book2Version1);


            var bookVersionList = _store.Select<BookVersion>()
                                         .LeftJoin<BookVersion, Book>()
                                         .Execute()
                                         .ToList();

            Assert.AreEqual(2, bookVersionList.Count);
            Assert.AreEqual(book1.Id, bookVersionList[0].BookId);
            Assert.IsNotNull(bookVersionList[0].Book);
            Assert.AreEqual(-1, bookVersionList[1].BookId);
            Assert.IsNull(bookVersionList[1].Book);
        }
    }
}
