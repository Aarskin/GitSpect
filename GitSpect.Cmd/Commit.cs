﻿namespace GitSpect.Cmd
{
    internal class Commit : GitObject
    {
        public Commit() : base()
        {
            Type = GitObjects.Commit;
        }

        public override string ToString()
        {
            string prettyPrint;



            return prettyPrint;
        }

        public string Author { get; internal set; }
        public string Committer { get; internal set; }
        public string Message { get; internal set; }
        public string Parent { get; internal set; }
        public string Tree { get; internal set; }
    }
}