using Orm.Core;
using Orm.Core.Attributes;
using Orm.Core.Constants;

namespace Orm.SqlCe.UnitTests.Entities
{
    [Entity(KeyScheme.Identity)]
    class SeekItem
    {
        [Field(IsPrimaryKey = true)]
        public int ID { get; set; }

        [Field(SearchOrder=FieldSearchOrder.Ascending)]
        public int SeekField { get; set; }

        [Field]
        public string Data { get; set; }

    }
}
