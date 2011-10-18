using System;
using System.Xml.Linq;

namespace Examine
{
    /// <summary>
    /// Interface to represent an Examine Indexer
    /// </summary>
    public interface IIndexer : IDisposable
    {
        /// <summary>
        /// Forces a particular XML node to be reindexed
        /// </summary>
        /// <param name="items">item to reindex</param>
        void PerformIndexing(params IndexOperation[] items);

    }
}
