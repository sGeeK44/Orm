namespace SmartWay.Orm.Entity.Fields
{
    public interface ISqlNumericType
    {
        /// <summary>
        ///     Get maximal number used to represent current custom field. Default should be zero
        ///     <example>Precision for 123.45 is equal to 5.</example>
        /// </summary>
        byte Precision { get; }

        /// <summary>
        ///     Get decimal number. Default should be zero
        ///     <example>Scale for 123.45 is equal to 2.</example>
        /// </summary>
        byte Scale { get; }
    }
}