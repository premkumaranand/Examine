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
        /// <param name="id">The id.</param>
        public IndexedNodeEventArgs(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public string Id { get; private set; }
    }
}
