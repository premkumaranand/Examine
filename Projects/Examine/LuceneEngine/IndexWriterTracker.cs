using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security;
using Lucene.Net.Index;

namespace Examine.LuceneEngine
{
    /// <summary>
    /// Used to retrieve/track the same lucene directory instance for a given DirectoryInfo object
    /// </summary>
    [SecuritySafeCritical]
    public sealed class IndexWriterTracker
    {
        private static readonly IndexWriterTracker Instance = new IndexWriterTracker();

        private readonly ConcurrentDictionary<string, IndexWriter> _writers = new ConcurrentDictionary<string, IndexWriter>();

        public static IndexWriterTracker Current
        {
            get { return Instance; }
        }

        [SecuritySafeCritical]
        public IndexWriter GetWriter(Lucene.Net.Store.Directory dir, Func<string, IndexWriter> creator)
        {
            if (dir == null) throw new ArgumentNullException("dir");
            if (creator == null) throw new ArgumentNullException("creator");
            var resolved = _writers.GetOrAdd(dir.GetLockID(), creator);
            return resolved;
        }
    }
}