using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Amib.Threading;
using Examine;
using Examine.Providers;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Examine.LuceneEngine.Config;
using Lucene.Net.Util;
using System.Xml;
using Directory = Lucene.Net.Store.Directory;

namespace Examine.LuceneEngine.Providers
{
    ///<summary>
    /// Abstract object containing all of the logic used to use Lucene as an indexer
    ///</summary>
    public class LuceneIndexer : BaseIndexProvider, IDisposable
    {
        #region Constructors

        /// <summary>
        /// Default constructor for use with provider implementation
        /// </summary>
        public LuceneIndexer()
            : this(IndexSets.GetDefaultInstance())
        {
        }

        /// <summary>
        /// Default constructor for use with provider implementation
        /// </summary>
        /// <param name="indexSetConfig">
        /// All index sets specified to be queried against in order to setup the indexer
        /// </param>
        /// <remarks>>
        /// Once constructed, a call must be made to Initialize
        /// </remarks>
        public LuceneIndexer(IEnumerable<IndexSet> indexSetConfig)
        {
            //_workerThreadDoWorkEventHandler = new ThreadStart(WorkerThreadDoWork);

            OptimizationCommitThreshold = 100;
            _indexSetConfiguration = indexSetConfig;
        }

        /// <summary>
        /// Constructor to allow for creating an indexer at runtime which uses the Lucene SimpleFSDirectory
        /// </summary>
        /// <param name="indexerData"></param>
        /// <param name="workingFolder"></param>
        /// <param name="analyzer"></param>
        /// <param name="synchronizationType"></param>
        public LuceneIndexer(IndexCriteria indexerData, DirectoryInfo workingFolder, Analyzer analyzer, SynchronizationType synchronizationType)
            : base(indexerData)
        {
            //_workerThreadDoWorkEventHandler = new ThreadStart(WorkerThreadDoWork);

            //set up our folders based on the index path
            WorkingFolder = workingFolder;
            

            IndexingAnalyzer = analyzer;

            //create our internal searcher, this is useful for inheritors to be able to search their own indexes inside of their indexer
            InternalSearcher = new LuceneSearcher(WorkingFolder, IndexingAnalyzer);

            OptimizationCommitThreshold = 100;
            SynchronizationType = synchronizationType;

            var luceneIndexFolder = new DirectoryInfo(Path.Combine(workingFolder.FullName, "Index"));
            VerifyFolder(luceneIndexFolder);
            LuceneDirectory = new SimpleFSDirectory(luceneIndexFolder);

            ReInitialize();
        }

        public LuceneIndexer(IndexCriteria indexerData, DirectoryInfo workingFolder, Analyzer analyzer, SynchronizationType synchronizationType, Directory luceneDirectory)
            : base(indexerData)
        {
            //_workerThreadDoWorkEventHandler = new ThreadStart(WorkerThreadDoWork);

            //set up our folders based on the index path
            WorkingFolder = workingFolder;
            
            IndexingAnalyzer = analyzer;

            //create our internal searcher, this is useful for inheritors to be able to search their own indexes inside of their indexer
            InternalSearcher = new LuceneSearcher(WorkingFolder, IndexingAnalyzer);

            OptimizationCommitThreshold = 100;
            SynchronizationType = synchronizationType;
            LuceneDirectory = luceneDirectory;

            ReInitialize();
        }

        #endregion

        /// <summary>
        /// This is our threadsafe queue of items which can be read by our background worker to process the queue
        /// </summary>
        private readonly ConcurrentQueue<IndexOperation> _asyncQueue = new ConcurrentQueue<IndexOperation>();

        #region Initialize

        /// <summary>
        /// Set up all properties for the indexer based on configuration information specified. This will ensure that
        /// all of the folders required by the indexer are created and exist. This will also create an instruction
        /// file declaring the computer name that is part taking in the indexing. This file will then be used to
        /// determine the master indexer machine in a load balanced environment (if one exists).
        /// </summary>
        /// <param name="name">The friendly name of the provider.</param>
        /// <param name="config">A collection of the name/value pairs representing the provider-specific attributes specified in the configuration for this provider.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The name of the provider is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The name of the provider has a length of zero.
        /// </exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// An attempt is made to call <see cref="M:System.Configuration.Provider.ProviderBase.Initialize(System.String,System.Collections.Specialized.NameValueCollection)"/> on a provider after the provider has already been initialized.
        /// </exception>
        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);

            //Need to check if the index set or IndexerData is specified...

            DirectoryInfo luceneIndexFolder = null;

            if (config["indexSet"] == null)
            {
                //if we don't have either, then we'll try to set the index set by naming conventions
                var found = false;
                if (name.EndsWith("Indexer"))
                {
                    var setNameByConvension = name.Remove(name.LastIndexOf("Indexer")) + "IndexSet";
                    //check if we can assign the index set by naming convention
                    var set = _indexSetConfiguration.SingleOrDefault(x => x.SetName == setNameByConvension);

                    if (set != null)
                    {
                        //we've found an index set by naming conventions :)
                        IndexSetName = set.SetName;

                        //get the index criteria and ensure folder
                        IndexerData = GetIndexerData(set);
                        VerifyFolder(set.IndexDirectory);

                        //now set the index folders
                        WorkingFolder = set.IndexDirectory;
                        luceneIndexFolder = new DirectoryInfo(Path.Combine(set.IndexDirectory.FullName, "Index"));

                        found = true;
                    }
                }

                if (!found)
                    throw new ArgumentNullException("indexSet on LuceneExamineIndexer provider has not been set in configuration and/or the IndexerData property has not been explicitly set");

            }
            else
            {
                //if an index set is specified, ensure it exists and initialize the indexer based on the set
                var set = _indexSetConfiguration.SingleOrDefault(x => x.SetName == config["indexSet"]);
                if (set == null)
                {
                    throw new ArgumentException("The indexSet specified for the LuceneExamineIndexer provider does not exist");
                }

                IndexSetName = config["indexSet"];

                //get the index criteria and ensure folder
                IndexerData = GetIndexerData(set);
                VerifyFolder(set.IndexDirectory);

                //now set the index folders
                WorkingFolder = set.IndexDirectory;
                luceneIndexFolder = new DirectoryInfo(Path.Combine(set.IndexDirectory.FullName, "Index"));
            }

            if (config["analyzer"] != null)
            {
                //this should be a fully qualified type
                var analyzerType = Type.GetType(config["analyzer"]);
                IndexingAnalyzer = (Analyzer)Activator.CreateInstance(analyzerType);
            }
            else
            {
                IndexingAnalyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);
            }

            VerifyFolder(luceneIndexFolder);

            //set the Lucene 'Directory' to SimpleFS
            LuceneDirectory = new SimpleFSDirectory(luceneIndexFolder);

            //create our internal searcher, this is useful for inheritors to be able to search their own indexes inside of their indexer
            InternalSearcher = new LuceneSearcher(WorkingFolder, IndexingAnalyzer);

            SynchronizationType = SynchronizationType.AsyncBackgroundWorker;
            if (config["synchronizationType"] != null)
            {
                SynchronizationType = (SynchronizationType)Enum.Parse(typeof(SynchronizationType), config["synchronizationType"]);
            }

            ReInitialize();

            CommitCount = 0;

        }

        #endregion

        #region Constants & Fields

        /// <summary>
        /// Gets or sets the index set configuration, used for the provider implementation
        /// </summary>
        /// <value>
        /// The index set configuration.
        /// </value>
        private readonly IEnumerable<IndexSet> _indexSetConfiguration;

        /// <summary>
        /// The prefix characters denoting a special field stored in the lucene index for use internally
        /// </summary>
        public const string SpecialFieldPrefix = "__";

        /// <summary>
        /// The prefix added to a field when it is included in the index for sorting
        /// </summary>
        public const string SortedFieldNamePrefix = SpecialFieldPrefix + "Sort_";

        /// <summary>
        /// Specifies how many index commits are performed before running an optimization
        /// </summary>
        public int OptimizationCommitThreshold { get; internal set; }

        /// <summary>
        /// Used to store a non-tokenized category for the document
        /// </summary>
        public const string IndexCategoryFieldName = SpecialFieldPrefix + "IndexCategory";

        /// <summary>
        /// Used to store a non-tokenized type for the document
        /// </summary>
        public const string IndexNodeIdFieldName = SpecialFieldPrefix + "NodeId";

        /// <summary>
        /// Used to perform thread locking
        /// </summary>
        private readonly object _indexerLocker = new object();

        /// <summary>
        /// used to thread lock calls for creating and verifying folders
        /// </summary>
        private readonly object _folderLocker = new object();

        /// <summary>
        /// Used for double check locking during an index operation
        /// </summary>
        private bool _isIndexing = false;

        private readonly object _cancelLocker = new object();
        /// <summary>
        /// Used to cancel the async operation
        /// </summary>
        private bool _isCancelling = false;

        //private readonly object _shouldProcessLock = new object();
        ///// <summary>
        ///// Used to notify the worker thread to run an iteration
        ///// </summary>
        //private bool _shouldProcess = false;

        ///// <summary>
        ///// Used to run the indexing async
        ///// </summary>
        private static SmartThreadPool _threadPool;
        private static readonly object CreateThreadPoolLock = new object();

        //private readonly ThreadStart _workerThreadDoWorkEventHandler;
        //private Thread _workerThread;

        /// <summary>
        /// We need an internal searcher used to search against our own index.
        /// This is used for finding all descendant nodes of a current node when deleting indexes.
        /// </summary>
        protected BaseSearchProvider InternalSearcher { get; private set; }

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if the thread is alive or if the indexing flag is still true
        /// </summary>
        protected internal bool IsBusy
        {
            get
            {
                return _isIndexing;
                //return _workerThread.IsAlive || _isIndexing;
                //return !_threadPool.IsIdle || _isIndexing;
            }
        }
        
        /// <summary>
        /// The analyzer to use when indexing content, by default, this is set to StandardAnalyzer
        /// </summary>
        public Analyzer IndexingAnalyzer { get; protected set; }

        /// <summary>
        /// Used to keep track of how many index commits have been performed.
        /// This is used to determine when index optimization needs to occur.
        /// </summary>
        public int CommitCount { get; protected internal set; }

        /// <summary>
        /// Indicates whether or this system will process the queue items asynchonously. Default is true.
        /// </summary>
        public SynchronizationType SynchronizationType { get; protected internal set; }

        /// <summary>
        /// The Lucene 'Directory' of where the index is stored
        /// </summary>
        public Directory LuceneDirectory { get; protected set; }

        /// <summary>
        /// The base folder that contains the queue and index folder and the indexer executive files
        /// </summary>
        public DirectoryInfo WorkingFolder { get; private set; }

        /// <summary>
        /// The Executive to determine if this is the master indexer
        /// </summary>
        protected IndexerExecutive ExecutiveIndex { get; set; }

        /// <summary>
        /// The index set name which references an Examine <see cref="IndexSet"/>
        /// </summary>
        public string IndexSetName { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [index optimizing].
        /// </summary>
        public event EventHandler IndexOptimizing;

        ///<summary>
        /// Occurs when the index is finished optmizing
        ///</summary>
        public event EventHandler IndexOptimized;

        /// <summary>
        /// Occurs when [document writing].
        /// </summary>
        public event EventHandler<DocumentWritingEventArgs> DocumentWriting;

        /// <summary>
        /// An event that is triggered when this machine has been elected as the IndexerExecutive
        /// </summary>
        public event EventHandler<IndexerExecutiveAssignedEventArgs> IndexerExecutiveAssigned;

        #endregion

        #region Event handlers

        protected virtual void OnIndexerExecutiveAssigned(IndexerExecutiveAssignedEventArgs e)
        {
            if (IndexerExecutiveAssigned != null)
                IndexerExecutiveAssigned(this, e);
        }

        /// <summary>
        /// Called when an indexing error occurs
        /// </summary>
        /// <param name="e"></param>
        /// <param name="resetIndexingFlag">set to true if the IsIndexing flag should be reset (set to false) so future indexing operations can occur</param>
        protected void OnIndexingError(IndexingErrorEventArgs e, bool resetIndexingFlag)
        {
            if (resetIndexingFlag)
            {
                if (_isIndexing)
                {
                    lock(_indexerLocker)
                    {
                        if(_isIndexing)
                        {
                            //reset our  flag... something else funny is going on but we don't want this to prevent ALL future operations
                            _isIndexing = false;            
                        }
                    }
                }
            }

            OnIndexingError(e);
        }

        /// <summary>
        /// Called when an indexing error occurs
        /// </summary>
        /// <param name="e"></param>
        protected override void OnIndexingError(IndexingErrorEventArgs e)
        {
            base.OnIndexingError(e);

            if (SynchronizationType == SynchronizationType.SingleThreaded)
            {
                throw new Exception("Indexing Error Occurred: " + e.Message, e.InnerException);
            }

        }

        protected virtual void OnDocumentWriting(DocumentWritingEventArgs docArgs)
        {
            if (DocumentWriting != null)
                DocumentWriting(this, docArgs);
        }

        protected virtual void OnIndexOptimizing(EventArgs e)
        {
            if (IndexOptimizing != null)
                IndexOptimizing(this, e);
        }

        protected virtual void OnIndexOptimized(EventArgs e)
        {
            if (IndexOptimized != null)
                IndexOptimized(this, e);
        }

        /// <summary>
        /// This is here for inheritors to deal with if there's a duplicate entry in the fields dictionary when trying to index.
        /// The system by default just ignores duplicates but this will give inheritors a chance to do something about it (i.e. logging, alerting...)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="indexSetName"></param>
        /// <param name="fieldName"></param>
        protected virtual void OnDuplicateFieldWarning(string id, string indexSetName, string fieldName) { }

        #endregion

        #region Provider implementation

        /// <summary>
        /// Reindexes an item
        /// </summary>
        /// <param name="items">XML node to reindex</param>
        /// <param name="category">Type of index to use</param>
        public override void ReIndexNodes(string category, params IndexItem[] items)
        {
            //now index the single node            
            AddNodesToIndex(category, items.Select(x => new IndexOperation {Item = x, Operation = IndexOperationType.Add}).ToArray());
        }

        /// <summary>
        /// Creates a brand new index, this will override any existing index with an empty one
        /// </summary>
        public void CreateIndex()
        {
            IndexWriter writer = null;
            try
            {
                //ensure the folder exists
                ReInitialize();

                //check if the index exists and it's locked
                if (IndexExists() && !IndexReady())
                {
                    OnIndexingError(new IndexingErrorEventArgs("Cannot create index, the index is currently locked", string.Empty, null));
                    return;
                }

                //create the writer (this will overwrite old index files)
                writer = new IndexWriter(LuceneDirectory, IndexingAnalyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);

            }
            catch (Exception ex)
            {
                OnIndexingError(new IndexingErrorEventArgs("An error occurred recreating the index set", string.Empty, ex));
                return;
            }
            finally
            {
                CloseWriter(ref writer);
            }

        }

        /// <summary>
        /// Deletes a node from the index.                
        /// </summary>
        /// <remarks>
        /// When a content node is deleted, we also need to delete it's children from the index so we need to perform a 
        /// custom Lucene search to find all decendents and create Delete item queues for them too.
        /// </remarks>
        /// <param name="id">ID of the node to delete</param>
        public override void DeleteFromIndex(string id)
        {
            var buffer = new[]
                {
                    GetDeleteItemOperation(id)
                };

            SafelyProcessQueueItems(buffer);
        }

        #endregion

        #region Protected

        

        /// <summary>
        /// Returns an index operation to remove the item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected IndexOperation GetDeleteItemOperation(string id)
        {
            var operation = new IndexOperation
                {
                    Item = new IndexItem
                        {
                            Fields = new Dictionary<string, string> { { IndexNodeIdFieldName, id } },
                            Id = id,
                            ItemCategory = string.Empty
                        },
                    Operation = IndexOperationType.Delete
                };
            return operation;
        }

        /// <summary>
        /// This will add all items to the index but will first attempt to delete them from the index to avoid duplicates
        /// </summary>
        /// <param name="category"></param>
        /// <param name="items"></param>
        protected void AddNodesToIndex(string category, params IndexOperation[] items)
        {

            //check if the index doesn't exist, and if so, create it and reindex everything, this will obviously index this
            //particular node
            if (!IndexExists())
            {
                CreateIndex();
            }

            var buffer = new List<IndexOperation>();

            foreach (var i in items)
            {
                var idResult = InternalSearcher.Search(InternalSearcher.CreateSearchCriteria().Id(i.Item.Id).Compile());
                if (idResult.Any())
                {
                    //first add a delete queue for this item
                    buffer.Add(GetDeleteItemOperation(i.Item.Id));    
                }

                if (ValidateDocument(i.Item))
                {
                    //save the index item to a queue
                    var fields = GetDataToIndex(i.Item, category);
                    BufferAddIndexQueueItem(fields, i.Item.Id, category, buffer);
                }
                else
                {
                    OnIgnoringNode(new IndexingNodeDataEventArgs(i.Item, i.Item.Id, null, category));
                }

            }

            //run the indexer on all queued files
            SafelyProcessQueueItems(buffer);
        }

        /// <summary>
        /// Returns IndexCriteria object from the IndexSet
        /// </summary>
        /// <param name="indexSet"></param>
        protected virtual IndexCriteria GetIndexerData(IndexSet indexSet)
        {
            return new IndexCriteria(
                indexSet.Fields.Cast<IIndexFieldDefinition>().ToArray(),
                indexSet.IncludeItemTypes.ToList().Select(x => x.Name).ToArray(),
                indexSet.ExcludeItemTypes.ToList().Select(x => x.Name).ToArray());
        }

        /// <summary>
        /// Checks if the index is ready to open/write to.
        /// </summary>
        /// <returns></returns>
        protected bool IndexReady()
        {
            return (!IndexWriter.IsLocked(LuceneDirectory));
        }

        /// <summary>
        /// Check if there is an index in the index folder
        /// </summary>
        /// <returns></returns>
        public virtual bool IndexExists()
        {
            return IndexReader.IndexExists(LuceneDirectory);
        }

        /// <summary>
        /// This wil optimize the index for searching, this gets executed when this class instance is instantiated.
        /// </summary>
        /// <remarks>
        /// This can be an expensive operation and should only be called when there is no indexing activity
        /// </remarks>
        protected void OptimizeIndex()
        {
            //check if this machine is the executive.
            if (!ExecutiveIndex.IsExecutiveMachine)
                return;

            if (!_isIndexing)
            {
                lock (_indexerLocker)
                {
                    //double check
                    if (!_isIndexing)
                    {

                        //set our volatile flag
                        _isIndexing = true;

                        IndexWriter writer = null;
                        try
                        {
                            if (!IndexExists())
                                return;

                            //check if the index is ready to be written to.
                            if (!IndexReady())
                            {
                                OnIndexingError(new IndexingErrorEventArgs("Cannot optimize index, the index is currently locked", string.Empty, null), true);
                                return;
                            }

                            writer = new IndexWriter(LuceneDirectory, IndexingAnalyzer, !IndexExists(), IndexWriter.MaxFieldLength.UNLIMITED);

                            OnIndexOptimizing(new EventArgs());

                            //wait for optimization to complete (true)
                            writer.Optimize(true);

                            OnIndexOptimized(new EventArgs());
                        }
                        catch (Exception ex)
                        {
                            OnIndexingError(new IndexingErrorEventArgs("Error optimizing Lucene index", string.Empty, ex));
                        }
                        finally
                        {
                            //set our volatile flag
                            _isIndexing = false;

                            CloseWriter(ref writer);
                        }
                    }

                }
            }


        }

        /// <summary>
        /// Removes the specified term from the index
        /// </summary>
        /// <param name="indexTerm"></param>
        /// <param name="ir"></param>
        /// <returns>Boolean if it successfully deleted the term, or there were on errors</returns>
        protected bool DeleteFromIndex(Term indexTerm, IndexReader ir)
        {
            var nodeId = string.Empty;
            if (indexTerm.Field() == "id")
                nodeId = indexTerm.Text();

            try
            {
                ReInitialize();

                //if the index doesn't exist, then no don't attempt to open it.
                if (!IndexExists())
                    return true;

                int delCount = ir.DeleteDocuments(indexTerm);

                ir.Commit(); //commit the changes!

                if (delCount > 0)
                {
                    OnIndexDeleted(new DeleteIndexEventArgs(new KeyValuePair<string, string>(indexTerm.Field(), indexTerm.Text()), delCount));
                }
                return true;
            }
            catch (Exception ee)
            {
                OnIndexingError(new IndexingErrorEventArgs("Error deleting Lucene index", nodeId, ee));
                return false;
            }
        }

        /// <summary>
        /// Ensures that the node being indexed is of a correct type and is a descendent of the parent id specified.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual bool ValidateDocument(IndexItem item)
        {
            //check if this document is of a correct type of node type alias
            if (IndexerData.IncludeItemTypes.Count() > 0)
                if (!IndexerData.IncludeItemTypes.Contains(item.ItemCategory))
                    return false;

            //if this node type is part of our exclusion list, do not validate
            if (IndexerData.ExcludeItemTypes.Count() > 0)
                if (IndexerData.ExcludeItemTypes.Contains(item.ItemCategory))
                    return false;

            return true;
        }


        /// <summary>
        /// Translates the XElement structure into a dictionary object to be indexed.
        /// </summary>
        /// <remarks>
        /// This is used when re-indexing an individual node since this is the way the provider model works.
        /// For this provider, it will use a very similar XML structure as umbraco 4.0.x:
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// <root>
        ///     <node id="1234" nodeTypeAlias="yourIndexType">
        ///         <data alias="fieldName1">Some data</data>
        ///         <data alias="fieldName2">Some other data</data>
        ///     </node>
        ///     <node id="345" nodeTypeAlias="anotherIndexType">
        ///         <data alias="fieldName3">More data</data>
        ///     </node>
        /// </root>
        /// ]]>
        /// </code>        
        /// </example>
        /// <param name="item"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        protected virtual IDictionary<string, string> GetDataToIndex(IndexItem item, string category)
        {
            IDictionary<string, string> values = new Dictionary<string, string>();

            var id = item.Id;

            // Get all user data that we want to index and store into a dictionary.

            // If no fields are specified, we will just index all fields!

            if (IndexerData.Fields.Any())
            {
                //only index the fields that are defined...

                foreach (var field in IndexerData.Fields)
                {
                    // Get the value of the data                
                    var value = item.Fields[field.Name];

                    //raise the event and assign the value to the returned data from the event
                    var indexingFieldDataArgs = new IndexingFieldDataEventArgs(item, field.Name, value, false, id);
                    OnGatheringFieldData(indexingFieldDataArgs);
                    value = indexingFieldDataArgs.FieldValue;

                    //don't add if the value is empty/null
                    if (string.IsNullOrEmpty(value)) continue;
                    if (!string.IsNullOrEmpty(value))
                        values.Add(field.Name, value);
                }
            }
            else
            {
                //no fields specified, so index all fields...

                foreach (var field in item.Fields)
                {
                    // Get the value of the data                
                    var value = field.Value;

                    //raise the event and assign the value to the returned data from the event
                    var indexingFieldDataArgs = new IndexingFieldDataEventArgs(item, field.Key, value, false, id);
                    OnGatheringFieldData(indexingFieldDataArgs);
                    value = indexingFieldDataArgs.FieldValue;

                    //don't add if the value is empty/null
                    if (string.IsNullOrEmpty(value)) continue;
                    if (!string.IsNullOrEmpty(value))
                        values.Add(field.Key, value);
                }
            }
            

            //raise the event and assign the value to the returned data from the event
            var indexingNodeDataArgs = new IndexingNodeDataEventArgs(item, id, values, category);
            OnGatheringNodeData(indexingNodeDataArgs);
            values = indexingNodeDataArgs.Fields;

            return values;
        }

        /// <summary>
        /// Determines the indexing policy for the field specified, by default unless thsi method is overridden, all fields will be "Analyzed"
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        protected virtual FieldIndexTypes GetPolicy(string fieldName, string indexCategory)
        {
            return FieldIndexTypes.ANALYZED;
        }

        /// <summary>
        /// Translates our FieldIndexTypes to Lucene field index types
        /// </summary>
        /// <param name="fieldIndex"></param>
        /// <returns></returns>
        private static Field.Index TranslateFieldIndexTypeToLuceneType(FieldIndexTypes fieldIndex)
        {
            switch (fieldIndex)
            {
                case FieldIndexTypes.ANALYZED:
                    return Field.Index.ANALYZED;

                case FieldIndexTypes.ANALYZED_NO_NORMS:
                    return Field.Index.ANALYZED_NO_NORMS;

                case FieldIndexTypes.NO:
                    return Field.Index.NO;

                case FieldIndexTypes.NOT_ANALYZED:
                    return Field.Index.NOT_ANALYZED;

                case FieldIndexTypes.NOT_ANALYZED_NO_NORMS:
                    return Field.Index.NOT_ANALYZED_NO_NORMS;

                default:
                    throw new Exception("Unknown field index type");
            }
        }


        /// <summary>
        /// Collects the data for the fields and adds the document which is then committed into Lucene.Net's index
        /// </summary>
        /// <param name="fields">The fields and their associated data.</param>
        /// <param name="writer">The writer that will be used to update the Lucene index.</param>
        /// <param name="id">The node id.</param>
        /// <param name="type">The type to index the node as.</param>
        /// <remarks>
        /// This will normalize (lowercase) all text before it goes in to the index.
        /// </remarks>
        protected virtual void AddDocument(IDictionary<string, string> fields, IndexWriter writer, string id, string type)
        {
            var args = new IndexingNodeEventArgs(id, fields, type);
            OnNodeIndexing(args);
            if (args.Cancel)
                return;

            var d = new Document();

            //get all index set fields that are defined
            var indexSetFields = IndexerData.Fields.ToList();

            //add all of our fields to the document index individually, don't include the special fields if they exists            
            var validFields = fields.Where(x => !x.Key.StartsWith(SpecialFieldPrefix)).ToList();

            foreach (var x in validFields)
            {
                var ourPolicyType = GetPolicy(x.Key, fields[IndexCategoryFieldName]);
                var lucenePolicy = TranslateFieldIndexTypeToLuceneType(ourPolicyType);

                var indexedFields = indexSetFields.Where(o => o.Name == x.Key);

                if (indexedFields.Count() == 0)
                {
                    //TODO: Decide if we should support non-strings in here too
                    d.Add(
                    new Field(x.Key,
                        x.Value,
                        Field.Store.YES,
                        lucenePolicy,
                        lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES));
                }

                else
                {
                    //checks if there's duplicates fields, if not check if the field needs to be sortable...
                    if (indexedFields.Count() > 1)
                    {
                        //we wont error if there are two fields which match, we'll just log an error and ignore the 2nd field                        
                        OnDuplicateFieldWarning(id, x.Key, IndexSetName);
                    }
                    else
                    {
                        var indexField = indexedFields.First();
                        Fieldable field = null;
                        object parsedVal = null;
                        if (string.IsNullOrEmpty(indexField.DataType)) indexField.DataType = string.Empty;
                        switch (indexField.DataType.ToUpper())
                        {
                            case "NUMBER":
                            case "INT":
                                if (!TryConvert<int>(x.Value, out parsedVal))
                                    break;
                                field = new NumericField(x.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetIntValue((int)parsedVal);
                                break;
                            case "FLOAT":
                                if (!TryConvert<float>(x.Value, out parsedVal))
                                    break;
                                field = new NumericField(x.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetFloatValue((float)parsedVal);
                                break;
                            case "DOUBLE":
                                if (!TryConvert<double>(x.Value, out parsedVal))
                                    break;
                                field = new NumericField(x.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetDoubleValue((double)parsedVal);
                                break;
                            case "LONG":
                                if (!TryConvert<long>(x.Value, out parsedVal))
                                    break;
                                field = new NumericField(x.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetLongValue((long)parsedVal);
                                break;
                            case "DATE":
                            case "DATETIME":
                                {
                                    if (!TryConvert<DateTime>(x.Value, out parsedVal))
                                        break;

                                    DateTime date = (DateTime)parsedVal;
                                    string dateAsString = DateTools.DateToString(date, DateTools.Resolution.MILLISECOND);
                                    //field = new NumericField(x.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetLongValue(long.Parse(dateAsString));
                                    field =
                                    new Field(x.Key,
                                        dateAsString,
                                        Field.Store.YES,
                                        lucenePolicy,
                                        lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                                    );

                                    break;
                                }
                            case "DATE.YEAR":
                                {
                                    if (!TryConvert<DateTime>(x.Value, out parsedVal))
                                        break;

                                    DateTime date = (DateTime)parsedVal;
                                    string dateAsString = DateTools.DateToString(date, DateTools.Resolution.YEAR);
                                    //field = new NumericField(x.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetIntValue(int.Parse(dateAsString));
                                    field =
                                    new Field(x.Key,
                                        dateAsString,
                                        Field.Store.YES,
                                        lucenePolicy,
                                        lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                                    );

                                    break;
                                }
                            case "DATE.MONTH":
                                {
                                    if (!TryConvert<DateTime>(x.Value, out parsedVal))
                                        break;

                                    DateTime date = (DateTime)parsedVal;
                                    string dateAsString = DateTools.DateToString(date, DateTools.Resolution.MONTH);
                                    //field = new NumericField(x.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetIntValue(int.Parse(dateAsString));
                                    field =
                                    new Field(x.Key,
                                        dateAsString,
                                        Field.Store.YES,
                                        lucenePolicy,
                                        lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                                    );

                                    break;
                                }
                            case "DATE.DAY":
                                {
                                    if (!TryConvert<DateTime>(x.Value, out parsedVal))
                                        break;

                                    DateTime date = (DateTime)parsedVal;
                                    string dateAsString = DateTools.DateToString(date, DateTools.Resolution.DAY);
                                    //field = new NumericField(x.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetIntValue(int.Parse(dateAsString));
                                    field =
                                    new Field(x.Key,
                                        dateAsString,
                                        Field.Store.YES,
                                        lucenePolicy,
                                        lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                                    );
                                    break;
                                }
                            case "DATE.HOUR":
                                {
                                    if (!TryConvert<DateTime>(x.Value, out parsedVal))
                                        break;

                                    DateTime date = (DateTime)parsedVal;
                                    string dateAsString = DateTools.DateToString(date, DateTools.Resolution.HOUR);
                                    //field = new NumericField(x.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetIntValue(int.Parse(dateAsString));
                                    field =
                                    new Field(x.Key,
                                        dateAsString,
                                        Field.Store.YES,
                                        lucenePolicy,
                                        lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                                    );
                                    break;
                                }
                            case "DATE.MINUTE":
                                {
                                    if (!TryConvert<DateTime>(x.Value, out parsedVal))
                                        break;

                                    DateTime date = (DateTime)parsedVal;
                                    string dateAsString = DateTools.DateToString(date, DateTools.Resolution.MINUTE);
                                    //field = new NumericField(x.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetIntValue(int.Parse(dateAsString));
                                    field =
                                    new Field(x.Key,
                                        dateAsString,
                                        Field.Store.YES,
                                        lucenePolicy,
                                        lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                                    );
                                    break;
                                }
                            default:
                                field =
                                    new Field(x.Key,
                                        x.Value,
                                        Field.Store.YES,
                                        lucenePolicy,
                                        lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                                    );
                                break;
                        }

                        //if the parsed value is null, this means it couldn't parse and we should log this error
                        if (field == null)
                        {
                            OnIndexingError(new IndexingErrorEventArgs("Could not parse value: " + x.Value + "into the type: " + indexField.DataType, id, null));
                        }
                        else
                        {
                            d.Add(field);

                            if (indexField.EnableSorting)
                            {
                                d.Add(new Field(SortedFieldNamePrefix + x.Key,
                                        x.Value,
                                        Field.Store.YES,
                                        Field.Index.NOT_ANALYZED,
                                        Field.TermVector.NO
                                        ));
                            }
                        }

                    }
                }
            }

            AddSpecialFieldsToDocument(d, fields);

            var docArgs = new DocumentWritingEventArgs(id, d, fields);
            OnDocumentWriting(docArgs);
            if (docArgs.Cancel)
                return;

            writer.AddDocument(d);

            writer.Commit(); //commit changes!

            OnNodeIndexed(new IndexedNodeEventArgs(id));
        }



        /// <summary>
        /// Returns a dictionary of special key/value pairs to store in the lucene index which will be stored by:
        /// - Field.Store.YES
        /// - Field.Index.NOT_ANALYZED_NO_NORMS
        /// - Field.TermVector.NO
        /// </summary>
        /// <param name="allValuesForIndexing">
        /// The dictionary object containing all name/value pairs that are to be put into the index
        /// </param>
        /// <returns></returns>
        protected virtual IDictionary<string, string> GetSpecialFieldsToIndex(IDictionary<string, string> allValuesForIndexing)
        {
            return new Dictionary<string, string>() 
			{
				//we want to store the nodeId separately as it's the index
				{IndexNodeIdFieldName, allValuesForIndexing[IndexNodeIdFieldName]},
				//add the index type first
				{IndexCategoryFieldName, allValuesForIndexing[IndexCategoryFieldName]}
			};
        }


        /// <summary>
        /// Process all of the queue items. This checks if this machine is the Executive and if it's in a load balanced
        /// environments. If then acts accordingly: 
        ///     Not the executive = doesn't index, i
        ///     In async mode = use file watcher timer
        /// </summary>
        protected internal void SafelyProcessQueueItems(IEnumerable<IndexOperation> buffer)
        {
            //if this is not the master indexer, exit
            if (!ExecutiveIndex.IsExecutiveMachine)
                return;

            //if in async mode, then process the queue using the timer
            switch (SynchronizationType)
            {
                case SynchronizationType.SingleThreaded:
                    var list = new ConcurrentQueue<IndexOperation>();
                    foreach(var i in buffer)
                    {
                        list.Enqueue(i);    
                    }
                    ForceProcessQueueItems(list);
                    break;
                case SynchronizationType.AsyncBackgroundWorker:
                    InitializeBackgroundWorker(buffer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        /// <summary>
        /// Loop through all files in the queue item folder and index them.
        /// Regardless of weather this machine is the executive indexer or not or is in a load balanced environment
        /// or not, this WILL attempt to process the queue items into the index.
        /// </summary>
        /// <returns>
        /// The number of queue items processed
        /// </returns>
        /// <remarks>
        /// Inheritors should be very carefully using this method, SafelyProcessQueueItems will ensure
        /// that the correct machine processes the items into the index. SafelyQueueItems calls this method
        /// if it confirms that this machine is the one to process the queue.
        /// </remarks>
        protected int ForceProcessQueueItems(ConcurrentQueue<IndexOperation> buffer)
        {
            try
            {
                ReInitialize();
            }
            catch (IOException ex)
            {
                OnIndexingError(new IndexingErrorEventArgs("Cannot index queue items, an error occurred verifying index folders", string.Empty, ex));
                return 0;
            }

            if (!IndexExists())
            {
                //this shouldn't happen!
                OnIndexingError(new IndexingErrorEventArgs("Cannot index queue items, the index doesn't exist!", string.Empty, null));
                return 0;
            }

            //check if the index is ready to be written to.
            if (!IndexReady())
            {
                OnIndexingError(new IndexingErrorEventArgs("Cannot index queue items, the index is currently locked", string.Empty, null));
                return 0;
            }

            if (!_isIndexing)
            {
                lock (_indexerLocker)
                {
                    //double check
                    if (!_isIndexing)
                    {
                        //set our volatile flag
                        _isIndexing = true;

                        IndexWriter writer = null;
                        IndexReader reader = null;

                        //track all of the nodes indexed
                        var indexedNodes = new List<IndexedNode>();

                        try
                        {
                            //iterate through the items in the buffer, they should be in the exact order in which 
                            //they were added so shouldn't need to sort anything

                            //we need to iterate like this because our threadsafe list doesn't allow enumeration
                            IndexOperation item;

                            while (buffer.TryDequeue(out item))
                            {
                                switch (item.Operation)
                                {
                                    case IndexOperationType.Add:
                                        if (GetExclusiveIndexWriter(ref writer, ref reader))
                                        {
                                            indexedNodes.Add(ProcessAddQueueItem(item.Item, writer));
                                        }
                                        else
                                        {
                                            OnIndexingError(new IndexingErrorEventArgs("Error indexing queue items, failed to obtain exclusive writer lock", string.Empty, null), true);
                                            return indexedNodes.Count;
                                        }
                                        break;
                                    case IndexOperationType.Delete:
                                        if (GetExclusiveIndexReader(ref reader, ref writer))
                                        {
                                            ProcessDeleteQueueItem(item.Item, reader);
                                        }
                                        else
                                        {
                                            OnIndexingError(new IndexingErrorEventArgs("Error indexing queue items, failed to obtain exclusive reader lock", string.Empty, null), true);
                                            return indexedNodes.Count;
                                        }
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }                                
                            }

                            //raise the completed event
                            OnNodesIndexed(new IndexedNodesEventArgs(IndexerData, indexedNodes));

                        }
                        catch (Exception ex)
                        {
                            OnIndexingError(new IndexingErrorEventArgs("Error indexing queue items", string.Empty, ex));
                        }
                        finally
                        {
                            //set our volatile flag
                            _isIndexing = false;

                            CloseWriter(ref writer);
                            CloseReader(ref reader);
                        }

                        //if there are enough commits, the we'll run an optimization
                        if (CommitCount >= OptimizationCommitThreshold)
                        {
                            OptimizeIndex();
                            CommitCount = 0; //reset the counter
                        }

                        return indexedNodes.Count;
                    }
                }
            }

            //if we get to this point, it means that another thead was beaten to the indexing operation so this thread will skip
            //this occurence.
            OnIndexingError(new IndexingErrorEventArgs("Cannot index queue items, another indexing operation is currently in progress", string.Empty, null));
            return 0;


        }

        /// <summary>
        /// Used for re-indexing many nodes at once, this updates the fields object and appends it to the buffered list of items which
        /// will then get written to file in one bulk file.
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="id"></param>
        /// <param name="itemCategory"></param>
        /// <param name="buffer"></param>
        protected void BufferAddIndexQueueItem(IDictionary<string, string> fields, string id, string itemCategory, IList<IndexOperation> buffer)
        {
            //ensure the special fields are added to the dictionary to be saved to file
            EnsureSpecialFields(fields, id, itemCategory);

            //ok, everything is ready to go, add it to the buffer
            buffer.Add(new IndexOperation { Item = new IndexItem { Fields = fields, Id = id, ItemCategory = itemCategory }, Operation = IndexOperationType.Add });
        }

        #endregion

        #region Private

        private void EnsureSpecialFields(IDictionary<string, string> fields, string id, string category)
        {
            //ensure the special fields are added to the dictionary to be saved to file
            if (!fields.ContainsKey(IndexNodeIdFieldName))
                fields.Add(IndexNodeIdFieldName, id.ToString());
            if (!fields.ContainsKey(IndexCategoryFieldName))
                fields.Add(IndexCategoryFieldName, category.ToString());
        }

        /// <summary>
        /// Tries to parse a type using the Type's type converter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="parsedVal"></param>
        /// <returns></returns>
        private static bool TryConvert<T>(string val, out object parsedVal)
            where T : struct
        {
            try
            {
                var t = typeof(T);
                TypeConverter tc = TypeDescriptor.GetConverter(t);
                parsedVal = (T)tc.ConvertFrom(val);
                return true;
            }
            catch (NotSupportedException)
            {
                parsedVal = null;
                return false;
            }

        }

        /// <summary>
        /// Adds 'special' fields to the Lucene index for use internally.
        /// By default this will add the __IndexType & __NodeId fields to the Lucene Index both specified by:
        /// - Field.Store.YES
        /// - Field.Index.NOT_ANALYZED_NO_NORMS
        /// - Field.TermVector.NO
        /// </summary>
        /// <param name="d"></param>
        /// <param name="fields"></param>
        private void AddSpecialFieldsToDocument(Document d, IDictionary<string, string> fields)
        {
            var specialFields = GetSpecialFieldsToIndex(fields);

            foreach (var s in specialFields)
            {
                //TODO: we're going to lower case the special fields, the Standard analyzer query parser always lower cases, so 
                //we need to do that... there might be a nicer way ?
                d.Add(new Field(s.Key, s.Value.ToLower(), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS, Field.TermVector.NO));
            }
        }

        /// <summary>
        /// This makes sure that the folders exist, that the executive indexer is setup and that the index is optimized.
        /// This is called at app startup when the providers are initialized but called again if folder are missing during a
        /// an indexing operation.
        /// </summary>
        private void ReInitialize()
        {
            //ensure all of the folders are created at startup   
            VerifyFolder(WorkingFolder);

            if (ExecutiveIndex == null)
            {
                ExecutiveIndex = new IndexerExecutive(WorkingFolder);
            }

            if (!ExecutiveIndex.IsInitialized())
            {
                ExecutiveIndex.Initialize();

                //log some info if executive indexer
                if (ExecutiveIndex.IsExecutiveMachine)
                {
                    OnIndexerExecutiveAssigned(new IndexerExecutiveAssignedEventArgs(ExecutiveIndex.ExecutiveIndexerMachineName, ExecutiveIndex.ServerCount));
                }
            }
        }

        private void InitializeBackgroundWorker(IEnumerable<IndexOperation> buffer)
        {
            if(_threadPool == null)
            {
                lock(CreateThreadPoolLock)
                {
                    var start = new STPStartInfo
                        {
                            UseCallerCallContext = false,
                            UseCallerHttpContext = false,
                            MaxWorkerThreads = 5
                        };
                    _threadPool = new SmartThreadPool(start);
                }
            }

            //if (_workerThread == null)
            //{
            //    _workerThread = new Thread(_workerThreadDoWorkEventHandler) {IsBackground = true};                             
            //}

            //if this is not the master indexer anymore... perhaps another server has taken over somehow...
            if (!ExecutiveIndex.IsExecutiveMachine)
            {
                //this will abort the thread once it's latest processing has stopped.
                if (!_isCancelling)
                {
                    lock(_cancelLocker)
                    {
                        if(!_isCancelling)
                        {
                            _isCancelling = true;            
                        }
                    }
                }
                
                return;
            }

            //re-index everything in the buffer, add everything safely to our threadsafe queue
            foreach (var b in buffer)
            {
                _asyncQueue.Enqueue(b);
            }

            //don't run the worker if it's currently running since it will just pick up the rest of the queue during its normal operation
            if (!IsBusy)
            {
                //_workerThread.Start();
                //_threadPool.QueueWorkItem(new WorkItemInfo {UseCallerHttpContext = false, UseCallerCallContext = false}, WorkerThreadDoWork);
                _threadPool.QueueWorkItem(WorkerThreadDoWork);
            }

            //if (!_shouldProcess)
            //{
            //    lock (_shouldProcessLock)
            //    {
            //        if (!_shouldProcess)
            //        {
            //            _shouldProcess = true;
            //        }
            //    }
            //}

        }

        /// <summary>
        /// Uses a background worker thread to do all of the indexing
        /// </summary>
        /// <remarks>>
        /// This will continue to run forever until cancelled is called
        /// </remarks>
        void WorkerThreadDoWork()
        {
            //Thread.Sleep(10000);

            //keep processing until it is complete
            var numProcessedItems = 0;
            do
            {
                numProcessedItems = ForceProcessQueueItems(_asyncQueue);
            } while (!_isCancelling && numProcessedItems > 0);
        }

        ///// <summary>
        ///// Uses a background worker thread to do all of the indexing
        ///// </summary>
        ///// <remarks>>
        ///// This will continue to run forever until cancelled is called
        ///// </remarks>
        //void WorkerThreadDoWork()
        //{
        //    while(!_isCancelling)
        //    {
        //        if (!_shouldProcess)
        //            Thread.Sleep(1000);
        //        else
        //        {
        //            Thread.Sleep(10000);

        //            //keep processing until it is complete
        //            var numProcessedItems = 0;
        //            do
        //            {
        //                numProcessedItems = ForceProcessQueueItems(_asyncQueue);
        //            } while (!_isCancelling && numProcessedItems > 0);

        //            //cease processing 
        //            _shouldProcess = false;    
        //        }                
        //    }
            
        //    //return null;
        //}


        /// <summary>
        /// Checks the writer passed in to see if it is active, if not, checks if the index is locked. If it is locked, 
        /// returns checks if the reader is not null and tries to close it. if it's still locked returns null, otherwise
        /// creates a new writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private bool GetExclusiveIndexWriter(ref IndexWriter writer, ref IndexReader reader)
        {
            //if the writer is already created, then we're ok
            if (writer != null)
                return true;

            //checks for locks and closes the reader if one is found
            if (!IndexReady())
            {
                if (reader != null)
                {
                    CloseReader(ref reader);
                    if (!IndexReady())
                    {
                        return false;
                    }
                }
            }

            writer = new IndexWriter(LuceneDirectory, IndexingAnalyzer, false, IndexWriter.MaxFieldLength.UNLIMITED);
            return true;
        }

        /// <summary>
        /// Checks the reader passed in to see if it is active, if not, checks if the index is locked. If it is locked, 
        /// returns checks if the writer is not null and tries to close it. if it's still locked returns null, otherwise
        /// creates a new reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="writer"></param>
        /// <returns>
        /// This also ensures that the reader is up to date, and if it is not, it re-opens the reader.
        /// </returns>
        private bool GetExclusiveIndexReader(ref IndexReader reader, ref IndexWriter writer)
        {
            //checks for locks and closes the writer if one is found
            if (!IndexReady())
            {
                if (writer != null)
                {
                    CloseWriter(ref writer);
                    if (!IndexReady())
                    {
                        return false;
                    }
                }
            }

            if (reader != null)
            {
                //Turns out that each time we need to process one of these items, we'll need to refresh the reader since it won't be up
                //to date if the .add files are processed
                switch (reader.GetReaderStatus())
                {
                    case ReaderStatus.Current:
                        //if it's current, then we're ok
                        return true;
                    case ReaderStatus.NotCurrent:
                        //this will generally not be current each time an .add is processed and there's more deletes after the fact, we'll need to re-open

                        //yes, this is actually the way the Lucene wants you to work...
                        //normally, i would have thought just calling Reopen() on the underlying reader would suffice... but it doesn't.
                        //here's references: 
                        // http://stackoverflow.com/questions/1323779/lucene-indexreader-reopen-doesnt-seem-to-work-correctly
                        // http://gist.github.com/173978 
                        var oldReader = reader;
                        var newReader = oldReader.Reopen(false);
                        if (newReader != oldReader)
                        {
                            oldReader.Close();
                            reader = newReader;
                        }
                        //now that the reader is re-opened, we're good
                        return true;
                    case ReaderStatus.Closed:
                        //if it's closed, then we'll allow it to be opened below...
                        break;
                    default:
                        break;
                }
            }

            //if we've made it this far, open a reader
            reader = IndexReader.Open(LuceneDirectory, false);
            return true;

        }

        /// <summary>
        /// Reads the FileInfo passed in into a dictionary object and deletes it from the index
        /// </summary>
        /// <param name="x"></param>
        /// <param name="ir"></param>
        private void ProcessDeleteQueueItem(IndexItem x, IndexReader ir)
        {
            //we know that there's only ever one item saved to the dictionary for deletions
            if (x.Fields.Count != 1)
            {
                OnIndexingError(new IndexingErrorEventArgs("Could not remove queue item from index, the dictionary is not properly formatted", string.Empty, null));
                return;
            }
            var term = x.Fields.First();
            DeleteFromIndex(new Term(term.Key, term.Value), ir);

            CommitCount++;
        }



        /// <summary>
        /// Reads the FileInfo passed in into a dictionary object and adds it to the index
        /// </summary>
        /// <param name="x"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        private IndexedNode ProcessAddQueueItem(IndexItem x, IndexWriter writer)
        {
            //get the node id
            var id = x.Fields[IndexNodeIdFieldName];

            //now, add the index with our dictionary object
            AddDocument(x.Fields, writer, id, x.Fields[IndexCategoryFieldName]);

            CommitCount++;

            return new IndexedNode() { Id = id, Type = x.Fields[IndexCategoryFieldName] };
        }

        private void CloseWriter(ref IndexWriter writer)
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;
            }
        }

        private void CloseReader(ref IndexReader reader)
        {
            if (reader != null)
            {
                reader.Close();
                reader = null;
            }
        }



        /// <summary>
        /// Creates the folder if it does not exist.
        /// </summary>
        /// <param name="folder"></param>
        private void VerifyFolder(DirectoryInfo folder)
        {
            if (!System.IO.Directory.Exists(folder.FullName))
            {
                lock (_folderLocker)
                {
                    if (!System.IO.Directory.Exists(folder.FullName))
                    {
                        folder.Create();
                        folder.Refresh();
                    }
                }
            }

        }



        #endregion

        #region IDisposable Members

        protected bool _disposed;

        /// <summary>
        /// Checks the disposal state of the objects
        /// </summary>
        protected void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("LuceneExamine.BaseLuceneExamineIndexer");
        }

        /// <summary>
        /// When the object is disposed, all data should be written
        /// </summary>
        public void Dispose()
        {
            this.CheckDisposed();
            this.Dispose(true);
            GC.SuppressFinalize(this);
            this._disposed = true;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            this.CheckDisposed();
            if (disposing)
            {
                if (!_isCancelling)
                {
                    lock (_cancelLocker)
                    {
                        if (!_isCancelling)
                        {
                            _isCancelling = true;
                        }
                    }
                }

                if (_threadPool != null)
                    _threadPool.Dispose();

                //if (_workerThread != null)
                //    _workerThread.Abort();
                //this._workerThread.Abort();
            }
                
        }

        #endregion
    }
}
