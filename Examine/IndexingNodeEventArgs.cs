using System.Collections.Generic;
using System.ComponentModel;

namespace Examine
{
    /// <summary>
    /// 
    /// </summary>
    public class IndexingNodeEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingNodeEventArgs"/> class.
        /// </summary>
        public IndexingNodeEventArgs(IndexItem item)
        {
            Item = item;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public IndexItem Item { get; private set; }
    }
}