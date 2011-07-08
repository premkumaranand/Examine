using System;

namespace Examine
{
    /// <summary>
    /// 
    /// </summary>
    public class IndexedNodeEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedNodeEventArgs"/> class.
        /// </summary>
        public IndexedNodeEventArgs(IndexItem item)
        {
            Item = item;
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        public IndexItem Item { get; private set; }
    }
}
