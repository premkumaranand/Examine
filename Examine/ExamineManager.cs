using System;
using System.Collections.Generic;
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
    /// Exposes searchers and indexers
    ///</summary>
    public class ExamineManager : ISearcher, IIndexer
    {
        private readonly ExamineSettings _settings;

        public ExamineManager()
        {
            _settings = ExamineSettings.GetDefaultInstance();
            LoadProviders();
        }

        public ExamineManager(ExamineSettings settings)
        {
            _settings = settings;
        }

        private readonly object _lock = new object();

        ///<summary>
        /// Returns the default search provider
        ///</summary>
        public BaseSearchProvider DefaultSearchProvider { get; private set; }

        /// <summary>
        /// Returns the collection of searchers
        /// </summary>
        public SearchProviderCollection SearchProviderCollection { get; private set; }

        /// <summary>
        /// Return the colleciton of indexers
        /// </summary>
        public IndexProviderCollection IndexProviderCollection { get; private set; }

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

                        IndexProviderCollection = new IndexProviderCollection();
                        ProvidersHelper.InstantiateProviders(_settings.IndexProviders.Providers, IndexProviderCollection, typeof(BaseIndexProvider));

                        SearchProviderCollection = new SearchProviderCollection();
                        ProvidersHelper.InstantiateProviders(_settings.SearchProviders.Providers, SearchProviderCollection, typeof(BaseSearchProvider));

                        //set the default
                        if (!string.IsNullOrEmpty(_settings.SearchProviders.DefaultProvider))
                             DefaultSearchProvider = SearchProviderCollection[_settings.SearchProviders.DefaultProvider];

                        if (DefaultSearchProvider == null)
                            throw new ProviderException("Unable to load default search provider");

                    }
                }
            }
        }

        /// <summary>
        /// Reindex nodes for the providers specified
        /// </summary>
        /// <param name="items"></param>
        /// <param name="indexCategory"></param>
        /// <param name="providers"></param>
        public void ReIndexNodes(IndexItem[] items, string indexCategory, IEnumerable<IIndexer> providers)
        {
            ReIndexNodesForProviders(items, indexCategory, providers);
        }

        /// <summary>
        /// Deletes index for node for the specified providers
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providers"></param>
        public void DeleteFromIndex(string id, IEnumerable<BaseIndexProvider> providers)
        {
            DeleteFromIndexForProviders(id, providers);
        }

        #region IIndexer Members

        /// <summary>
        /// Reindex nodes for all providers
        /// </summary>
        /// <param name="item"></param>
        /// <param name="indexCategory"></param>
        public void ReIndexNodes(string indexCategory, params IndexItem[] item)
        {
            ReIndexNodesForProviders(item, indexCategory, IndexProviderCollection.Cast<IIndexer>());
        }

        private static void ReIndexNodesForProviders(IndexItem[] items, string indexCategory, IEnumerable<IIndexer> providers)
        {
            foreach (var provider in providers)
            {
                provider.ReIndexNodes(indexCategory, items);
            }
        }

        /// <summary>
        /// Deletes index for node for all providers
        /// </summary>
        /// <param name="id"></param>
        public void DeleteFromIndex(string id)
        {
            DeleteFromIndexForProviders(id, IndexProviderCollection);
        }    

        private static void DeleteFromIndexForProviders(string nodeId, IEnumerable<BaseIndexProvider> providers)
        {
            foreach (var provider in providers)
            {
                provider.DeleteFromIndex(nodeId);
            }
        }

        public IndexCriteria IndexerData
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
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
        public ISearchResults Search(ISearchCriteria searchParameters)
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
        public ISearchCriteria CreateSearchCriteria()
        {
            return this.CreateSearchCriteria(string.Empty, BooleanOperation.And);
        }

        public ISearchCriteria CreateSearchCriteria(string type)
        {
            return this.CreateSearchCriteria(type, BooleanOperation.And);
        }

        public ISearchCriteria CreateSearchCriteria(BooleanOperation defaultOperation)
        {
            return this.CreateSearchCriteria(string.Empty, defaultOperation);
        }

        public ISearchCriteria CreateSearchCriteria(string type, BooleanOperation defaultOperation)
        {
            return this.DefaultSearchProvider.CreateSearchCriteria(type, defaultOperation);
        }

        #endregion
    }
}
