using System.Collections.Generic;
using System.Text;

namespace GitSpect.Cmd
{
    public abstract class GitObject
    {
        public GitObject()
        {
            RefShas = new List<string>();
        }

        public override string ToString()
        {
            StringBuilder prettyPrint = new StringBuilder();
            string typeLine = string.Format("Type: {0}", Type);
            string sizeLine = string.Format("Size: {0}", Size.ToString("D5"));
            string shaLine = string.Format("Object ID (SHA): {0}", SHA);

            prettyPrint.AppendLine(typeLine);
            prettyPrint.AppendLine(sizeLine);
            prettyPrint.AppendLine(shaLine);

            return prettyPrint.ToString();
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
        MergeCommit,
        Unknown
    }
}