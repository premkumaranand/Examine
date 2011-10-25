using System.Collections.Generic;

namespace Examine
{

    /// <summary>
    /// The data type of a field
    /// </summary>
    public enum FieldDataType
    {
        String, Number, Int, Float, Double, Long, DateDay, DateHour, DateMinute, DateSecond, DateTime
    }

    /// <summary>
    /// Represents a field in an index
    /// </summary>
    public class ItemField
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemField"/> class.
        /// </summary>
        public ItemField()
        {
            DataType = FieldDataType.String;
            EnableSorting = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemField"/> class.
        /// </summary>
        /// <param name="fieldValue">The field value.</param>
        public ItemField(object fieldValue)
            :this()
        {
            FieldValue = fieldValue;
        }

        /// <summary>
        /// Gets or sets the field value.
        /// </summary>
        /// <value>
        /// The field value.
        /// </value>
        public object FieldValue { get; set; }

        /// <summary>
        /// Gets or sets the type of the data.
        /// </summary>
        /// <value>
        /// The type of the data.
        /// </value>
        public FieldDataType DataType { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether [enable sorting].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable sorting]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableSorting { get; set; }

    }

    /// <summary>
    /// Represents an item going into the index
    /// </summary>
    public class IndexItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexItem"/> class.
        /// </summary>
        public IndexItem()
        {
            Fields = new Dictionary<string, ItemField>();
        }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        public IDictionary<string, ItemField> Fields { get; set; }

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
        public string ItemCategory { get; set; }
    }
}