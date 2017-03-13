using Orm.Core.Constants;

namespace Orm.Core.Interfaces
{
    public interface IDefaultValue
    {
        DefaultType DefaultType { get; }
        object GetDefaultValue();
    }
}
