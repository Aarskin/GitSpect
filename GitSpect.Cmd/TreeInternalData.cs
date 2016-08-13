namespace GitSpect.Cmd
{
    internal class TreeInternalData
    {
        public TreeInternalData()
        {
        }

        public string FileName { get; internal set; }
        public string ModeCode { get; internal set; }
        public string SHA { get; internal set; }
    }
}