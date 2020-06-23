using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;

namespace SmartWay.Orm.Sqlite.UnitTests
{
    [TestFixture]
    public class AddParam
    {
        [Test]
        public void Guid()
        {
            var factory = new SqliteFactory();
            var @params = new List<IDataParameter>();

            var paramValue = factory.AddParam(new Guid("A383CDBF-4791-4B9F-9DD4-6286108388BE"), @params);

            Assert.AreEqual("@p0", paramValue);
            Assert.AreEqual(1, @params.Count);
            Assert.AreEqual("a383cdbf-4791-4b9f-9dd4-6286108388be", @params[0].Value);
        }

        [Test]
        public void Integer()
        {
            var factory = new SqliteFactory();
            var @params = new List<IDataParameter>();

            var paramValue = factory.AddParam(1, @params);

            Assert.AreEqual("@p0", paramValue);
            Assert.AreEqual(1, @params.Count);
            Assert.AreEqual(1, @params[0].Value);
        }
    }
}