using System.Collections.Generic;

namespace GitSpect.Cmd
{
    internal class Tree : GitObject
    {
        public Tree() : base()
        {
            Type = GitObjects.Tree;
        }

        public override string ToString()
        {
            string prettyPrint;



            return prettyPrint;
        }

        public List<TreeInternalData> Blobs { get; internal set; }
        public List<TreeInternalData> Trees { get; internal set; }
    }
}