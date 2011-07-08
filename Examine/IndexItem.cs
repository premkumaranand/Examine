using System.Collections.Generic;

namespace Examine
{

    public enum FieldDataType
    {
        String, Number, Int, Float, Double, Long, DateTime, DateYear, DateMonth, DateDay, DateHour, DateMinute
    }

    public class ItemField
    {

        public ItemField()
        {
            DataType = FieldDataType.String;
            EnableSorting = false;
        }

        public ItemField(string fieldValue)
            :this()
        {
            FieldValue = fieldValue;
        }

        public string FieldValue { get; set; }
        public FieldDataType DataType { get; set; }
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