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
            string prettyPrint;



            return prettyPrint;
        }

        public string ParentA { get; set; }
        public string ParentB { get; set; }
    }
}