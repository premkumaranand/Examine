using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Examine;
using Examine.SearchCriteria;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Examine.LuceneEngine;
using Examine.LuceneEngine.Config;
using Examine.LuceneEngine.SearchCriteria;
using Lucene.Net.Analysis;
using Directory = Lucene.Net.Store.Directory;


namespace Examine.LuceneEngine.Providers
{
    ///<summary>
	/// Standard object used to search a Lucene index
	///</summary>
    public class LuceneSearcher : BaseLuceneSearcher
	{
		#region Constructors

		/// <summary>
		/// Default constructor for use with Provider implementation
		/// </summary>
        public LuceneSearcher()
            : this(IndexSets.GetDefaultInstance())
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneSearcher"/> class.
        /// </summary>
        /// <param name="indexSetConfig">The index set config.</param>
        public LuceneSearcher(IEnumerable<IndexSet> indexSetConfig)
        {
            IndexSetConfiguration = indexSetConfig;
        }

        /// <summary>
        /// Constructor to allow for creating an indexer at runtime
        /// </summary>
        /// <param name="indexFolder"></param>
        /// <param name="analyzer"></param>
        public LuceneSearcher(DirectoryInfo indexFolder, Analyzer analyzer)
            : base(analyzer)
		{
            LuceneDirectory = new SimpleFSDirectory(indexFolder);
		}

        /// <summary>
        /// Constructor to allow for creating an indexer at runtime using the specified Lucene 'Directory'
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="luceneDirectory"></param>
        public LuceneSearcher(Analyzer analyzer, Directory luceneDirectory)
            : base(analyzer)
        {
            LuceneDirectory = luceneDirectory;
        }

		#endregion

		/// <summary>
		/// Used as a singleton instance
		/// </summary>
		private IndexSearcher _searcher;
		private static readonly object Locker = new object();

		/// <summary>
		/// Initializes the provider.
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

			//need to check if the index set is specified, if it's not, we'll see if we can find one by convension
			//if the folder is not null and the index set is null, we'll assume that this has been created at runtime.
			if (config["indexSet"] == null)
			{
				//if we don't have either, then we'll try to set the index set by naming convensions
				var found = false;
				if (name.EndsWith("Searcher"))
				{
					var setNameByConvension = name.Remove(name.LastIndexOf("Searcher")) + "IndexSet";
					//check if we can assign the index set by naming convension
				    var set = IndexSetConfiguration.SingleOrDefault(x => x.SetName == setNameByConvension);

					if (set != null)
					{
						//we've found an index set by naming convensions :)
						_indexSetName = set.SetName;
						found = true;

                        //get the folder to index
					    LuceneDirectory = new SimpleFSDirectory(new DirectoryInfo(Path.Combine(set.IndexDirectory.FullName, "Index")));
					}
				}

				if (!found)
					throw new ArgumentNullException("indexSet on LuceneExamineIndexer provider has not been set in configuration");
			}
			else
			{
                var set = IndexSetConfiguration.SingleOrDefault(x => x.SetName == config["indexSet"]);
                if (set == null)
					throw new ArgumentException("The indexSet specified for the LuceneExamineIndexer provider does not exist");

				_indexSetName = config["indexSet"];

				//get the folder to index
                LuceneDirectory = new SimpleFSDirectory(new DirectoryInfo(Path.Combine(set.IndexDirectory.FullName, "Index")));
			}            		
		}

        /// <summary>
        /// Gets or sets the index set configuration.
        /// </summary>
        /// <value>
        /// The index set configuration.
        /// </value>
        protected IEnumerable<IndexSet> IndexSetConfiguration { get; set; }

        /// <summary>
        /// The Lucene 'Directory' of where the index is stored
        /// </summary>
        public Directory LuceneDirectory { get; protected set; }

        
        /// <summary>
        /// A simple search mechanism to search all fields based on an index type.
        /// </summary>
        /// <remarks>
        /// This can be used to do a simple search against an index type instead of the entire index.
        /// </remarks>
        /// <param name="searchText"></param>
        /// <param name="useWildcards"></param>
        /// <param name="indexType"></param>
        /// <returns></returns>
        public ISearchResults Search(string searchText, bool useWildcards, string indexType)
        {
            var sc = CreateSearchCriteria(indexType);

            if (useWildcards)
            {
                var wildcardSearch = new ExamineValue(Examineness.ComplexWildcard, searchText.MultipleCharacterWildcard().Value);
                sc = sc.GroupedOr(GetSearchFields(), wildcardSearch).Compile();
            }
            else
            {
                sc = sc.GroupedOr(GetSearchFields(), searchText).Compile();
            }

            return Search(sc);
        }

        /// <summary>
        /// Searches the data source using the Examine Fluent API
        /// </summary>
        /// <param name="searchParams">The fluent API search.</param>
        /// <returns></returns>
        public override ISearchResults Search(ISearchCriteria searchParams)
        {
            if (!IndexExists())
                throw new DirectoryNotFoundException("The index does not exist. Ensure that an index has been created");

            return base.Search(searchParams);
        }

        /// <summary>
        /// Name of the Lucene.NET index set
        /// </summary>
        private string _indexSetName;

        /// <summary>
        /// Gets the searcher for this instance
        /// </summary>
        /// <returns></returns>
        public override Searcher GetSearcher()
        {
            ValidateSearcher(false);

            //ensure scoring is turned on for sorting
            _searcher.SetDefaultFieldSortScoring(true, true);
            return _searcher;
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
        /// Returns a list of fields to search on
        /// </summary>
        /// <returns></returns>
        protected override internal string[] GetSearchFields()
        {
            ValidateSearcher(false);

            var reader = _searcher.GetIndexReader();
            var fields = reader.GetFieldNames(IndexReader.FieldOption.ALL);
            //exclude the special index fields
            var searchFields = fields
                .Where(x => !x.StartsWith(LuceneIndexer.SpecialFieldPrefix))
                .ToArray();
            return searchFields;
        }

        /// <summary>
        /// This checks if the singleton IndexSearcher is initialized and up to date.
        /// </summary>
        /// <param name="forceReopen"></param>
        internal protected void ValidateSearcher(bool forceReopen)
        {
            if (!forceReopen)
            {
                if (_searcher == null)
                {
                    lock (Locker)
                    {
                        //double check
                        if (_searcher == null)
                        {
                            try
                            {
                                _searcher = new IndexSearcher(LuceneDirectory, true);
                            }
                            catch (IOException ex)
                            {
                                throw new ApplicationException("There is no Lucene index in the specified folder, cannot create a searcher", ex);
                            }
                        }
                    }
                }
                else
                {
                    if (_searcher.GetReaderStatus() != ReaderStatus.Current)
                    {
                        lock (Locker)
                        {
                            //double check, now, we need to find out if it's closed or just not current
                            switch (_searcher.GetReaderStatus())
                            {
                                case ReaderStatus.Current:
                                    break;
                                case ReaderStatus.Closed:
                                    _searcher = new IndexSearcher(LuceneDirectory, true);
                                    break;
                                case ReaderStatus.NotCurrent:

                                    //yes, this is actually the way the Lucene wants you to work...
                                    //normally, i would have thought just calling Reopen() on the underlying reader would suffice... but it doesn't.
                                    //here's references: 
                                    // http://stackoverflow.com/questions/1323779/lucene-indexreader-reopen-doesnt-seem-to-work-correctly
                                    // http://gist.github.com/173978 

                                    var oldReader = _searcher.GetIndexReader();
                                    var newReader = oldReader.Reopen(true);
                                    if (newReader != oldReader)
                                    {
                                        _searcher.Close();
                                        oldReader.Close();
                                        _searcher = new IndexSearcher(newReader);
                                    }

                                    break;
                            }
                        }
                    }
                }
            }
            else
            {
                //need to close the searcher and force a re-open

                if (_searcher != null)
                {
                    lock (Locker)
                    {
                        //double check
                        if (_searcher != null)
                        {
                            try
                            {
                                _searcher.Close();
                            }
                            catch (IOException)
                            {
                                //this will happen if it's already closed ( i think )
                            }
                            finally
                            {
                                //set to null in case another call to this method has passed the first lock and is checking for null
                                _searcher = null;
                            }


                            try
                            {
                                _searcher = new IndexSearcher(LuceneDirectory, true);
                            }
                            catch (IOException ex)
                            {
                                throw new ApplicationException("There is no Lucene index in the specified folder, cannot create a searcher", ex);
                            }
                            
                        }
                    }
                }
            }

        }

	}
}
