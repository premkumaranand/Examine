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
    public class IndexingFieldDataEventArgs : EventArgs, INodeEventArgs
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingFieldDataEventArgs"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldValue">The field value.</param>
        /// <param name="isStandardField">if set to <c>true</c> [is standard field].</param>
        /// <param name="id">The node id.</param>
        public IndexingFieldDataEventArgs(IndexItem item, string fieldName, string fieldValue, bool isStandardField, string id)
        {
            this.Item = item;
            this.FieldName = fieldName;
            this.FieldValue = fieldValue;
            this.IsStandardField = isStandardField;
            this.Id = id;
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        public IndexItem Item { get; private set; }
        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        /// <value>
        /// The name of the field.
        /// </value>
        public string FieldName { get; private set; }
        /// <summary>
        /// Gets the field value.
        /// </summary>
        public string FieldValue { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this instance is standard field.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is standard field; otherwise, <c>false</c>.
        /// </value>
        public bool IsStandardField { get; private set; }

        #region INodeEventArgs Members

        /// <summary>
        /// Gets the node id.
        /// </summary>
        public string Id { get; private set; }

        #endregion
    }
}
