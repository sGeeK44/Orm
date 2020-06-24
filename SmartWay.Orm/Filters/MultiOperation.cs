namespace SmartWay.Orm.Filters
{
    /// <summary>
    ///     Encapsulate behaviour to manage subtract operation
    /// </summary>
    public class MultiOperation : Condition
    {
        private const string Subtract = " * ";

        public MultiOperation(IFilterFactory filterFactory, IFilter leftPart, IFilter rightPart)
            : base(filterFactory, leftPart, rightPart, Subtract)
        {
        }
    }
}