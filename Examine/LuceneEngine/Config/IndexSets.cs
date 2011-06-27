using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Web;

namespace Examine.LuceneEngine.Config
{
    /// <summary>
    /// Defines XPath statements that map to specific umbraco nodes
    /// </summary>
    public class IndexSets : ConfigurationSection
    {

        #region Singleton definition

        private static readonly IndexSets _indexSets;
        protected IndexSets() { }
        static IndexSets()
        {
            _indexSets = ConfigurationManager.GetSection(SectionName) as IndexSets;     
  
        }
        public static IndexSets Instance
        {
            get { return _indexSets; }
        }

        #endregion

        private const string SectionName = "examineLuceneIndexSets";

        [ConfigurationCollection(typeof(IndexSetCollection))]
        [ConfigurationProperty("", IsDefaultCollection = true, IsRequired = true)]
        public IndexSetCollection Sets
        {
            get
            {
                return (IndexSetCollection)base[""];
            }
        }
                
    }

    
    
}