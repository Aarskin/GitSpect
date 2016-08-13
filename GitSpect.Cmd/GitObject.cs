namespace GitSpect.Cmd
{
    public abstract class GitObject
    {
        public string SHA;
        public GitObjects Type;
    }

    public enum GitObjects
    {
        Blob,
        Tree,
        Commit
    }
}