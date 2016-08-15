using System.Collections.Generic;
using System.Text;

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
            int i = 0;
            var baseString = base.ToString();
            StringBuilder prettyPrint = new StringBuilder(baseString);

            foreach (var blob in Blobs)
            {
                string line = string.Format("B{0} - Blob: {1}", i, blob.FileName);
                prettyPrint.AppendLine(line);
                i++;
            }

            i = 0;
            foreach (var tree in Trees)
            {
                string line = string.Format("T{0} - Tree: {1}", i, tree.FileName);
                prettyPrint.AppendLine(line);
                i++;
            }

            prettyPrint.AppendLine();

            return prettyPrint.ToString();
        }

        public List<TreeInternalData> Blobs { get; internal set; }
        public List<TreeInternalData> Trees { get; internal set; }
    }
}