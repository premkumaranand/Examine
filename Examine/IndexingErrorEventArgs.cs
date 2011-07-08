using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examine
{
    /// <summary>
    /// 
    /// </summary>
    public class IndexingErrorEventArgs : EventArgs
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingErrorEventArgs"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="id">The id.</param>
        /// <param name="innerException">The inner exception.</param>
        public IndexingErrorEventArgs(string message, string id, Exception innerException)
        {
            this.Id = id;
            this.Message = message;
            this.InnerException = innerException;
        }

        /// <summary>
        /// Gets the inner exception.
        /// </summary>
        public Exception InnerException { get; private set; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; private set; }

        #region INodeEventArgs Members

        /// <summary>
        /// Gets the id.
        /// </summary>
        public string Id { get; private set; }

        #endregion
    }
}
