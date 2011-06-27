using System.ComponentModel;
using System.Collections.Generic;

namespace Examine
{
    /// <summary>
    /// 
    /// </summary>
    public class IndexingNodeEventArgs : CancelEventArgs, INodeEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingNodeEventArgs"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="indexType">Type of the index.</param>
        public IndexingNodeEventArgs(string id, Dictionary<string, string> fields, string indexType)
        {
            Id = id;
            Fields = fields;
            IndexType = indexType;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        public Dictionary<string, string> Fields { get; private set; }

        /// <summary>
        /// Gets the type of the index.
        /// </summary>
        /// <value>
        /// The type of the index.
        /// </value>
        public string IndexType { get; private set; }
    }
}