using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Examine.LuceneEngine.Config
{
    public sealed class IndexFieldCollection : ConfigurationElementCollection
    {
        #region Overridden methods to define collection
        protected override ConfigurationElement CreateNewElement()
        {
            return new IndexFieldDefinition();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            IndexFieldDefinition field = (IndexFieldDefinition)element;
            return field.Name;
        }

        public override bool IsReadOnly()
        {
            return false;
        }
        #endregion

        /// <summary>
        /// Adds an index field to the collection
        /// </summary>
        /// <param name="field"></param>
        public void Add(IndexFieldDefinition field)
        {
            BaseAdd(field, true);
        }

        /// <summary>
        /// Default property for accessing an IndexField definition
        /// </summary>
        /// <value>Field Name</value>
        /// <returns></returns>
        public new IndexFieldDefinition this[string name]
        {
            get
            {
                return (IndexFieldDefinition)this.BaseGet(name);
            }
        }

    }
}
