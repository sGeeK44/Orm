using System.Linq;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity.Fields;

namespace SmartWay.Orm.Entity.Constraints
{
    public class FieldIndex : Index
    {
        public FieldIndex(string name, string entityName, Field field)
            : base(name, entityName, field)
        {
            IsUnique = field.RequireUniqueValue;
            SearchOrder = field.SearchOrder != FieldSearchOrder.NotSearchable
                ? field.SearchOrder
                : FieldSearchOrder.Ascending;
        }

        protected override string GetVariablePartName()
        {
            return Fields.First().FieldName;
        }
    }
}