using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examine
{
    public interface ISearchResults : IEnumerable<SearchResult>
    {
        /// <summary>
        /// Gets the total item count.
        /// </summary>
        int TotalItemCount { get; }

        /// <summary>
        /// Skips the specified skip.
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <returns></returns>
        IEnumerable<SearchResult> Skip(int skip);
    }
}
