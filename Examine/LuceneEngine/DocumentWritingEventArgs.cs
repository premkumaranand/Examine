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
    public class DocumentWritingEventArgs : CancelEventArgs, INodeEventArgs
    {
        /// <summary>
        /// Lucene.NET Document, including all previously added fields
        /// </summary>
        public Document Document { get; private set; }
        /// <summary>
        /// Fields of the indexer
        /// </summary>
        public IDictionary<string, string> Fields { get; private set; }
        /// <summary>
        /// NodeId of the document being written
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="d"></param>
        /// <param name="fields"></param>
        public DocumentWritingEventArgs(string id, Document d, IDictionary<string, string> fields)
        {
            this.Id = id;
            this.Document = d;
            this.Fields = fields;
        }
    }
}
