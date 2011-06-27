using System.Configuration;
using System.IO;
using System.Web;
using System.Web.Hosting;

namespace Examine.LuceneEngine.Config
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class IndexSet : ConfigurationElement
    {

        /// <summary>
        /// Gets the name of the set.
        /// </summary>
        /// <value>
        /// The name of the set.
        /// </value>
        [ConfigurationProperty("setName", IsRequired = true, IsKey = true)]
        public string SetName
        {
            get
            {
                return (string)this["setName"];
            }
        }

        private string _indexPath = "";

        /// <summary>
        /// The folder path of where the lucene index is stored
        /// </summary>
        /// <value>The index path.</value>
        /// <remarks>
        /// This can be set at runtime but will not be persisted to the configuration file
        /// </remarks>
        [ConfigurationProperty("indexPath", IsRequired = true, IsKey = false)]
        public string IndexPath
        {
            get
            {
                if (string.IsNullOrEmpty(_indexPath))
                    _indexPath = (string)this["indexPath"];

                return _indexPath;
            }
            set
            {
                _indexPath = value;
            }
        }

        /// <summary>
        /// Returns the DirectoryInfo object for the index path.
        /// </summary>
        /// <value>The index directory.</value>
        public DirectoryInfo IndexDirectory
        {
            get
            {
                //TODO: Get this out of the index set. We need to use the Indexer's DataService to lookup the folder so it can be unit tested. Probably need DataServices on the searcher then too

                //we need to de-couple the context
                if (HttpContext.Current != null)
                    return new DirectoryInfo(HttpContext.Current.Server.MapPath(this.IndexPath));
                else if (HostingEnvironment.ApplicationID != null)
                    return new DirectoryInfo(HostingEnvironment.MapPath(this.IndexPath));
                else
                    return new DirectoryInfo(this.IndexPath);
            }
        }

        /// <summary>
        /// The collection of node types to index, if not specified, all node types will be indexed (apart from the ones specified in the ExcludeNodeTypes collection).
        /// </summary>
        [ConfigurationCollection(typeof(IndexFieldCollection))]
        [ConfigurationProperty("includeItemTypes", IsDefaultCollection = false, IsRequired = false)]
        public IndexFieldCollection IncludeItemTypes
        {
            get
            {
                return (IndexFieldCollection)base["includeItemTypes"];
            }
        }

        /// <summary>
        /// The collection of node types to not index. If specified, these node types will not be indexed.
        /// </summary>
        [ConfigurationCollection(typeof(IndexFieldCollection))]
        [ConfigurationProperty("excludeItemTypes", IsDefaultCollection = false, IsRequired = false)]
        public IndexFieldCollection ExcludeItemTypes
        {
            get
            {
                return (IndexFieldCollection)base["excludeItemTypes"];
            }
        }

        /// <summary>
        /// A collection of user defined umbraco fields to index
        /// </summary>
        /// <remarks>
        /// If this property is not specified, or if it's an empty collection, the default user fields will be all user fields defined in Umbraco
        /// </remarks>
        [ConfigurationCollection(typeof(IndexFieldCollection))]
        [ConfigurationProperty("fields", IsDefaultCollection = false, IsRequired = false)]
        public IndexFieldCollection Fields
        {
            get
            {
                return (IndexFieldCollection)base["fields"];
            }
        }

    }
}
