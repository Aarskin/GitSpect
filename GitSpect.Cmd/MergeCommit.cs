namespace GitSpect.Cmd
{
    internal class MergeCommit : Commit
    {
        public MergeCommit()
        {
            Type = GitObjects.MergeCommit;
        }
        public string ParentA { get; set; }
        public string ParentB { get; set; }
    }
}