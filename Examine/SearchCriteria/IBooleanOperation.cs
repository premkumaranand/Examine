
namespace Examine.SearchCriteria
{
    /// <summary>
    /// Defines the supported operation for addition of additional clauses in the fluent API
    /// </summary>
    public interface IBooleanOperation
    {
        /// <summary>
        /// Sets the next operation to be AND
        /// </summary>
        /// <returns></returns>
        IQuery Must();
        /// <summary>
        /// Sets the next operation to be OR
        /// </summary>
        /// <returns></returns>
        IQuery Should();
        /// <summary>
        /// Sets the next operation to be NOT
        /// </summary>
        /// <returns></returns>
        IQuery Not();

        /// <summary>
        /// Compiles this instance for fluent API conclusion
        /// </summary>
        /// <returns></returns>
        IQuery Compile();
    }
}
