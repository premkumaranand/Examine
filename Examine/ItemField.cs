namespace Examine
{
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
}