using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Linq;
using System.Web.Configuration;
using System.Xml.Linq;
using Examine.Config;
using Examine.Providers;
using Examine.SearchCriteria;
using System.Web;

namespace Examine
{
    ///<summary>
    /// Exposes searchers and indexers via the providers configuration
    ///</summary>
    public class ExamineManager : ISearcher, IIndexer
    {
        private readonly ExamineSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExamineManager"/> class using the default configuration/providers
        /// </summary>
        public ExamineManager()
        {
            _settings = ExamineSettings.GetDefaultInstance();
            LoadProviders();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExamineManager"/> class using the specified provider's configuration
        /// </summary>
        /// <param name="settings">The settings.</param>
        public ExamineManager(ExamineSettings settings)
        {
            _settings = settings;
            LoadProviders();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExamineManager"/> class with the specified pre-configured searchers/indexers
        /// </summary>
        /// <param name="searchers">The searchers.</param>
        /// <param name="indexers">The indexers.</param>
        /// <param name="defaultSearcher"></param>
        public ExamineManager(IEnumerable<ISearcher> searchers, IEnumerable<IIndexer> indexers, ISearcher defaultSearcher)
        {
            SearchProviderCollection = searchers;
            IndexProviderCollection = indexers;
            DefaultSearchProvider = defaultSearcher;
        }

        private readonly object _lock = new object();

        ///<summary>
        /// Returns the default search provider
        ///</summary>
        public ISearcher DefaultSearchProvider { get; private set; }

        /// <summary>
        /// Returns the collection of searchers
        /// </summary>
        public IEnumerable<ISearcher> SearchProviderCollection { get; private set; }

        /// <summary>
        /// Return the colleciton of indexers
        /// </summary>
        public IEnumerable<IIndexer> IndexProviderCollection { get; private set; }

        /// <summary>
        /// Loads the providers from the config
        /// </summary>
        private void LoadProviders()
        {
            if (IndexProviderCollection == null)
            {
                lock (_lock)
                {
                    // Do this again to make sure _provider is still null
                    if (IndexProviderCollection == null)
                    {

                        // Load registered providers and point _provider to the default provider	

                        var indexProviderCollection = new IndexProviderCollection();
                        ProvidersHelper.InstantiateProviders(_settings.IndexProviders.Providers, indexProviderCollection, typeof(BaseIndexProvider));
                        IndexProviderCollection = indexProviderCollection.Cast<IIndexer>();

                        var searchProviderCollection = new SearchProviderCollection();
                        ProvidersHelper.InstantiateProviders(_settings.SearchProviders.Providers, searchProviderCollection, typeof(BaseSearchProvider));
                        SearchProviderCollection = searchProviderCollection.Cast<ISearcher>();

                        //set the default
                        if (!string.IsNullOrEmpty(_settings.SearchProviders.DefaultProvider))
                            DefaultSearchProvider = searchProviderCollection[_settings.SearchProviders.DefaultProvider];

                        if (DefaultSearchProvider == null)
                            throw new ProviderException("Unable to load default search provider");

                    }
                }
            }
        }

     

        #region IIndexer Members

        /// <summary>
        /// Reindex nodes for the providers specified
        /// </summary>
        /// <param name="items"></param>
        /// <param name="providers"></param>
        public void PerformIndexing(IndexOperation[] items, IEnumerable<IIndexer> providers)
        {
            ReIndexNodesForProviders(items, providers);
        }
  
        /// <summary>
        /// Reindex nodes for all providers
        /// </summary>
        public void PerformIndexing(params IndexOperation[] item)
        {
            ReIndexNodesForProviders(item, IndexProviderCollection.Cast<IIndexer>());
        }

        private static void ReIndexNodesForProviders(IndexOperation[] items, IEnumerable<IIndexer> providers)
        {
            foreach (var provider in providers)
            {
                provider.PerformIndexing(items);
            }
        }      

        #endregion

        #region ISearcher Members

        /// <summary>
        /// Uses the default provider specified to search
        /// </summary>
        /// <param name="searchParameters"></param>
        /// <returns></returns>
        /// <remarks>This is just a wrapper for the default provider</remarks>
        public ISearchResults Search(IQuery searchParameters)
        {
            return DefaultSearchProvider.Search(searchParameters);
        }

        /// <summary>
        /// Uses the default provider specified to search
        /// </summary>
        /// <param name="searchText"></param>
        /// <param name="useWildcards"></param>
        /// <returns></returns>
        public ISearchResults Search(string searchText, bool useWildcards)
        {
            return DefaultSearchProvider.Search(searchText, useWildcards);
        }

        /// <summary>
        /// Creates search criteria that defaults to IndexType.Any and BooleanOperation.And
        /// </summary>
        /// <returns></returns>
        public IQuery CreateSearchCriteria()
        {
            return this.CreateSearchCriteria(string.Empty, BooleanOperation.And);
        }

        public IQuery CreateSearchCriteria(string category)
        {
            return this.CreateSearchCriteria(category, BooleanOperation.And);
        }

        public IQuery CreateSearchCriteria(BooleanOperation defaultOperation)
        {
            return this.CreateSearchCriteria(string.Empty, defaultOperation);
        }

        public IQuery CreateSearchCriteria(string category, BooleanOperation defaultOperation)
        {
            return this.DefaultSearchProvider.CreateSearchCriteria(category, defaultOperation);
        }

        #endregion

        #region IDisposable Members

        private bool _disposed = false;

        /// <summary>
        /// When the object is disposed, all data should be written
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    DisposeResources();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Disposes the indexers and searchers
        /// </summary>
        protected virtual void DisposeResources()
        {
            foreach(var i in this.IndexProviderCollection)
            {
                i.Dispose();
            }
            foreach(var s in this.SearchProviderCollection)
            {
                s.Dispose();
            }
        }

        #endregion
    }
}
