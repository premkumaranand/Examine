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
        /// <param name="item">item to reindex</param>
        /// <param name="indexCategory">Category of index to use</param>
        void ReIndexNode(IndexItem item, string indexCategory);
        
        /// <summary>
        /// Deletes a node from the index
        /// </summary>
        /// <param name="id">item to delete</param>
        void DeleteFromIndex(string id);
        
        /// <summary>
        /// Gets/sets the index criteria to create the index with
        /// </summary>
        /// <value>The indexer data.</value>
        IndexCriteria IndexerData { get; set; }

    }
}
