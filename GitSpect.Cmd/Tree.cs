using System.Collections.Generic;

namespace GitSpect.Cmd
{
    internal class Tree : GitObject
    {
        public Tree()
        {
            Type = GitObjects.Tree;
        }

        public List<TreeInternalData> Blobs { get; internal set; }
        public List<TreeInternalData> Trees { get; internal set; }
    }
}