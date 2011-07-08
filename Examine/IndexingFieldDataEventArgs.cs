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
    public class IndexingFieldDataEventArgs : EventArgs
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingFieldDataEventArgs"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldValue">The field value.</param>
        /// <param name="isStandardField">if set to <c>true</c> [is standard field].</param>
        public IndexingFieldDataEventArgs(IndexItem item, string fieldName, ItemField fieldValue, bool isStandardField)
        {
            this.Item = item;
            this.FieldName = fieldName;
            this.FieldValue = fieldValue;
            this.IsStandardField = isStandardField;
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
        public ItemField FieldValue { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this instance is standard field.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is standard field; otherwise, <c>false</c>.
        /// </value>
        public bool IsStandardField { get; private set; }

       
    }
}
