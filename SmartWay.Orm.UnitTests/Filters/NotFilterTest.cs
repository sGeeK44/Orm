using System.Collections.Generic;
using System.Data;
using Moq;
using NUnit.Framework;
using SmartWay.Orm.Filters;

namespace SmartWay.Orm.UnitTests.Filters
{
    [TestFixture]
    public class NotTest
    {
        [Test]
        public void ValidCondition()
        {
            var @params = new List<IDataParameter>();
            var filterToNegate = new Mock<IFilter>();
            filterToNegate.Setup(_ => _.ToStatement(@params)).Returns("Filter");
            var condition = new Not(filterToNegate.Object);

            Assert.AreEqual(condition.ToStatement(@params), "NOT(Filter)");
        }
    }
}