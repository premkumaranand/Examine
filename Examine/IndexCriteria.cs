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
        ///<param name="parentNodeId"></param>
        public IndexCriteria(IEnumerable<IIndexFieldDefinition> fields, 
            IEnumerable<string> includeNodeTypes, 
            IEnumerable<string> excludeNodeTypes, 
            string parentNodeId)
        {
            if (fields == null) fields = Enumerable.Empty<IIndexFieldDefinition>();
            if (includeNodeTypes == null) includeNodeTypes = Enumerable.Empty<string>();
            if (excludeNodeTypes == null) excludeNodeTypes = Enumerable.Empty<string>();
            if (parentNodeId == null) parentNodeId = string.Empty;

            Fields = fields.ToList();
            IncludeItemTypes = includeNodeTypes;
            ExcludeItemTypes = excludeNodeTypes;
            ParentId = parentNodeId;
        }

        public IEnumerable<IIndexFieldDefinition> Fields { get; internal set; }

        /// <summary>
        /// Gets the include item types.
        /// </summary>
        public IEnumerable<string> IncludeItemTypes { get; internal set; }

        /// <summary>
        /// Gets the exclude item types.
        /// </summary>
        public IEnumerable<string> ExcludeItemTypes { get; internal set; }

        public string ParentId { get; internal set; }
    }

    
}
