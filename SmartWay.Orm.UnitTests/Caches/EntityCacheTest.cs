using NUnit.Framework;
using SmartWay.Orm.Caches;

namespace SmartWay.Orm.UnitTests.Caches
{
    [TestFixture]
    public class RemoveEntityTest
    {
        [SetUp]
        public void SetUp()
        {
            Cache = new EntityCache();
        }

        private EntityCache Cache { get; set; }

        [Test]
        public void EntityNotGettedFromCache()
        {
            var entity = new ConcreteEntity {Id = 1};
            var notGetFromCache = new ConcreteEntity {Id = 1};

            Cache.Add(entity, entity.Id);
            Cache.Remove(notGetFromCache);

            Assert.IsNull(Cache.GetOrDefault(entity));
        }
    }
}