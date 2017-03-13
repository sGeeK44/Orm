using Orm.Core.Constants;
using Orm.Core.Interfaces;

namespace Orm.Core
{
    public class SqlDateTimeDefault : IDefaultValue
    {
        public DefaultType DefaultType
        {
            get { return DefaultType.CurrentDateTime; }
        }

        public object GetDefaultValue()
        {
            return "GETDATE()";
        }

        public static SqlDateTimeDefault Value
        {
            get { return new SqlDateTimeDefault(); }
        }
    }
}
