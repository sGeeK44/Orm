using NUnit.Framework;
using SmartWay.Orm.Caches;

namespace SmartWay.Orm.UnitTests.Caches
{
    [TestFixture]
    public class InvalidateEntitiesTest
    {
        [SetUp]
        public void SetUp()
        {
            Cache = new EntitiesCache();
        }

        private EntitiesCache Cache { get; set; }

        private ConcreteEntity AddEntity(int id)
        {
            var entity = new ConcreteEntity {Id = id};
            Cache.Cache(entity, entity.Id);
            return entity;
        }

        [Test]
        public void SeveralEntityInvalidate()
        {
            var entity1 = AddEntity(1);
            var entity2 = AddEntity(2);

            Cache.Invalidate<ConcreteEntity>(_ => _.Id == entity1.Id);

            var removedEntityCached = Cache.GetOrDefault(typeof(ConcreteEntity), entity1.Id);
            Assert.IsNull(removedEntityCached);

            var entityStillCached = Cache.GetOrDefault(typeof(ConcreteEntity), entity2.Id);
            Assert.AreEqual(entity2, entityStillCached);
        }
    }
}