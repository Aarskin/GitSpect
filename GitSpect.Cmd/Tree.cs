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
            var baseString = base.ToString();
            StringBuilder prettyPrint = new StringBuilder(baseString);

            foreach (var blob in Blobs)
            {
                string line = string.Format("Blob: {0}", blob.FileName);
                prettyPrint.AppendLine(line);
            }

            foreach (var tree in Trees)
            {
                string line = string.Format("Tree: {0}", tree.FileName);
                prettyPrint.AppendLine(line);
            }

            prettyPrint.AppendLine();

            return prettyPrint.ToString();
        }

        public List<TreeInternalData> Blobs { get; internal set; }
        public List<TreeInternalData> Trees { get; internal set; }
    }
}