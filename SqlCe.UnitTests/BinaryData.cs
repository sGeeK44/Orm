using Orm.Core;
using Orm.Core.Attributes;
using Orm.Core.Constants;

namespace Orm.SqlCe.UnitTests
{
    [Entity(KeyScheme.Identity)]
    public class BinaryData
    {
        [Field(IsPrimaryKey=true, SearchOrder=FieldSearchOrder.Ascending)]
        public int ID { get; set; }
        
        [Field()]
        public byte[] BinaryField { get; set; }

        [Field()]
        public byte[] ImageField { get; set; }

        [Field()]
        public string NTextField { get; set; }
    }
}
