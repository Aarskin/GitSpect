using System.Collections.Generic;

namespace GitSpect.Cmd
{
    public abstract class GitObject
    {
        public GitObject()
        {
            RefShas = new List<string>();
        }

        public string SHA;
        public GitObjects Type;
        public int Size;
        
        public int RefCount { get; internal set; }
        public List<string> RefShas { get; internal set; }
    }

    public enum GitObjects
    {
        Blob,
        Tree,
        Commit,
        MergeCommit
    }
}