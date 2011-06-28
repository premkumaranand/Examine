using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Examine.LuceneEngine.Config;
using Examine.LuceneEngine.Providers;

using Lucene.Net.Analysis.Standard;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Examine.Test.Index
{
    [TestClass]
    public class IndexingTests
    {
        private DirectoryInfo _currentFolder;

        [TestMethod]
        public void Indexing_Item_Indexed()
        {
            //arrange

            var indexer = GetIndexer(new[] {new IndexFieldDefinition {Name = "Field1"}});

            //act

            indexer.ReIndexNodes("TestCategory", new IndexItem
                {
                    Fields = new Dictionary<string, string> {{"Field1", "hello world"}},
                    Id = "test1",
                    ItemType = "test"
                });

            //assert

            var searcher = GetSearcher(_currentFolder);
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello world").Compile());

            Assert.AreEqual(1, results.Count());

        }

        [TestMethod]
        public void Indexing_Special_Fields_Indexed()
        {
            //arrange

            var indexer = GetIndexer(new[] { new IndexFieldDefinition { Name = "Field1" } });

            //act

            indexer.ReIndexNodes("TestCategory", new IndexItem
            {
                Fields = new Dictionary<string, string> { { "Field1", "hello world" } },
                Id = "test1",
                ItemType = "test"
            });

            //assert

            var searcher = GetSearcher(_currentFolder);
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello world").Compile());

            Assert.AreEqual(3, results.First().Fields.Count());
            Assert.AreEqual("test1", results.First().Fields[LuceneIndexer.IndexNodeIdFieldName]);
            Assert.AreEqual("testcategory", results.First().Fields[LuceneIndexer.IndexCategoryFieldName]);
        }

        [TestMethod]
        public void Indexing_Background_Worker_Indexes_Many_Individually()
        {
            //get an async indexer

            _currentFolder = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString()));
            _currentFolder.Create();
            var indexer = new LuceneIndexer(
                new IndexCriteria(new[] { new IndexFieldDefinition { Name = "Field1" } }, null, null),
                _currentFolder,
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29), SynchronizationType.AsyncBackgroundWorker);
            
            for(var i = 0;i<20;i++)
            {
                indexer.ReIndexNodes("TestCategory", new IndexItem
                {
                    Fields = new Dictionary<string, string> { { "Field1", "hello world " + i } },
                    Id = "test" + i,
                    ItemType = "test"
                });
            }

            while(indexer.IsBusy)
            {
                Thread.Sleep(1000);
            }
            
            //assert

            var searcher = GetSearcher(_currentFolder);
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello").Compile());

            Assert.AreEqual(20, results.Count());
        }

        [TestMethod]
        public void Indexing_Background_Worker_Indexes_Many_At_Once()
        {
            //Arrange

            //get an async indexer
            _currentFolder = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString()));
            _currentFolder.Create();
            var indexer = new LuceneIndexer(
                new IndexCriteria(new[] { new IndexFieldDefinition { Name = "Field1" } }, null, null),
                _currentFolder,
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29), SynchronizationType.AsyncBackgroundWorker);
            var toIndex = new List<IndexItem>();

            for(var i = 0;i<20;i++)
            {
                toIndex.Add(new IndexItem
                    {
                        Fields = new Dictionary<string, string> {{"Field1", "hello world " + i}},
                        Id = "test" + i,
                        ItemType = "test"
                    });
            }
            
            //ACT

            //add them all at once
            indexer.ReIndexNodes("TestCategory", toIndex.ToArray());

            while(indexer.IsBusy)
            {
                Thread.Sleep(1000);
            }
            
            //assert

            var searcher = GetSearcher(_currentFolder);
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello").Compile());

            Assert.AreEqual(20, results.Count());
        }

        /// <summary>
        /// This tests that the async thread operation is able to run again after it had already completed once
        /// </summary>
        [TestMethod]
        public void Indexing_Background_Worker_Indexes_Many_Waits_Then_Indexes_More()
        {
            //get an async indexer

            _currentFolder = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString()));
            _currentFolder.Create();
            var indexer = new LuceneIndexer(
                new IndexCriteria(new[] { new IndexFieldDefinition { Name = "Field1" } }, null, null),
                _currentFolder,
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29), SynchronizationType.AsyncBackgroundWorker);

            for (var i = 0; i < 5; i++)
            {
                indexer.ReIndexNodes("TestCategory", new IndexItem
                {
                    Fields = new Dictionary<string, string> { { "Field1", "hello world " + i } },
                    Id = "test" + i,
                    ItemType = "test"
                });
            }
            while (indexer.IsBusy)
            {
                Thread.Sleep(1000);
            }
            
            var searcher = GetSearcher(_currentFolder);
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello").Compile());
            Assert.AreEqual(5, results.Count());

            //now we want to re-index again 
            Thread.Sleep(2000);

            for (var i = 5; i < 10; i++)
            {
                indexer.ReIndexNodes("TestCategory", new IndexItem
                {
                    Fields = new Dictionary<string, string> { { "Field1", "hello world " + i } },
                    Id = "test" + i,
                    ItemType = "test"
                });
            }
            while (indexer.IsBusy)
            {
                Thread.Sleep(1000);
            }

            results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello").Compile());
            Assert.AreEqual(10, results.Count());
        }

        private ISearcher GetSearcher(DirectoryInfo folder)
        {
            var searcher = new LuceneSearcher(folder, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29));
            return searcher;
        }

        private IIndexer GetIndexer(IEnumerable<IndexFieldDefinition> fields)
        {
            _currentFolder = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString()));
            _currentFolder.Create();
            var indexer = new LuceneIndexer(
                new IndexCriteria(fields, null, null),
                _currentFolder,
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29), SynchronizationType.SingleThreaded);
            return indexer;
        }
    }
}
