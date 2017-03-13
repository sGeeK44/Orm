using System.Collections.Generic;
using System.Data;

namespace Orm.Core.Filters
{
    /// <summary>
    /// Encapsulate behaviour to IN sql condition
    /// </summary>
    public class InCondition : Condition
    {
        private const string In = " IN ";

        public InCondition(IFilterFactory filterFactory, IFilter leftPart, IFilter rightPart)
            : base(filterFactory, leftPart, rightPart, In) { }

        public InCondition(IFilterFactory filterFactory, IFilter leftPart, ObjectValue rightPart)
            : base(filterFactory, leftPart, rightPart, In) { }

        /// <summary>
        /// Convert part to sql string equivalent
        /// </summary>
        /// <param name="params">existing param list to populate in case of part object value</param>
        /// <returns>Sql string representation</returns>
        public override string ToStatement(ref List<IDataParameter> @params)
        {
            return string.Concat(LeftPart.ToStatement(ref @params), ConditionValue, "(", RightPart.ToStatement(ref @params), ")");
        }
    }
}