using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using Examine;

namespace Examine.LuceneEngine
{
    /// <summary>
    /// Event arguments for a Document Writing event
    /// </summary>
    public class DocumentWritingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Lucene.NET Document, including all previously added fields
        /// </summary>
        public Document Document { get; private set; }
        /// <summary>
        /// Fields of the indexer
        /// </summary>
        public IndexItem Item { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="item"></param>
        public DocumentWritingEventArgs(Document d, IndexItem item)
        {
            this.Document = d;
            this.Item = item;
        }
    }
}
