using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Examine.LuceneEngine.Providers;
using Lucene.Net.Analysis;

namespace Examine.Test.Stubs
{
    public class TestIndexer : LuceneIndexer
    {

        public TestIndexer(IndexCriteria indexerData, DirectoryInfo workingFolder, Analyzer analyzer, bool async)
            : base (indexerData, workingFolder, analyzer, async)
        {
            
        }
    }
}
