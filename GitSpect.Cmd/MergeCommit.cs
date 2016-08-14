namespace GitSpect.Cmd
{
    internal class MergeCommit : Commit
    {
        public string ParentA { get; set; }
        public string ParentB { get; set; }
    }
}