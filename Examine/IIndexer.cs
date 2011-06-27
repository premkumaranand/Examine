using System.Collections.Generic;
using System.Xml.Linq;

namespace Examine
{
    /// <summary>
    /// Interface to represent an Examine Indexer
    /// </summary>
    public interface IIndexer
    {
        /// <summary>
        /// Forces a particular XML node to be reindexed
        /// </summary>
        /// <param name="node">XML node to reindex</param>
        /// <param name="type">Type of index to use</param>
        void ReIndexNode(IDictionary<string, string> node, string type);
        
        /// <summary>
        /// Deletes a node from the index
        /// </summary>
        /// <param name="nodeId">Node to delete</param>
        void DeleteFromIndex(string nodeId);
        
        /// <summary>
        /// Gets/sets the index criteria to create the index with
        /// </summary>
        /// <value>The indexer data.</value>
        IIndexCriteria IndexerData { get; set; }

    }
}
