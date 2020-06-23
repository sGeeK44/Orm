using SmartWay.Orm.Constants;

namespace SmartWay.Orm
{
    public class SqlIndexInfo
    {
        public string IndexName { get; set; }
        public string TableName { get; set; }
        public string[] Fields { get; set; }
        public FieldSearchOrder SearchOrder { get; set; }
        public bool IsUnique { get; set; }

        public bool IsComposite => Fields.Length > 1;
    }
}