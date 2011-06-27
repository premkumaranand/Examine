using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Examine
{
    /// <summary>
    /// a data structure for storing indexing/searching instructions
    /// </summary>
    public class IndexCriteria
    {

        ///<summary>
        /// Constructor
        ///</summary>
        ///<param name="fields"></param>
        ///<param name="includeNodeTypes"></param>
        ///<param name="excludeNodeTypes"></param>
        public IndexCriteria(IEnumerable<IIndexFieldDefinition> fields, 
            IEnumerable<string> includeNodeTypes, 
            IEnumerable<string> excludeNodeTypes)
        {
            if (fields == null) fields = Enumerable.Empty<IIndexFieldDefinition>();
            if (includeNodeTypes == null) includeNodeTypes = Enumerable.Empty<string>();
            if (excludeNodeTypes == null) excludeNodeTypes = Enumerable.Empty<string>();
            
            Fields = fields.ToList();
            IncludeItemTypes = includeNodeTypes;
            ExcludeItemTypes = excludeNodeTypes;
        }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        public IEnumerable<IIndexFieldDefinition> Fields { get; internal set; }

        /// <summary>
        /// Gets the include item types.
        /// </summary>
        public IEnumerable<string> IncludeItemTypes { get; internal set; }

        /// <summary>
        /// Gets the exclude item types.
        /// </summary>
        public IEnumerable<string> ExcludeItemTypes { get; internal set; }

    }

    
}
