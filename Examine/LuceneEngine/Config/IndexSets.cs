using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Web;

namespace Examine.LuceneEngine.Config
{

    /// <summary>
    /// Defines an Index set
    /// </summary>
    public class IndexSets : ConfigurationSection, IEnumerable<IndexSet>
    {
        private const string SectionName = "examine.indexes";
       
        ///<summary>
        /// Returns a default instance of the config
        ///</summary>
        ///<returns></returns>
        public static IndexSets GetDefaultInstance()
        {
            return ConfigurationManager.GetSection(SectionName) as IndexSets;  
        }

        /// <summary>
        /// Gets the sets.
        /// </summary>
        [ConfigurationCollection(typeof(IndexSetCollection))]
        [ConfigurationProperty("", IsDefaultCollection = true, IsRequired = true)]
        public IndexSetCollection Sets
        {
            get
            {
                return (IndexSetCollection)base[""];
            }
        }

        public IEnumerator<IndexSet> GetEnumerator()
        {
            return Sets.Cast<IndexSet>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Sets.GetEnumerator();
        }
    }

    
    
}