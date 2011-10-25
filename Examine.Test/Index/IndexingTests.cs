using System;
using System.Collections;
using System.Diagnostics;
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
        private readonly Lucene.Net.Store.Directory _luceneDirectory;
        private readonly DirectoryInfo _workingFolder;

        public IndexingTests()
        {
            _workingFolder = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString()));
            _luceneDirectory = new RAMDirectory();
        }

        [TestMethod]
        public void Indexing_Date_Value()
        {
            //arrange

            var indexer = GetIndexer();

            //act

            indexer.PerformIndexing(
                new IndexOperation
                {
                    Item = new IndexItem
                    {
                        Fields = new Dictionary<string, ItemField>
                                    {
                                        {
                                            "Field1", new ItemField(new DateTime(2010, 10, 10, 10, 10, 10))
                                                {
                                                    DataType = FieldDataType.DateTime
                                                }
                                            }
                                    },
                        Id = "test1",
                        ItemCategory = "TestCategory"
                    },
                    Operation = IndexOperationType.Add
                });

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Range("Field1", new DateTime(2010, 10, 10, 10, 10, 10), new DateTime(2010, 10, 10, 10, 10, 10)).Compile());
            var results2 = searcher.Search(searcher.CreateSearchCriteria().Range("Field1", new DateTime(2010, 11, 10, 10, 10, 10), new DateTime(2010, 11, 10, 10, 10, 10)).Compile());

            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(0, results2.Count());
        }

        [TestMethod]
        public void Indexing_Numberical_Value()
        {
            //arrange

            var indexer = GetIndexer();

            //act

            indexer.PerformIndexing(
                new IndexOperation
                    {
                        Item = new IndexItem
                            {
                                Fields = new Dictionary<string, ItemField>
                                    {
                                        {
                                            "Field1", new ItemField(123456)
                                                {
                                                    DataType = FieldDataType.Number
                                                }
                                            }
                                    },
                                Id = "test1",
                                ItemCategory = "TestCategory"
                            },
                        Operation = IndexOperationType.Add
                    });

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Range("Field1", 123456, 123456).Compile());

            Assert.AreEqual(1, results.Count());
        }

        [TestMethod]
        public void Indexing_Item_Deletes()
        {
            //arrange

            var indexer = GetIndexer();
            indexer.PerformIndexing(
                new IndexOperation
                {
                    Item = new IndexItem
                    {
                        Fields = new Dictionary<string, ItemField> { { "Field1", new ItemField("hello world") } },
                        Id = "test1",
                        ItemCategory = "TestCategory"
                    },
                    Operation = IndexOperationType.Add
                });

            //act

            indexer.PerformIndexing(new IndexOperation
                {
                    Operation = IndexOperationType.Delete,
                    Item = new IndexItem
                        {
                            Id = "test1"
                        }
                });

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Id("test1").Compile());

            Assert.AreEqual(0, results.Count());

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
                                Fields = new Dictionary<string, ItemField> { { "Field1", new ItemField("hello world") } },
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

        private void _Indexing_Same_Item_No_Duplicates_Multithreaded_Async(IIndexer indexer)
        {
            for (var i = 0; i < 102; i++)
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
        }

        [TestMethod]
        public void Indexing_Same_Item_No_Duplicates_Multithreaded_Async()
        {
            //arrange

            var indexer = GetAsyncIndexer();
            var totalCount = 0;
            indexer.NodesIndexed += (source, args) =>
            {
                totalCount += args.Nodes.Count();
            };

            //act

            Action doIndex = () => _Indexing_Same_Item_No_Duplicates_Multithreaded_Async(indexer);
            var t1 = new Thread(doIndex.Invoke);
            var t2 = new Thread(doIndex.Invoke);
            var t3 = new Thread(doIndex.Invoke);
            var t4 = new Thread(doIndex.Invoke);
            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            while (t1.IsAlive || t2.IsAlive || t3.IsAlive || t4.IsAlive || totalCount < 102)
            {
                Thread.Sleep(500);
                Debug.WriteLine("WAITING FOR COMPLETION...");
            }

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Id("test").Compile());

            Assert.AreEqual(1, results.TotalItemCount);
        }

        [TestMethod]
        public void Indexing_Same_Item_No_Duplicates_Single_Threaded()
        {
            //arrange

            var indexer = GetIndexer();
            var totalCount = 0;
            indexer.NodesIndexed += (source, args) =>
            {
                totalCount += args.Nodes.Count();
            };

            //act

            //this will index enough times to optimize ... though there shouldn't be duplicates anyways
            for (var i = 0; i < 102; i++)
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
                            Fields = new Dictionary<string, ItemField> { { "Field1", new ItemField("hello world") } },
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
                            Fields = new Dictionary<string, ItemField> { { "Field1", new ItemField("hello world") } },
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
            Assert.AreEqual("TestCategory", results.First().Fields[LuceneIndexer.IndexCategoryFieldName]);
        }

        private void _Indexing_Multiple_Items_Individually_No_Duplicates_Multi_Threaded(IIndexer indexer, int seed)
        {
            for (var i = seed; i < 20 + seed; i++)
            {
                indexer.PerformIndexing(new IndexOperation
                {
                    Item = new IndexItem
                    {
                        Fields = new Dictionary<string, ItemField> { { "Field1", new ItemField("hello world " + i) } },
                        Id = "test" + i,
                        ItemCategory = "TestCategory"
                    },
                    Operation = IndexOperationType.Add
                });
            }

        }

        [TestMethod]
        public void Indexing_Multiple_Items_Individually_No_Duplicates_Multi_Threaded()
        {
            //get an async indexer

            var indexer = GetAsyncIndexer();
            var totalCount = 0;
            indexer.NodesIndexed += (source, args) =>
                {
                    totalCount += args.Nodes.Count();
                };


            Action doIndex1 = () => _Indexing_Multiple_Items_Individually_No_Duplicates_Multi_Threaded(indexer, 0);
            Action doIndex2 = () => _Indexing_Multiple_Items_Individually_No_Duplicates_Multi_Threaded(indexer, 20);
            Action doIndex3 = () => _Indexing_Multiple_Items_Individually_No_Duplicates_Multi_Threaded(indexer, 40);
            Action doIndex4 = () => _Indexing_Multiple_Items_Individually_No_Duplicates_Multi_Threaded(indexer, 60);
            var t1 = new Thread(doIndex1.Invoke);
            var t2 = new Thread(doIndex2.Invoke);
            var t3 = new Thread(doIndex3.Invoke);
            var t4 = new Thread(doIndex4.Invoke);
            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            while (t1.IsAlive || t2.IsAlive || t3.IsAlive || t4.IsAlive || totalCount < 80)
            {
                Thread.Sleep(500);
                Debug.WriteLine("WAITING FOR COMPLETION...");
            }

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello").Compile());

            Assert.AreEqual(80, results.Count());
        }

        private void _Indexing_Multiple_Items_At_Once_No_Duplicates_Multi_Threaded(IIndexer indexer, int seed)
        {
            var toIndex = new List<IndexOperation>();
            for (var i = seed; i < 20 + seed; i++)
            {
                toIndex.Add(new IndexOperation
                {
                    Item = new IndexItem
                    {
                        Fields = new Dictionary<string, ItemField> { { "Field1", new ItemField("hello world " + i) } },
                        Id = "test" + i,
                        ItemCategory = "TestCategory"
                    },
                    Operation = IndexOperationType.Add
                });
            }
            //add them all at once
            indexer.PerformIndexing(toIndex.ToArray());
        }

        [TestMethod]
        public void Indexing_Multiple_Items_At_Once_No_Duplicates_Multi_Threaded()
        {
            //Arrange

            //get an async indexer
            var indexer = GetAsyncIndexer();
            var totalCount = 0;
            indexer.NodesIndexed += (source, args) =>
            {
                totalCount += args.Nodes.Count();
            };

            Action doIndex1 = () => _Indexing_Multiple_Items_At_Once_No_Duplicates_Multi_Threaded(indexer, 0);
            Action doIndex2 = () => _Indexing_Multiple_Items_At_Once_No_Duplicates_Multi_Threaded(indexer, 20);
            Action doIndex3 = () => _Indexing_Multiple_Items_At_Once_No_Duplicates_Multi_Threaded(indexer, 40);
            Action doIndex4 = () => _Indexing_Multiple_Items_At_Once_No_Duplicates_Multi_Threaded(indexer, 60);
            var t1 = new Thread(doIndex1.Invoke);
            var t2 = new Thread(doIndex2.Invoke);
            var t3 = new Thread(doIndex3.Invoke);
            var t4 = new Thread(doIndex4.Invoke);
            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            while (t1.IsAlive || t2.IsAlive || t3.IsAlive || t4.IsAlive || totalCount < 80)
            {
                Thread.Sleep(500);
                Debug.WriteLine("WAITING FOR COMPLETION...");
            }

            //assert

            var searcher = GetSearcher();
            var results = searcher.Search(searcher.CreateSearchCriteria().Field("Field1", "hello").Compile());

            Assert.AreEqual(80, results.Count());
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
                                Fields = new Dictionary<string, ItemField> { { "Field1", new ItemField("hello world " + i) } },
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
                                Fields = new Dictionary<string, ItemField> { { "Field1", new ItemField("hello world " + i) } },
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
                SynchronizationType.Asynchronous,
                _luceneDirectory);
            indexer.IndexingError += (s, e) => Assert.Fail(e.Message);
            return indexer;
        }

        private LuceneIndexer GetIndexer()
        {
            var indexer = new LuceneIndexer(
                _workingFolder,
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29),
                SynchronizationType.Synchronized,
                _luceneDirectory);

            indexer.IndexingError += (s, e) => Assert.Fail(e.Message);

            return indexer;
        }
    }
}
