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
using Lucene.Net.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Examine.Test.Index
{
    [TestClass]
    public class IndexingTests
    {
        private Lucene.Net.Store.Directory _luceneDirectory;
        private DirectoryInfo _workingFolder;

        public IndexingTests()
        {
            _workingFolder = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString()));
            _luceneDirectory = new RAMDirectory();
        }

        [TestMethod]
        public void Indexing_Item_Indexed()
        {
            //arrange

            var indexer = GetIndexer();

            //act

            indexer.PerformIndexing(
                new IndexOperation
                    {
                        Item = new IndexItem
                            {
                                Fields = new Dictionary<string, ItemField> {{"Field1", new ItemField("hello world")}},
                                Id = "test1",
                                ItemCategory = "TestCategory"
                            },
                        Operation = IndexOperationType.Add
                    });

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello world").Compile());

            Assert.AreEqual(1, results.Count());

        }

        [TestMethod]
        public void Indexing_Deduplicates()
        {
            //arrange

            var indexer = GetAsyncIndexer();
            var totalCount = 0;
            indexer.NodesIndexed += (source, args) =>
            {
                totalCount += args.Nodes.Count();
            };

            //act

            //this will index enough times to optimize which will run a dedupe process... though there shouldn't be duplicates anyways
            for(var i = 0;i<102;i++)
            {
                var op = new IndexOperation
                {
                    Item = new IndexItem
                    {
                        Fields = new Dictionary<string, ItemField> { { "Field1", new ItemField("value" + i) } },
                        Id = "test",
                        ItemCategory = "TestCategory"
                    },
                    Operation = IndexOperationType.Add
                };
                indexer.PerformIndexing(op);
            }

            while (totalCount < 102)
            {
                Thread.Sleep(1000);
            }


            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Id("test").Compile());

            Assert.AreEqual(1, results.TotalItemCount);
        }

        [TestMethod]
        public void Indexing_Doesnt_Index_Null_Field_Values()
        {
            //arrange

            var indexer = GetIndexer();

            //act

            var op = new IndexOperation
            {
                Item = new IndexItem
                {
                    Fields = new Dictionary<string, ItemField> { { "Field1", new ItemField("") } },
                    Id = "test1",
                    ItemCategory = "TestCategory"
                },
                Operation = IndexOperationType.Add
            };
            indexer.PerformIndexing(op);

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Id("test1").Compile());

            Assert.IsFalse(results.First().Fields.ContainsKey("Field1"));
        }

        [TestMethod]
        public void Indexing_Same_Id_Multiple_Times_Yields_One_Entry()
        {
            //arrange

            var indexer = GetIndexer();

            //act

            var op = new IndexOperation
                {
                    Item = new IndexItem
                        {
                            Fields = new Dictionary<string, ItemField> {{"Field1", new ItemField("hello world")}},
                            Id = "test1",
                            ItemCategory = "TestCategory"
                        },
                    Operation = IndexOperationType.Add
                };
            indexer.PerformIndexing(op, op, op, op, op, op, op);

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Id("test1").Compile());

            Assert.AreEqual(1, results.TotalItemCount);            
        }

        [TestMethod]
        public void Indexing_Special_Fields_Indexed()
        {
            //arrange

            var indexer = GetIndexer();

            //act

            indexer.PerformIndexing(new IndexOperation
                {
                    Item = new IndexItem
                        {
                            Fields = new Dictionary<string, ItemField> {{"Field1", new ItemField("hello world")}},
                            Id = "test1",
                            ItemCategory = "TestCategory"
                        },
                    Operation = IndexOperationType.Add
                });

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello world").Compile());

            Assert.AreEqual(3, results.First().Fields.Count());
            Assert.AreEqual("test1", results.First().Fields[LuceneIndexer.IndexNodeIdFieldName]);
            Assert.AreEqual("testcategory", results.First().Fields[LuceneIndexer.IndexCategoryFieldName]);
        }

        [TestMethod]
        public void Indexing_Background_Worker_Indexes_Many_Individually()
        {
            //get an async indexer

            var indexer = GetAsyncIndexer();
            var totalCount = 0;
            indexer.NodesIndexed += (source, args) =>
                {
                    totalCount += args.Nodes.Count();
                };

            for (var i = 0; i < 20; i++)
            {
                indexer.PerformIndexing(new IndexOperation
                    {
                        Item = new IndexItem
                            {
                                Fields = new Dictionary<string, ItemField> {{"Field1", new ItemField("hello world " + i)}},
                                Id = "test" + i,
                                ItemCategory = "TestCategory"
                            },
                        Operation = IndexOperationType.Add
                    });
            }

            while (totalCount < 20)
            {
                Thread.Sleep(1000);
            }

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello").Compile());

            Assert.AreEqual(20, results.Count());
        }

        [TestMethod]
        public void Indexing_Background_Worker_Indexes_Many_At_Once()
        {
            //Arrange

            //get an async indexer
            var indexer = GetAsyncIndexer();
            var totalCount = 0;
            indexer.NodesIndexed += (source, args) =>
            {
                totalCount += args.Nodes.Count();
            };

            var toIndex = new List<IndexOperation>();

            for (var i = 0; i < 20; i++)
            {
                toIndex.Add(new IndexOperation
                    {
                        Item = new IndexItem
                            {
                                Fields = new Dictionary<string, ItemField> {{"Field1", new ItemField("hello world " + i)}},
                                Id = "test" + i,
                                ItemCategory = "TestCategory"
                            },
                        Operation = IndexOperationType.Add
                    });
            }

            //ACT

            //add them all at once
            indexer.PerformIndexing(toIndex.ToArray());

            while (totalCount < 20)
            {
                Thread.Sleep(1000);
            }

            //assert

            var searcher = GetSearcher();
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

            var indexer = GetAsyncIndexer();
            var totalCount = 0;
            indexer.NodesIndexed += (source, args) =>
            {
                totalCount += args.Nodes.Count();
            };

            for (var i = 0; i < 5; i++)
            {
                indexer.PerformIndexing(new IndexOperation
                    {
                        Item = new IndexItem
                            {
                                Fields = new Dictionary<string, ItemField> {{"Field1", new ItemField("hello world " + i)}},
                                Id = "test" + i,
                                ItemCategory = "TestCategory"
                            },
                        Operation = IndexOperationType.Add
                    });
            }
            while (totalCount < 5)
            {
                Thread.Sleep(1000);
            }
            totalCount = 0;

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello").Compile());
            Assert.AreEqual(5, results.Count());

            //now we want to re-index again 
            Thread.Sleep(2000);

            for (var i = 5; i < 10; i++)
            {
                indexer.PerformIndexing(new IndexOperation
                    {
                        Item = new IndexItem
                            {
                                Fields = new Dictionary<string, ItemField> {{"Field1", new ItemField("hello world " + i)}},
                                Id = "test" + i,
                                ItemCategory = "TestCategory"
                            },
                        Operation = IndexOperationType.Add
                    });
            }
            while (totalCount < 5)
            {
                Thread.Sleep(1000);
            }

            results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello").Compile());
            Assert.AreEqual(10, results.Count());
        }

        private ISearcher GetSearcher()
        {
            var searcher = new LuceneSearcher(new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29), _luceneDirectory);
            return searcher;
        }

        private LuceneIndexer GetAsyncIndexer()
        {
            var indexer = new LuceneIndexer(
                _workingFolder,
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29),
                SynchronizationType.AsyncBackgroundWorker,
                _luceneDirectory);
            indexer.IndexingError += (s, e) => Assert.Fail(e.Message);
            return indexer;
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
    }
}
