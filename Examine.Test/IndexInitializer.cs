using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using UmbracoExamine.Config;
using UmbracoExamine;
using Examine.Test.DataServices;
using System.Threading;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Examine.Providers;
using System.Collections.Specialized;
using Examine.LuceneEngine.Config;
using Examine.LuceneEngine;
using Examine.LuceneEngine.Providers;
using Lucene.Net.Search;
using UmbracoExamine.PDF;

namespace Examine.Test
{
    /// <summary>
    /// Used internally by test classes to initialize a new index from the template
    /// </summary>
    internal static class IndexInitializer
    {
        public static UmbracoContentIndexer GetUmbracoIndexer(DirectoryInfo d)
        {
            var i = new UmbracoContentIndexer(new IndexCriteria(
                                                         new[]
                                                             {
                                                                 new TestIndexFieldDefinition { Name = "id", EnableSorting = true, Type = "Number" }, 
                                                                 new TestIndexFieldDefinition { Name = "nodeName", EnableSorting = true },
                                                                 new TestIndexFieldDefinition { Name = "updateDate", EnableSorting = true, Type = "DateTime" }, 
                                                                 new TestIndexFieldDefinition { Name = "writerName" }, 
                                                                 new TestIndexFieldDefinition { Name = "path" }, 
                                                                 new TestIndexFieldDefinition { Name = "nodeTypeAlias" }, 
                                                                 new TestIndexFieldDefinition { Name = "parentID" }
                                                             },
                                                         new[]
                                                             {
                                                                 new TestIndexFieldDefinition { Name = "headerText" }, 
                                                                 new TestIndexFieldDefinition { Name = "bodyText" },
                                                                 new TestIndexFieldDefinition { Name = "metaDescription" }, 
                                                                 new TestIndexFieldDefinition { Name = "metaKeywords" }, 
                                                                 new TestIndexFieldDefinition { Name = "bodyTextColOne" }, 
                                                                 new TestIndexFieldDefinition { Name = "bodyTextColTwo" }, 
                                                                 new TestIndexFieldDefinition { Name = "xmlStorageTest" }
                                                             },
                                                         new[]
                                                             {
                                                                 "CWS_Home", 
                                                                 "CWS_Textpage",
                                                                 "CWS_TextpageTwoCol", 
                                                                 "CWS_NewsEventsList", 
                                                                 "CWS_NewsItem", 
                                                                 "CWS_Gallery", 
                                                                 "CWS_EventItem", 
                                                                 "Image", 
                                                             },
                                                         new string[] { },
                                                         -1),
                                                         d,
                                                         new TestDataService(),
                                                         new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29),
                                                         false);

            i.IndexSecondsInterval = 1;

            i.IndexingError += IndexingError;

            return i;
        } 
        public static UmbracoExamineSearcher GetUmbracoSearcher(DirectoryInfo d)
        {
            return new UmbracoExamineSearcher(d, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29));
        }
        public static SimpleDataIndexer GetSimpleIndexer(DirectoryInfo d)
        {
            var i = new SimpleDataIndexer(new IndexCriteria(
                                                         new IIndexField[] { },
                                                         new[]
                                                             {
                                                                 new TestIndexFieldDefinition { Name = "Author" }, 
                                                                 new TestIndexFieldDefinition { Name = "DateCreated", EnableSorting = true, Type = "DateTime"  },
                                                                 new TestIndexFieldDefinition { Name = "Title" }, 
                                                                 new TestIndexFieldDefinition { Name = "Photographer" }, 
                                                                 new TestIndexFieldDefinition { Name = "YearCreated", Type = "Date.Year" }, 
                                                                 new TestIndexFieldDefinition { Name = "MonthCreated", Type = "Date.Month" }, 
                                                                 new TestIndexFieldDefinition { Name = "DayCreated", Type = "Date.Day" },
                                                                 new TestIndexFieldDefinition { Name = "HourCreated", Type = "Date.Hour" },
                                                                 new TestIndexFieldDefinition { Name = "MinuteCreated", Type = "Date.Minute" },
                                                                 new TestIndexFieldDefinition { Name = "SomeNumber", Type = "Number" },
                                                                 new TestIndexFieldDefinition { Name = "SomeFloat", Type = "Float" },
                                                                 new TestIndexFieldDefinition { Name = "SomeDouble", Type = "Double" },
                                                                 new TestIndexFieldDefinition { Name = "SomeLong", Type = "Long" }
                                                             },
                                                         new string[] { },
                                                         new string[] { },
                                                         -1),
                                                         d,
                                                         new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29),
                                                         new TestSimpleDataProvider(),
                                                         new[] { "Documents", "Pictures" }, 
                                                         false);
            i.IndexingError += IndexingError;

            return i;
        }
        public static LuceneSearcher GetLuceneSearcher(DirectoryInfo d)
        {
            return new LuceneSearcher(d, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29));
        }
        public static PDFIndexer GetPdfIndexer(DirectoryInfo d)
        {
            var i = new PDFIndexer(d,
                                      new TestDataService(),
                                      new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29),
                                      false);

            i.IndexingError += IndexingError;

            return i;
        }
        public static MultiIndexSearcher GetMultiSearcher(DirectoryInfo pdfDir, DirectoryInfo simpleDir, DirectoryInfo conventionDir, DirectoryInfo cwsDir)
        {
            var i = new MultiIndexSearcher(new[] { pdfDir, simpleDir, conventionDir, cwsDir }, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29));
            return i;
        }

        
        internal static void IndexingError(object sender, IndexingErrorEventArgs e)
        {
            throw new ApplicationException(e.Message, e.InnerException);
        }

     
    }
}
