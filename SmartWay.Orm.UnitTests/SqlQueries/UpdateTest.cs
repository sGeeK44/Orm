using System;
using System.Collections.Generic;
using System.Data;
using Moq;
using NUnit.Framework;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Queries;
using SmartWay.Orm.Sql;
using SmartWay.Orm.Sql.Queries;
using SmartWay.Orm.Testkit.Entities;

namespace SmartWay.Orm.UnitTests.SqlQueries
{
    [TestFixture]
    public class UpdateTest
    {
        [SetUp]
        public void Init()
        {
            var fieldPropertyFactory = new Mock<IFieldPropertyFactory>();

            var sqlCommand = new Mock<IDbCommand>();

            var sqlFactory = new Mock<ISqlFactory>();
            sqlFactory.Setup(_ => _.CreateFieldPropertyFactory()).Returns(fieldPropertyFactory.Object);
            sqlFactory.Setup(_ => _.AddParam(It.IsAny<object>(), It.IsAny<ICollection<IDataParameter>>())).Returns("X");
            sqlFactory.Setup(_ => _.CreateCommand()).Returns(sqlCommand.Object);

            var connection = new Mock<IDbConnection>();

            var engine = new Mock<IDbEngine>();
            engine.Setup(_ => _.GetNewConnection()).Returns(connection.Object);

            DataStore = new SqlDataStore(engine.Object, sqlFactory.Object);
            DataStore.AddType<Book>();
            UpdateQuery = new Updatable<Book>(DataStore);
        }

        private IDataStore DataStore { get; set; }
        private Updatable<Book> UpdateQuery { get; set; }

        [Test]
        public void EntityTypeDoesNotExistInDatastore()
        {
            Assert.Throws<NotSupportedException>(
                () =>
                {
                    var dumy = new Updatable<Author>(DataStore);
                }
            );
        }

        [Test]
        public void OneField()
        {
            UpdateQuery.Set(Book.BookTypeColName, BookType.NonFiction).Update();

            var sql = UpdateQuery.ToStatement(null);

            Assert.AreEqual("UPDATE [Book] SET [Book].[BookType] = X;", sql);
        }

        [Test]
        public void TwoField()
        {
            UpdateQuery.Set(Book.BookTypeColName, BookType.NonFiction).Update();
            UpdateQuery.Set(Book.BookTypeColName, BookType.NonFiction).Update();

            var sql = UpdateQuery.ToStatement(null);

            Assert.AreEqual("UPDATE [Book] SET [Book].[BookType] = X, [Book].[BookType] = X;", sql);
        }

        [Test]
        public void WithFilter()
        {
            var filter = DataStore.Condition<Book>(Book.BookTypeColName, BookType.Fiction, FilterOperator.Equals);
            UpdateQuery.Set(Book.BookTypeColName, BookType.NonFiction).Where(filter).Update();

            var sql = UpdateQuery.ToStatement(null);

            Assert.AreEqual("UPDATE [Book] SET [Book].[BookType] = X WHERE [Book].[BookType] = X;", sql);
        }

        [Test]
        public void WithoutSetClause()
        {
            Assert.Throws<NotSupportedException>(
                () => { UpdateQuery.ToStatement(null); }
            );
        }
    }
}