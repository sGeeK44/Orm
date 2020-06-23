using SmartWay.Orm.Interfaces;

namespace SmartWay.Orm.Filters
{
    /// <summary>
    ///     Expose methods to create/population condition of filter
    /// </summary>
    public interface ICondition : IFilter
    {
        IEntityInfo Entity1 { get; set; }
        IEntityInfo Entity2 { get; set; }

        /// <summary>
        ///     Create an And association with new condition and current
        /// </summary>
        /// <typeparam name="TEntity">Type of entity associated to column name on new condition</typeparam>
        /// <param name="columnName">Column Name involve on new condition</param>
        /// <param name="value">Value to compare to column name on new condition</param>
        /// <param name="filterOperator">Operator to apply on new condition</param>
        /// <returns>New build condition</returns>
        ICondition And<TEntity>(string columnName, object value, FilterOperator filterOperator);

        /// <summary>
        ///     Create an And association with new condition and current
        /// </summary>
        /// <typeparam name="TEntity1">Type of entity associated to column name 1 on new condition</typeparam>
        /// <typeparam name="TEntity2">Type of entity associated to column name 2 on new condition</typeparam>
        /// <param name="columnName1">First column Name involve on new condition</param>
        /// <param name="columnName2">Second column Name to compare on new condition</param>
        /// <param name="filterOperator">Operator to apply on new condition</param>
        /// <returns>New build condition</returns>
        ICondition And<TEntity1, TEntity2>(string columnName1, string columnName2, FilterOperator filterOperator);

        /// <summary>
        ///     Create an And association with specified condition and current
        /// </summary>
        /// <param name="condition">Condition to associate</param>
        /// <returns>New build condition</returns>
        ICondition And(ICondition condition);

        /// <summary>
        ///     Create an AndNot association with specified condition and current
        /// </summary>
        /// <param name="condition">Condition to associate</param>
        /// <returns>New build condition</returns>
        ICondition AndNot(ICondition condition);

        /// <summary>
        ///     Create an Or association with new condition and current
        /// </summary>
        /// <typeparam name="TEntity">Type of entity associated to column name on new condition</typeparam>
        /// <param name="columnName">Column Name involve on new condition</param>
        /// <param name="value">Value to compare to column name on new condition</param>
        /// <param name="filterOperator">Operator to apply on new condition</param>
        /// <returns>New build condition</returns>
        ICondition Or<TEntity>(string columnName, object value, FilterOperator filterOperator);

        /// <summary>
        ///     Create an Or association with new condition and current
        /// </summary>
        /// <typeparam name="TEntity1">Type of entity associated to column name 1 on new condition</typeparam>
        /// <typeparam name="TEntity2">Type of entity associated to column name 2 on new condition</typeparam>
        /// <param name="columnName1">First column Name involve on new condition</param>
        /// <param name="columnName2">Second column Name to compare on new condition</param>
        /// <param name="filterOperator">Operator to apply on new condition</param>
        /// <returns>New build condition</returns>
        ICondition Or<TEntity1, TEntity2>(string columnName1, string columnName2, FilterOperator filterOperator);

        /// <summary>
        ///     Create an Or association with specified condition and current
        /// </summary>
        /// <param name="condition">Condition to associate</param>
        /// <returns>New build condition</returns>
        ICondition Or(ICondition condition);
    }
}