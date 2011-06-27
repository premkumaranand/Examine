using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Examine.LuceneEngine.Config
{
    public static class IndexFieldCollectionExtensions
    {
        public static List<IndexFieldDefinition> ToList(this IndexFieldCollection indexes)
        {
            List<IndexFieldDefinition> fields = new List<IndexFieldDefinition>();
            foreach (IndexFieldDefinition field in indexes)
                fields.Add(field);
            return fields;
        }
    }
}
