using System.Text;

namespace GitSpect.Cmd
{
    internal class MergeCommit : Commit
    {
        public MergeCommit() : base()
        {
            Type = GitObjects.MergeCommit;
        }

        public override string ToString()
        {
            string baseString = base.ToString();
            StringBuilder prettyPrint = new StringBuilder(baseString);

            string authorLine = string.Format("Author: {0}", Author);
            string committerLine = string.Format("Committer: {0}", Committer);
            string parentALine = string.Format("ParentA: {0}", ParentA);
            string parentBLine = string.Format("ParentB: {0}", ParentB);
            string treeLine = string.Format("Tree: {0}", Tree);
            string messageLine = string.Format("Message: {0}", Message);

            prettyPrint.AppendLine(authorLine);
            prettyPrint.AppendLine(committerLine);
            prettyPrint.AppendLine(parentALine);
            prettyPrint.AppendLine(parentBLine);
            prettyPrint.AppendLine(treeLine);
            prettyPrint.AppendLine(messageLine);
            prettyPrint.AppendLine();

            return prettyPrint.ToString();
        }

        public string ParentA { get; set; }
        public string ParentB { get; set; }
    }
}