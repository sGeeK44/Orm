using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Moq;
using NFluent;
using NUnit.Framework;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Queries;
using SmartWay.Orm.Sql.Queries;
using SmartWay.Orm.Testkit.Entities;

namespace SmartWay.Orm.UnitTests.SqlQueries
{
    [TestFixture]
    public class WhereTest
    {
        private const string SqlValue = "####";

        [SetUp]
        public void Init()
        {
            Entities = new EntityInfoCollection();
            var factory = new Mock<IFieldPropertyFactory>();
            Entity = EntityInfo.Create(factory.Object, Entities, typeof(Author));
            SqlFactory = new Mock<ISqlFactory>();
            FilterFactory = new FilterFactory(SqlFactory.Object, Entities);
            Params = new List<IDataParameter>();
            SqlFactory.Setup(
                _ => _.AddParam(It.IsAny<object>(), Params)
                ).Returns(SqlValue);
        }

        public List<IDataParameter> Params { get; set; }

        public Mock<ISqlFactory> SqlFactory { get; set; }

        public FilterFactory FilterFactory { get; set; }

        private IEntityInfo Entity { get; set; }
        private EntityInfoCollection Entities { get; set; }

        [Test]
        public void ToStatement_Empty_ShouldReturnExpectedSql()
        {
            var where = new Where();

            var @params = new List<IDataParameter>();
            Assert.AreEqual(string.Empty, where.ToStatement(@params));
            Assert.AreEqual(0, @params.Count);
        }

        [Test]
        public void ToStatement_OneFilter_ShouldReturnExpectedSqlString()
        {
            var primaryKey = FilterFactory.ToColumnValue(Entity, Author.PrimaryKeyName);
            var value = FilterFactory.ToObjectValue(1);
            var filter = FilterFactory.Equal(primaryKey, value);
            var where = new Where(filter);

            var result = where.ToStatement(Params);

            Assert.AreEqual($" WHERE [Author].[Id] = {SqlValue}", result);
        }

        [TestCase]
        public void ToStatement_TruePredicate_ShouldReturnNull()
        {
            var query = CreateSqlWhereClause<Author>(_ => true);

            Check.That(query.ToStatement(new List<IDataParameter>())).IsEmpty();
        }

        [TestCase]
        public void ToStatement_ById()
        {
            var ent = new Author();
            var query = CreateSqlWhereClause<Author>(_ => _.Id == ent.Id);
            var expected = $" WHERE [Author].[Id] = {SqlValue}";

            Assert.AreEqual(expected, query.ToStatement(Params));
        }

        [TestCase]
        public void ToStatement_ByVarId()
        {
            var query = CreateSqlWhereClause<Author>(_ => _.Id == 0);
            var expected = $" WHERE [Author].[Id] = {SqlValue}";

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_ByName()
        {
            var query = CreateSqlWhereClause<Author>(_ => _.Name == "NameSearch");
            var expected = $" WHERE [Author].[Name] = {SqlValue}";

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_And()
        {
            var ent = new Author();
            var query = CreateSqlWhereClause<Author>(_ => _.Id == ent.Id && _.Name == "NameSearch");
            var expected = string.Format(" WHERE [Author].[Id] = {0} AND [Author].[Name] = {0}", SqlValue);

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_Or()
        {
            var ent = new Author();
            var query = CreateSqlWhereClause<Author>(_ => _.Id == ent.Id || _.Name == "NameSearch");
            var expected = string.Format(" WHERE ([Author].[Id] = {0} OR [Author].[Name] = {0})", SqlValue);

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_AddOperator()
        {
            var param = 5;
            var query = CreateSqlWhereClause<Author>(_ => _.Age == param + 5);
            var expected = string.Format(" WHERE [Author].[Age] = {0} + {0}", SqlValue);

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_SubtractOperator()
        {
            var param = 5;
            var query = CreateSqlWhereClause<Author>(_ => _.Age == param - 5);
            var expected = string.Format(" WHERE [Author].[Age] = {0} - {0}", SqlValue);

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_NegateMember()
        {
            var query = CreateSqlWhereClause<Author>(_ => -(_.Age) == 5);
            var expected = $" WHERE -[Author].[Age] = {SqlValue}";

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_MultiplyMember()
        {
            var query = CreateSqlWhereClause<Author>(_ => _.Age * 5 == 5);
            var expected = string.Format(" WHERE [Author].[Age] * {0} = {0}", SqlValue);

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_DivideMember()
        {
            var query = CreateSqlWhereClause<Author>(_ => _.Age / 5 == 5);
            var expected = string.Format(" WHERE [Author].[Age] / {0} = {0}", SqlValue);

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_ModuloMember()
        {
            var query = CreateSqlWhereClause<Author>(_ => _.Age % 5 == 5);
            var expected = string.Format(" WHERE [Author].[Age] MOD {0} = {0}", SqlValue);

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_LessThanExpression()
        {
            var query = CreateSqlWhereClause<Author>(_ => _.Age < 5);
            var expected = $" WHERE [Author].[Age] < {SqlValue}";

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_LessThanOrEqualExpression()
        {
            var query = CreateSqlWhereClause<Author>(_ => _.Age <= 5);
            var expected = $" WHERE [Author].[Age] <= {SqlValue}";

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_MoreThanExpression()
        {
            var query = CreateSqlWhereClause<Author>(_ => _.Age > 5);
            var expected = $" WHERE [Author].[Age] > {SqlValue}";

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_MoreThanOrEqualExpression()
        {
            var query = CreateSqlWhereClause<Author>(_ => _.Age >= 5);
            var expected = $" WHERE [Author].[Age] >= {SqlValue}";

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_DifferentExpression()
        {
            var query = CreateSqlWhereClause<Author>(_ => _.Age != 5);
            var expected = $" WHERE [Author].[Age] <> {SqlValue}";

            Assert.AreEqual(expected, query.ToStatement(new List<IDataParameter>()));
        }

        [TestCase]
        public void ToStatement_NegateExpression()
        {
            var query = CreateSqlWhereClause<Author>(_ => !_.Active);

            Assert.AreEqual(" WHERE NOT [Author].[Active]", query.ToStatement(new List<IDataParameter>()));
        }

        public IClause CreateSqlWhereClause<T>(Expression<Func<T, bool>> predicate)
            where T : class
        {
            var filterBuilder = new FilterBuilder<T>(Entities, FilterFactory, predicate);
            return filterBuilder.Build();
        }
    }
}