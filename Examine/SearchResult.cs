using System;
using System.Collections.Generic;
using System.Linq;

namespace Examine
{
    /// <summary>
    /// Represents a search result
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult"/> class.
        /// </summary>
        public SearchResult()
        {
            this.Fields = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>
        /// The score.
        /// </value>
        public float Score { get; set; }

        /// <summary>
        /// Gets or sets the fields.
        /// </summary>
        /// <value>
        /// The fields.
        /// </value>
        public IDictionary<string, string> Fields { get; protected set; }

        /// <summary>
        /// Returns the key value pair for the index specified
        /// </summary>
        /// <param name="resultIndex"></param>
        /// <returns></returns>
        public KeyValuePair<string, string> this[int resultIndex] 
        {
            get
            {
                return Fields.ToArray()[resultIndex];
            }
        }

        /// <summary>
        /// Returns the value for the key specified
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key] 
        {
            get
            {
                return Fields[key];
            }
        }
        
        /// <summary>
        /// Override this method so that the Distinct() operator works
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var result = (SearchResult)obj;

            return Id.Equals(result.Id);
        }

        /// <summary>
        /// Override this method so that the Distinct() operator works
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

    }
}
