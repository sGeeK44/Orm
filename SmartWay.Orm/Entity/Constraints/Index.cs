using System.Text;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity.Fields;

namespace SmartWay.Orm.Entity.Constraints
{
    public abstract class Index : IDistinctable
    {
        protected Index(string name, string entityName, Field field)
        {
            Name = name;
            EntityName = entityName;
            Fields = new DistinctCollection<Field> {field};
        }

        protected string Name { get; }
        public bool IsUnique { get; set; }
        public FieldSearchOrder SearchOrder { get; set; }

        private string EntityName { get; }
        protected DistinctCollection<Field> Fields { get; set; }

        /// <summary>
        ///     A unique string key to identify an object in collection
        /// </summary>
        public string Key => Name;

        public void AddField(Field field)
        {
            Fields.Add(field);
        }

        public string GetNameInStore()
        {
            return $"ORM_IDX_{EntityName}_{GetVariablePartName()}_{GetSearchOrder()}";
        }

        public string GetCreateSqlQuery()
        {
            return
                $"CREATE {(IsUnique ? "UNIQUE " : string.Empty)}INDEX {GetNameInStore()} ON [{EntityName}] ({GetFieldInvolves()} {GetSearchOrder()})";
        }

        private string GetFieldInvolves()
        {
            StringBuilder result = null;
            foreach (var field in Fields)
            {
                if (result != null)
                    result.AppendFormat(", ");
                else
                    result = new StringBuilder();

                result.AppendFormat("[{0}]", field.FieldName);
            }

            return result == null ? string.Empty : result.ToString();
        }

        protected abstract string GetVariablePartName();

        private string GetSearchOrder()
        {
            return SearchOrder == FieldSearchOrder.Descending ? "DESC" : "ASC";
        }

        public static Index CreateStandard(string entityName, Field field)
        {
            return new FieldIndex(field.FieldName, entityName, field);
        }

        public static Index CreateCustom(string name, string entityName, Field field)
        {
            return new CustomIndex(name, entityName, field);
        }
    }
}