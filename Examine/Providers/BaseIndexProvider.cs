using System;
using System.Configuration.Provider;
using Examine;
using System.Xml.Linq;

namespace Examine.Providers
{
    /// <summary>
    /// Base class for an Examine Index Provider. You must implement this class to create an IndexProvider
    /// </summary>
    public abstract class BaseIndexProvider : ProviderBase, IIndexer, IDisposable
    {

        #region IIndexer members      
        
        /// <summary>
        /// Forces a particular XML node to be reindexed
        /// </summary>
        /// <param name="items">XML node to reindex</param>
        public abstract void PerformIndexing(params IndexOperation[] items);

        #endregion

        #region Events
        /// <summary>
        /// Occurs for an Indexing Error
        /// </summary>
        public event EventHandler<IndexingErrorEventArgs> IndexingError;

        /// <summary>
        /// Occurs when a node is in its Indexing phase
        /// </summary>
        public event EventHandler<IndexingNodeEventArgs> NodeIndexing;
        /// <summary>
        /// Occurs when a node is in its Indexed phase
        /// </summary>
        public event EventHandler<IndexedNodeEventArgs> NodeIndexed;

        /// <summary>
        /// Occurs when the collection of nodes have been indexed
        /// </summary>
        public event EventHandler<IndexedNodesEventArgs> NodesIndexed;

        /// <summary>
        /// Occurs when a node is deleted from the index
        /// </summary>
        public event EventHandler<DeleteIndexEventArgs> IndexDeleted;


        #endregion

        #region Protected Event callers

        /// <summary>
        /// Raises the <see cref="E:IndexingError"/> event.
        /// </summary>
        /// <param name="e">The <see cref="Examine.IndexingErrorEventArgs"/> instance containing the event data.</param>
        protected virtual void OnIndexingError(IndexingErrorEventArgs e)
        {
            if (IndexingError != null)
                IndexingError(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:NodeIndexed"/> event.
        /// </summary>
        /// <param name="e">The <see cref="Examine.IndexedNodeEventArgs"/> instance containing the event data.</param>
        protected virtual void OnNodeIndexed(IndexedNodeEventArgs e)
        {
            if (NodeIndexed != null)
                NodeIndexed(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:NodeIndexing"/> event.
        /// </summary>
        /// <param name="e">The <see cref="Examine.IndexingNodeEventArgs"/> instance containing the event data.</param>
        protected virtual void OnNodeIndexing(IndexingNodeEventArgs e)
        {
            if (NodeIndexing != null)
                NodeIndexing(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:IndexDeleted"/> event.
        /// </summary>
        /// <param name="e">The <see cref="Examine.DeleteIndexEventArgs"/> instance containing the event data.</param>
        protected virtual void OnIndexDeleted(DeleteIndexEventArgs e)
        {
            if (IndexDeleted != null)
                IndexDeleted(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:NodesIndexed"/> event.
        /// </summary>
        /// <param name="e">The <see cref="Examine.IndexedNodesEventArgs"/> instance containing the event data.</param>
        protected virtual void OnNodesIndexed(IndexedNodesEventArgs e)
        {
            if (NodesIndexed != null)
                NodesIndexed(this, e);
        }

        #endregion


        #region IDisposable Members

        private bool _disposed = false;

        /// <summary>
        /// When the object is disposed, all data should be written
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    DisposeResources();
                }
                _disposed = true;
            }
        }

        protected virtual void DisposeResources()
        {
           
        }

        #endregion
    }

   
}
