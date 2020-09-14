using System.Collections.Generic;
using Moq;
using SmartWay.Orm.Repositories;

namespace SmartWay.Orm.UnitTests
{
    public class RepositoryTester : Repository<ConcreteEntity, ConcreteEntity>
    {
        public RepositoryTester()
        {
            Mock = new Mock<IRepository<ConcreteEntity>>();
        }

        public Mock<IRepository<ConcreteEntity>> Mock { get; set; }

        public override List<ConcreteEntity> GetAllReference<TForeignEntity>(object id)
        {
            return Mock.Object.GetAllReference<TForeignEntity>(id);
        }
    }
}