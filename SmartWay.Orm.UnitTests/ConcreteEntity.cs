using SmartWay.Orm.Attributes;
using SmartWay.Orm.Entity;
using SmartWay.Orm.Repositories;

namespace SmartWay.Orm.UnitTests
{
    [Entity]
    public class ConcreteEntity : EntityBase<ConcreteEntity>
    {
        private IRepository<ConcreteEntity> _repository;

        public override IRepository<ConcreteEntity> Repository
        {
            get => _repository;
            set => _repository = value;
        }

        [Field] public string Name { get; set; }

        public void SetRepository(IRepository<ConcreteEntity> repo)
        {
            _repository = repo;
        }
    }
}