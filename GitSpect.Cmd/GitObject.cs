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
            string shaLine = string.Format("Object ID (SHA): {0}", SHA);
            string typeAndSizeLine = string.Format("Type: {0} Size: {1}", Type, Size.ToString("D5"));

            prettyPrint.AppendLine(shaLine);
            prettyPrint.AppendLine(typeAndSizeLine);
            prettyPrint.AppendLine();

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
        MergeCommit
    }
}