using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Examine.LuceneEngine.SearchCriteria;
using Examine.SearchCriteria;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lucene.Net.Search;
using Lucene.Net.Index;
using System.Diagnostics;
using Examine.LuceneEngine;
using Examine.LuceneEngine.Providers;
using System.Threading;
using Directory = System.IO.Directory;

namespace Examine.Test.Search
{
    [TestClass]
    public class FluentApiTests
    {

        private readonly Lucene.Net.Store.Directory _luceneDirectory;
        private readonly DirectoryInfo _workingFolder;

        public FluentApiTests()
        {
            _workingFolder = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "Examine", Guid.NewGuid().ToString()));
            _workingFolder.Create();

            //clear out old folders
            var parentFolder = _workingFolder.Parent;
            foreach (var f in parentFolder.GetDirectories())
            {
                try
                {
                    Directory.Delete(f.FullName, true);
                }
                catch (IOException)
                {
                    //ignore
                }
            }

            var assemblyFolder = new DirectoryInfo(TestHelper.AssemblyDirectory);
            var testIndexFolder = assemblyFolder.Parent.Parent.GetDirectories("App_Data").First().GetDirectories("TemplateIndex").First();
            _luceneDirectory = new RAMDirectory(new SimpleFSDirectory(testIndexFolder));
        }

        private LuceneSearcher GetSearcher()
        {
            var searcher = new LuceneSearcher(new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29), _luceneDirectory);
            return searcher;
        }
      
        private LuceneIndexer GetIndexer()
        {
            var indexer = new LuceneIndexer(
                _workingFolder,
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29),
                SynchronizationType.SingleThreaded,
                _luceneDirectory);

            indexer.IndexingError += (s, e) => Assert.Fail(e.Message);

            return indexer;
        }

        [TestMethod]
        public void FluentApi_Search_With_Stop_Words()
        {
            var searcher = GetSearcher();
            var criteria = searcher.CreateSearchCriteria();
            var filter = criteria.Field("nodeName", "into")
                .Or().Field("nodeTypeAlias", "into");

            var results = searcher.Search(filter.Compile());

            Assert.AreEqual(0, results.TotalItemCount);
        }

        [TestMethod]
        public void FluentApi_Search_Raw_Query()
        {
            var searcher = GetSearcher();
            var criteria = searcher.CreateSearchCriteria();            

            var filter = criteria.RawQuery("nodeTypeAlias:cws_home");
            Console.WriteLine(filter.ToString());
            var results = searcher.Search(filter);

            Assert.IsTrue(results.TotalItemCount > 0);
        }

        [TestMethod]
        public void FluentApi_Find_By_Field()
        {
            var searcher = GetSearcher();
            //var criteria = searcher.CreateSearchCriteria("content");
            //NOTE: we manually construct this so we can set a custom category field so that it works with our test index
            var criteria = new LuceneSearchCriteria(
                "content",
                searcher.IndexingAnalyzer,
                searcher.GetSearchFields(),
                false,
                BooleanOperation.And,
                "__IndexType");

            var filter = criteria.Field("nodeTypeAlias", "cws_home".Escape()).Compile();
            Console.WriteLine(filter.ToString());
            var results = searcher.Search(filter);

            Assert.IsTrue(results.TotalItemCount > 0);
        }

        [TestMethod]
        public void FluentApi_Sort_Result_By_Single_Field()
        {
            var searcher = GetSearcher();
            var sc = searcher.CreateSearchCriteria();
            var sc1 = sc.Field("writerName", "administrator").And().OrderBy("nodeName").Compile();

            sc = searcher.CreateSearchCriteria();
            var sc2 = sc.Field("writerName", "administrator").And().OrderByDescending("nodeName").Compile();

            Console.WriteLine(sc1.ToString());
            var results1 = searcher.Search(sc1);
            Console.WriteLine(sc2.ToString());
            var results2 = searcher.Search(sc2);

            Assert.AreNotEqual(results1.First().Id, results2.First().Id);
        }

        [TestMethod]
        public void FluentApi_Standard_Results_Sorted_By_Score()
        {
            var searcher = GetSearcher();

            //Arrange
            var sc = searcher.CreateSearchCriteria(SearchCriteria.BooleanOperation.Or);
            sc = sc.Field("nodeName", "umbraco").Or().Field("headerText", "umbraco").Or().Field("bodyText", "umbraco").Compile();

            //Act
            var results = searcher.Search(sc);

            //Assert
            for (int i = 0; i < results.TotalItemCount - 1; i++)
            {
                var curr = results.ElementAt(i);
                var next = results.ElementAtOrDefault(i + 1);

                if (next == null)
                    break;

                Assert.IsTrue(curr.Score >= next.Score, string.Format("Result at index {0} must have a higher score than result at index {1}", i, i + 1));
            }
        }

        [TestMethod]
        public void FluentApi_Skip_Results_Returns_Different_Results()
        {
            var searcher = GetSearcher();

            //Arrange
            var sc = searcher.CreateSearchCriteria();
            sc = sc.Field("writerName", "administrator").Compile();

            //Act
            var results = searcher.Search(sc);

            //Assert
            Assert.AreNotEqual(results.First(), results.Skip(2).First(), "Third result should be different");
        }

        [TestMethod]
        public void FluentApiTests_Escaping_Includes_All_Words()
        {
            var searcher = GetSearcher();

            //Arrange
            var sc = searcher.CreateSearchCriteria();
            var op = sc.Field("nodeName", "codegarden 09".Escape());
            sc = op.Compile();

            //Act
            var results = searcher.Search(sc);

            //Assert
            Assert.IsTrue(results.TotalItemCount > 0);
        }

        [TestMethod]
        public void FluentApiTests_Grouped_And_Examiness()
        {
            var searcher = GetSearcher();

            ////Arrange
            var criteria = searcher.CreateSearchCriteria();

            //get all node type aliases starting with CWS and all nodees starting with "A"
            var filter = criteria.GroupedAnd(
                new string[] { "nodeTypeAlias", "nodeName" },
                new IExamineValue[] { "CWS".MultipleCharacterWildcard(), "A".MultipleCharacterWildcard() })
                .Compile();


            ////Act
            var results = searcher.Search(filter);

            ////Assert
            Assert.IsTrue(results.TotalItemCount > 0);
        }

        [TestMethod]
        public void FluentApiTests_Examiness_Proximity()
        {
            var searcher = GetSearcher();

            ////Arrange
            var criteria = searcher.CreateSearchCriteria();

            //get all nodes that contain the words warren and creative within 5 words of each other
            var filter = criteria.Field("metaKeywords", "Warren creative".Proximity(5)).Compile();

            ////Act
            var results = searcher.Search(filter);

            ////Assert
            Assert.IsTrue(results.TotalItemCount > 0);
        }

        [TestMethod]
        public void FluentApiTests_Grouped_Or_Examiness()
        {
            var searcher = GetSearcher();
          
            ////Arrange
            var criteria = searcher.CreateSearchCriteria();

            //get all node type aliases starting with CWS_Home OR and all nodees starting with "About"
            var filter = criteria.GroupedOr(
                new[] { "nodeTypeAlias", "nodeName" },
                new[] { "CWS\\_Home".Boost(10), "About".MultipleCharacterWildcard() })
                .Compile();


            ////Act
            var results = searcher.Search(filter);

            ////Assert
            Assert.IsTrue(results.TotalItemCount > 0);
        }

    }
}
