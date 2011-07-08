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
using System.Threading.Tasks;
using System.Xml.Linq;
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
    public class LuceneIndexer : BaseIndexProvider
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
        public LuceneIndexer(DirectoryInfo workingFolder, Analyzer analyzer, SynchronizationType synchronizationType)
        {
            //_workerThreadDoWorkEventHandler = new ThreadStart(WorkerThreadDoWork);

            //set up our folders based on the index path
            WorkingFolder = workingFolder;


            IndexingAnalyzer = analyzer;

            var luceneIndexFolder = new DirectoryInfo(Path.Combine(workingFolder.FullName, "Index"));
            VerifyFolder(luceneIndexFolder);

            //create our internal searcher, this is useful for inheritors to be able to search their own indexes inside of their indexer
            InternalSearcher = new LuceneSearcher(luceneIndexFolder, IndexingAnalyzer);

            OptimizationCommitThreshold = 100;
            SynchronizationType = synchronizationType;

            LuceneDirectory = new SimpleFSDirectory(luceneIndexFolder);

            ReInitialize();
        }

        /// <summary>
        /// Constructor to allow for creating an indexer at runtime which allows specifying a custom lucene 'Directory'
        /// </summary>
        /// <param name="indexerData"></param>
        /// <param name="workingFolder"></param>
        /// <param name="analyzer"></param>
        /// <param name="synchronizationType"></param>
        /// <param name="luceneDirectory"></param>
        public LuceneIndexer(DirectoryInfo workingFolder, Analyzer analyzer, SynchronizationType synchronizationType, Directory luceneDirectory)
        {
            //_workerThreadDoWorkEventHandler = new ThreadStart(WorkerThreadDoWork);

            //set up our folders based on the index path
            WorkingFolder = workingFolder;

            IndexingAnalyzer = analyzer;

            //create our internal searcher, this is useful for inheritors to be able to search their own indexes inside of their indexer
            InternalSearcher = new LuceneSearcher(IndexingAnalyzer, luceneDirectory);

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
            InternalSearcher = new LuceneSearcher(IndexingAnalyzer, LuceneDirectory);

            SynchronizationType = SynchronizationType.AsyncBackgroundWorker;
            if (config["synchronizationType"] != null)
            {
                SynchronizationType = (SynchronizationType)Enum.Parse(typeof(SynchronizationType), config["synchronizationType"]);
            }

            ReInitialize();

            CommitCount = 0;

        }

        #endregion

        #region Static Helpers
        /// <summary>
        /// Returns an index operation to remove the item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IndexOperation CreateDeleteItemOperation(string id)
        {
            var operation = new IndexOperation
            {
                Item = new IndexItem
                {
                    Fields = new Dictionary<string, ItemField> { { IndexNodeIdFieldName, new ItemField(id) } },
                    Id = id,
                    ItemCategory = string.Empty
                },
                Operation = IndexOperationType.Delete
            };
            return operation;
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
        /// Used to timestamp a record, this is used to deduplicate
        /// </summary>
        public const string IndexItemTimeStamp = SpecialFieldPrefix + "TimeStamp";

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

        private Task _asyncTask;

        /// <summary>
        /// Used to cancel the async operation
        /// </summary>
        private bool _isCancelling = false;

        /// <summary>
        /// We need an internal searcher used to search against our own index.
        /// This is used for finding all descendant nodes of a current node when deleting indexes.
        /// </summary>
        protected BaseSearchProvider InternalSearcher { get; private set; }

        #endregion

        #region Properties

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


        /// <summary>
        /// Overrides the handler to strip out any null values that might remain
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNodeIndexing(IndexingNodeEventArgs e)
        {
            base.OnNodeIndexing(e);


            var keysToRemove = (from i in e.Item.Fields
                                where string.IsNullOrEmpty(i.Value.FieldValue)
                                select i.Key).ToList();
            foreach (var k in keysToRemove)
            {
                e.Item.Fields.Remove(k);
            }
        }

        protected virtual void OnIndexerExecutiveAssigned(IndexerExecutiveAssignedEventArgs e)
        {
            if (IndexerExecutiveAssigned != null)
                IndexerExecutiveAssigned(this, e);
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

        #endregion

        #region Provider implementation

        /// <summary>
        /// Reindexes an item
        /// </summary>
        /// <param name="items">XML node to reindex</param>
        /// <param name="category">Type of index to use</param>
        public override void PerformIndexing(params IndexOperation[] items)
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
                switch (i.Operation)
                {
                    case IndexOperationType.Add:
                        //check if it is already in our index
                        var idResult = InternalSearcher.Search(InternalSearcher.CreateSearchCriteria().Id(i.Item.Id).Compile());
                        if (idResult.Any())
                        {
                            //TODO: We should add an 'Update' instead of deleting first, would be much faster
                            //first add a delete queue for this item
                            buffer.Add(CreateDeleteItemOperation(i.Item.Id));
                        }
                        //now check if it is already in our queue, in which case we want to ignore 
                        //the previous ones.
                        buffer.RemoveAll(x => x.Item.Id == i.Item.Id && x.Operation == IndexOperationType.Add);

                        //ensure the special fields are added to the dictionary to be saved to file
                        EnsureSpecialFields(i.Item);
                        buffer.Add(i);
                        break;
                    case IndexOperationType.Delete:
                        buffer.Add(i);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            //we should now have a deduplicated list to create a queue from

            //run the indexer on all queued files
            SafelyProcessQueueItems(new Queue<IndexOperation>(buffer));
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

        #endregion

        #region Protected


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
        /// This can be an expensive operation and should only be called when there is no indexing activity, 
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
                    if (!_isIndexing)
                    {
                        IndexWriter writer = null;
                        try
                        {
                            if (!IndexExists())
                                return;

                            //check if the index is ready to be written to.
                            if (!IndexReady())
                            {
                                OnIndexingError(new IndexingErrorEventArgs("Cannot optimize index, the index is currently locked", string.Empty, null));
                                _isIndexing = false;
                                return;
                            }

                            OnIndexOptimizing(new EventArgs());

                            //first, lets remove any duplicates that might exist
                            var reader = IndexReader.Open(LuceneDirectory, false);
                            var theTerms = reader.Terms(new Term(IndexNodeIdFieldName));
                            do
                            {
                                var term = theTerms.Term();

                                if ((term == null) || term.Field().ToUpper() != IndexNodeIdFieldName.ToUpper())
                                {
                                    break;
                                }

                                if (theTerms.DocFreq() > 1)
                                {
                                    var allDocs = new List<int>();
                                    var timeStampDocs = new Dictionary<int, long>();
                                    var td = reader.TermDocs(term);
                                    while(td.Next())
                                    {
                                        allDocs.Add(td.Doc());
                                    }
                                    foreach(var d in allDocs)
                                    {
                                        var doc = reader.Document(d);
                                        var timeStamp = doc.Get(IndexItemTimeStamp);
                                        if (!string.IsNullOrEmpty(timeStamp))
                                        {
                                            long realStamp;
                                            if (long.TryParse(timeStamp, out realStamp))
                                            {
                                                timeStampDocs.Add(d, realStamp);
                                            }
                                            
                                        }
                                    }
                                    //now that we have all of the timestamped docs, we can delete the old ones,
                                    //find the latest and remove it from the dictionary
                                    var maxStamp = timeStampDocs.First(latest => latest.Value == timeStampDocs.Max(x => x.Value));
                                    timeStampDocs.Remove(maxStamp.Key);
                                    foreach (var d in timeStampDocs)
                                    {
                                        reader.DeleteDocument(d.Key);
                                    } 
                                }
                            }
                            while (theTerms.Next());

                            //close the reader
                            reader.Close();

                            //open the writer for optization
                            writer = new IndexWriter(LuceneDirectory, IndexingAnalyzer, !IndexExists(), IndexWriter.MaxFieldLength.UNLIMITED);

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
                            CloseWriter(ref writer);

                            _isIndexing = false;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Removes the specified term from the index
        /// </summary>
        /// <param name="indexTerm"></param>
        /// <param name="iw"></param>
        /// <returns>Boolean if it successfully deleted the term, or there were on errors</returns>
        protected bool DeleteFromIndex(Term indexTerm, IndexWriter iw)
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

                iw.DeleteDocuments(indexTerm);

                OnIndexDeleted(new DeleteIndexEventArgs(new KeyValuePair<string, string>(indexTerm.Field(), indexTerm.Text())));
                return true;
            }
            catch (Exception ee)
            {
                OnIndexingError(new IndexingErrorEventArgs("Error deleting Lucene index", nodeId, ee));
                return false;
            }
        }

        /// <summary>
        /// Determines the indexing policy for the field specified, by default unless thsi method is overridden, all fields will be "Analyzed"
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="indexCategory"></param>
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
        /// <param name="item"></param>
        /// <param name="writer">The writer that will be used to update the Lucene index.</param>
        /// <remarks>
        /// This will normalize (lowercase) all text before it goes in to the index.
        /// </remarks>
        protected virtual void AddDocument(IndexItem item, IndexWriter writer)
        {
            var args = new IndexingNodeEventArgs(item);
            OnNodeIndexing(args);
            if (args.Cancel)
                return;

            var d = new Document();

            //add all of our fields to the document index individually, don't include the special fields if they exists            
            var validFields = item.Fields.Where(x => !x.Key.StartsWith(SpecialFieldPrefix)).ToList();

            foreach (var f in validFields)
            {
                var ourPolicyType = GetPolicy(f.Key, item.Fields[IndexCategoryFieldName].FieldValue);
                var lucenePolicy = TranslateFieldIndexTypeToLuceneType(ourPolicyType);

                Fieldable field = null;
                object parsedVal = null;

                switch (f.Value.DataType)
                {
                    case FieldDataType.Number:
                    case FieldDataType.Int:
                        if (!TryConvert<int>(f.Value.FieldValue, out parsedVal))
                            break;
                        field = new NumericField(f.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetIntValue((int)parsedVal);
                        break;
                    case FieldDataType.Float:
                        if (!TryConvert<float>(f.Value.FieldValue, out parsedVal))
                            break;
                        field = new NumericField(f.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetFloatValue((float)parsedVal);
                        break;
                    case FieldDataType.Double:
                        if (!TryConvert<double>(f.Value.FieldValue, out parsedVal))
                            break;
                        field = new NumericField(f.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetDoubleValue((double)parsedVal);
                        break;
                    case FieldDataType.Long:
                        if (!TryConvert<long>(f.Value.FieldValue, out parsedVal))
                            break;
                        field = new NumericField(f.Key, Field.Store.YES, lucenePolicy != Field.Index.NO).SetLongValue((long)parsedVal);
                        break;
                    case FieldDataType.DateTime:
                        {
                            if (!TryConvert<DateTime>(f.Value.FieldValue, out parsedVal))
                                break;

                            DateTime date = (DateTime)parsedVal;
                            string dateAsString = DateTools.DateToString(date, DateTools.Resolution.MILLISECOND);
                            field = new Field(f.Key,
                                dateAsString,
                                Field.Store.YES,
                                lucenePolicy,
                                lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                            );

                            break;
                        }
                    case FieldDataType.DateYear:
                        {
                            if (!TryConvert<DateTime>(f.Value.FieldValue, out parsedVal))
                                break;

                            DateTime date = (DateTime)parsedVal;
                            string dateAsString = DateTools.DateToString(date, DateTools.Resolution.YEAR);
                            field = new Field(f.Key,
                                dateAsString,
                                Field.Store.YES,
                                lucenePolicy,
                                lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                            );

                            break;
                        }
                    case FieldDataType.DateMonth:
                        {
                            if (!TryConvert<DateTime>(f.Value.FieldValue, out parsedVal))
                                break;

                            DateTime date = (DateTime)parsedVal;
                            string dateAsString = DateTools.DateToString(date, DateTools.Resolution.MONTH);
                            field = new Field(f.Key,
                                dateAsString,
                                Field.Store.YES,
                                lucenePolicy,
                                lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                            );

                            break;
                        }
                    case FieldDataType.DateDay:
                        {
                            if (!TryConvert<DateTime>(f.Value.FieldValue, out parsedVal))
                                break;

                            DateTime date = (DateTime)parsedVal;
                            string dateAsString = DateTools.DateToString(date, DateTools.Resolution.DAY);
                            field = new Field(f.Key,
                                dateAsString,
                                Field.Store.YES,
                                lucenePolicy,
                                lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                            );
                            break;
                        }
                    case FieldDataType.DateHour:
                        {
                            if (!TryConvert<DateTime>(f.Value.FieldValue, out parsedVal))
                                break;

                            DateTime date = (DateTime)parsedVal;
                            string dateAsString = DateTools.DateToString(date, DateTools.Resolution.HOUR);
                            field = new Field(f.Key,
                                dateAsString,
                                Field.Store.YES,
                                lucenePolicy,
                                lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                            );
                            break;
                        }
                    case FieldDataType.DateMinute:
                        {
                            if (!TryConvert<DateTime>(f.Value.FieldValue, out parsedVal))
                                break;

                            DateTime date = (DateTime)parsedVal;
                            string dateAsString = DateTools.DateToString(date, DateTools.Resolution.MINUTE);
                            field = new Field(f.Key,
                                dateAsString,
                                Field.Store.YES,
                                lucenePolicy,
                                lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                            );
                            break;
                        }
                    default:
                        field = new Field(f.Key,
                                f.Value.FieldValue,
                                Field.Store.YES,
                                lucenePolicy,
                                lucenePolicy == Field.Index.NO ? Field.TermVector.NO : Field.TermVector.YES
                            );
                        break;
                }

                //if the parsed value is null, this means it couldn't parse and we should log this error
                if (field == null)
                {
                    OnIndexingError(new IndexingErrorEventArgs("Could not parse value: " + f.Value + "into the type: " + f.Value.DataType, item.Id, null));
                }
                else
                {
                    d.Add(field);

                    if (f.Value.EnableSorting)
                    {
                        d.Add(new Field(SortedFieldNamePrefix + f.Key,
                                f.Value.FieldValue,
                                Field.Store.YES,
                                Field.Index.NOT_ANALYZED,
                                Field.TermVector.NO
                                ));
                    }
                }

            }



            AddSpecialFieldsToDocument(d, item.Fields);

            var docArgs = new DocumentWritingEventArgs(d, item);
            OnDocumentWriting(docArgs);
            if (docArgs.Cancel)
                return;

            writer.AddDocument(d);

            OnNodeIndexed(new IndexedNodeEventArgs(item));
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
        protected virtual IDictionary<string, ItemField> GetSpecialFieldsToIndex(IDictionary<string, ItemField> allValuesForIndexing)
        {
            return new Dictionary<string, ItemField>() 
			{
				//we want to store the nodeId separately as it's the index
				{IndexNodeIdFieldName, allValuesForIndexing[IndexNodeIdFieldName]},
				//add the index type first
				{IndexCategoryFieldName, allValuesForIndexing[IndexCategoryFieldName]},
                //add the timestamp
				{IndexItemTimeStamp, new ItemField(DateTime.UtcNow.Ticks.ToString())}
			};
        }


        /// <summary>
        /// Process all of the queue items. This checks if this machine is the Executive and if it's in a load balanced
        /// environments. If then acts accordingly: 
        ///     Not the executive = doesn't index, i
        ///     In async mode = use file watcher timer
        /// </summary>
        protected internal void SafelyProcessQueueItems(Queue<IndexOperation> buffer)
        {
            //if this is not the master indexer, exit
            if (!ExecutiveIndex.IsExecutiveMachine)
                return;

            //if in async mode, then process the queue using the timer
            switch (SynchronizationType)
            {
                case SynchronizationType.SingleThreaded:
                    //add the buffer to the queue
                    var list = new ConcurrentQueue<IndexOperation>(buffer);
                    ForceProcessQueueItems(list);
                    //if there are enough commits, then we'll run an optimization
                    if (CommitCount >= OptimizationCommitThreshold)
                    {
                        OptimizeIndex();
                        CommitCount = 0; //reset the counter
                    }
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

            if (!_isIndexing)
            {
                lock (_indexerLocker)
                {
                    if (!_isIndexing)
                    {
                        _isIndexing = true;

                        //check if the index is ready to be written to.
                        if (!IndexReady())
                        {
                            OnIndexingError(new IndexingErrorEventArgs("Cannot index queue items, the index is currently locked", string.Empty, null));
                            return 0;
                        }

                        //wrap in array because resharper told me to because of access to modified closure
                        IndexWriter[] writer = { GetIndexWriter() };

                        //track all of the nodes indexed
                        var indexedNodes = new ConcurrentBag<IndexItem>();

                        try
                        {

                            ////iterate over the concurrent queue
                            //Parallel.For(0, buffer.Count, x =>
                            //{
                                //iterate through the items in the buffer, they should be in the exact order in which 
                                //they were added so shouldn't need to sort anything

                                //we need to iterate like this because our threadsafe list doesn't allow enumeration
                                IndexOperation item;
                                while (buffer.TryDequeue(out item))
                                {
                                    Console.WriteLine("Indexing : " + item.Item.Id + " op = " + item.Operation.ToString());

                                    switch (item.Operation)
                                    {
                                        case IndexOperationType.Add:
                                            ProcessAddQueueItem(item.Item, writer[0]);
                                            indexedNodes.Add(item.Item);
                                            break;
                                        case IndexOperationType.Delete:
                                            ProcessDeleteQueueItem(item.Item, writer[0]);
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                }
                            //});

                            writer[0].Commit(); //commit changes!
                            writer[0].WaitForMerges(); //wait until commits are done

                            //raise the completed event
                            OnNodesIndexed(new IndexedNodesEventArgs(indexedNodes));

                        }
                        catch (Exception ex)
                        {
                            OnIndexingError(new IndexingErrorEventArgs("Error indexing queue items", string.Empty, ex));
                        }
                        finally
                        {
                            CloseWriter(ref writer[0]);

                            _isIndexing = false;
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


        #endregion

        #region Private

        private void EnsureSpecialFields(IndexItem item)
        {
            //ensure the special fields are added to the dictionary to be saved to file
            if (!item.Fields.ContainsKey(IndexNodeIdFieldName))
                item.Fields.Add(IndexNodeIdFieldName, new ItemField(item.Id));
            if (!item.Fields.ContainsKey(IndexCategoryFieldName))
                item.Fields.Add(IndexCategoryFieldName, new ItemField(item.ItemCategory));
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
        private void AddSpecialFieldsToDocument(Document d, IDictionary<string, ItemField> fields)
        {
            var specialFields = GetSpecialFieldsToIndex(fields);

            foreach (var s in specialFields)
            {
                //TODO: we're going to lower case the special fields, the Standard analyzer query parser always lower cases, so 
                //we need to do that... there might be a nicer way ?
                d.Add(new Field(s.Key, s.Value.FieldValue.ToLower(), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS, Field.TermVector.NO));
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

        private void InitializeBackgroundWorker(Queue<IndexOperation> buffer)
        {

            //if this is not the master indexer anymore... perhaps another server has taken over somehow...
            if (!ExecutiveIndex.IsExecutiveMachine)
            {
                //this will abort the thread once it's latest processing has stopped.
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

                return;
            }

            //re-index everything in the buffer, add everything safely to our threadsafe queue
            while (buffer.Count > 0)
            {
                _asyncQueue.Enqueue(buffer.Dequeue());
            }

            //don't run the worker if it's currently running since it will just pick up the rest of the queue during its normal operation
            if (!_isIndexing && (_asyncTask == null || _asyncTask.IsCompleted))
            {
                _asyncTask = Task.Factory.StartNew(WorkerThreadDoWork);
            }

        }

        /// <summary>
        /// Uses a background worker thread to do all of the indexing
        /// </summary>
        void WorkerThreadDoWork()
        {
            //keep processing until it is complete
            var numProcessedItems = 0;
            do
            {
                numProcessedItems = ForceProcessQueueItems(_asyncQueue);
            } while (!_isCancelling && numProcessedItems > 0);

            //if there are enough commits, then we'll run an optimization
            if (CommitCount >= OptimizationCommitThreshold)
            {
                OptimizeIndex();
                CommitCount = 0; //reset the counter
            }

        }

        /// <summary>
        /// Returns a Lucene index writer        
        /// </summary>        
        /// <returns></returns>
        private IndexWriter GetIndexWriter()
        {
            return new IndexWriter(LuceneDirectory, IndexingAnalyzer, false, IndexWriter.MaxFieldLength.UNLIMITED);
        }

        /// <summary>
        /// Reads the FileInfo passed in into a dictionary object and deletes it from the index
        /// </summary>
        /// <param name="x"></param>
        /// <param name="iw"></param>
        private void ProcessDeleteQueueItem(IndexItem x, IndexWriter iw)
        {
            //we know that there's only ever one item saved to the dictionary for deletions
            if (x.Fields.Count != 1)
            {
                OnIndexingError(new IndexingErrorEventArgs("Could not remove queue item from index, the dictionary is not properly formatted", string.Empty, null));
                return;
            }
            var term = x.Fields.First();
            DeleteFromIndex(new Term(term.Key, term.Value.FieldValue), iw);

            CommitCount++;
        }

        /// <summary>
        /// Reads the FileInfo passed in into a dictionary object and adds it to the index
        /// </summary>
        /// <param name="x"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        private void ProcessAddQueueItem(IndexItem x, IndexWriter writer)
        {

            //now, add the index with our dictionary object
            AddDocument(x, writer);

            CommitCount++;
        }

        private void CloseWriter(ref IndexWriter writer)
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;
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

        protected override void DisposeResources()
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

            InternalSearcher.Dispose();
            LuceneDirectory.Close();

        }

        #endregion

    }
}
