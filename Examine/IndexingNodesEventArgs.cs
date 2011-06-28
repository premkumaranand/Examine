using System.ComponentModel;

namespace Examine
{
    public class IndexingNodesEventArgs : CancelEventArgs
    {

        public IndexingNodesEventArgs(IndexCriteria indexData, string xPath, string type)
        {
            this.IndexData = indexData;
            this.XPath = xPath;
            this.Type = type;
        }

        public IndexCriteria IndexData { get; private set; }
        public string XPath { get; private set; }
        public string Type {get; private set;}

    }
}