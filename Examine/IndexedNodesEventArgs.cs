using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Examine
{
    public class IndexedNodesEventArgs : EventArgs
    {

        public IndexedNodesEventArgs(IEnumerable<IndexItem> nodes)
        {
            this.Nodes = nodes;
        }

        public IEnumerable<IndexItem> Nodes { get; private set; }

    }
}