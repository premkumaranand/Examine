using System.Collections.Generic;

namespace Examine
{
    /// <summary>
    /// Represents an item going into the index
    /// </summary>
    public class IndexItem
    {
        /// <summary>
        /// Gets the fields.
        /// </summary>
        public IDictionary<string, string> Fields { get; set; }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets the type of the item.
        /// </summary>
        /// <value>
        /// The type of the item.
        /// </value>
        public string ItemType { get; set; }
    }
}