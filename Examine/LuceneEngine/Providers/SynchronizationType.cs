namespace Examine.LuceneEngine.Providers
{
    /// <summary>
    /// The index ingestion thread synchronization type
    /// </summary>
    public enum SynchronizationType
    {
        /// <summary>
        /// Processes index queue in the same thread as the application
        /// </summary>
        Synchronized,

        /// <summary>
        /// Processes index queue using a System.Threading.Task
        /// </summary>
        Asynchronous
    }
}