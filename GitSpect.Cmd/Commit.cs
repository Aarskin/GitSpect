using System.Text;

namespace GitSpect.Cmd
{
    internal class Commit : GitObject
    {
        public Commit() : base()
        {
            Type = GitObjects.Commit;
        }

        public override string ToString()
        {
            string baseString = base.ToString();
            StringBuilder prettyPrint = new StringBuilder(baseString);

            string authorLine = string.Format("Author: {0}", Author);
            string committerLine = string.Format("Committer: {0}", Committer);
            string parentLine = string.Format("Parent: {0}", Parent);
            string treeLine = string.Format("Tree: {0}", Tree);
            string messageLine = string.Format("Message: {0}", Message);

            prettyPrint.AppendLine(authorLine);
            prettyPrint.AppendLine(committerLine);
            prettyPrint.AppendLine(parentLine);
            prettyPrint.AppendLine(treeLine);
            prettyPrint.AppendLine(messageLine);
            prettyPrint.AppendLine();

            return prettyPrint.ToString();
        }

        public string Author { get; internal set; }
        public string Committer { get; internal set; }
        public string Message { get; internal set; }
        public string Parent { get; internal set; }
        public string Tree { get; internal set; }
    }
}