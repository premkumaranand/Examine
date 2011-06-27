using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Examine
{
    /// <summary>
    /// 
    /// </summary>
    public class IndexingNodeDataEventArgs : IndexingNodeEventArgs
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingNodeDataEventArgs"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="id">The node id.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="indexType">Type of the index.</param>
        public IndexingNodeDataEventArgs(IndexItem item, string id, Dictionary<string, string> fields, string indexType)
            : base(id, fields, indexType)
        {
            this.Item = item;
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        public IndexItem Item { get; private set; }
    }
}
