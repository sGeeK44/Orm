namespace SmartWay.Orm.Filters
{
    /// <summary>
    ///     Encapsulate behaviour to manage subtract operation
    /// </summary>
    public class DivideOperation : Condition
    {
        private const string Subtract = " / ";

        public DivideOperation(IFilterFactory filterFactory, IFilter leftPart, IFilter rightPart)
            : base(filterFactory, leftPart, rightPart, Subtract)
        {
        }
    }
}