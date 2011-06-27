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
        SingleThreaded,

        /// <summary>
        /// Processes index queue in a background worker thread
        /// </summary>
        AsyncBackgroundWorker
    }
}