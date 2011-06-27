using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Examine.LuceneEngine.Config
{
    ///<summary>
    /// A configuration item representing a field to index
    ///</summary>
    public sealed class IndexFieldDefinition : ConfigurationElement, IIndexFieldDefinition
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("enableSorting", IsRequired = false)]
        public bool EnableSorting
        {
            get
            {
                return (bool)this["enableSorting"];
            }
            set
            {
                this["enableSorting"] = value;
            }
        }

        [ConfigurationProperty("dataType", IsRequired = false, DefaultValue = "String")]
        public string DataType
        {
            get
            {
                return (string)this["dataType"];
            }
            set
            {
                this["dataType"] = value;
            }
        }

        public override bool Equals(object compareTo)
        {
            if (compareTo is IndexFieldDefinition)
            {
                return this.Name.Equals(((IndexFieldDefinition)compareTo).Name);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}
