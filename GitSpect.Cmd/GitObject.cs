﻿using System.Collections.Generic;

namespace GitSpect.Cmd
{
    public abstract class GitObject
    {
        public GitObject()
        {
            RefShas = new List<string>();
        }

        public string SHA;
        public GitObjects Type;

        public IList<GitObject> IncomingReferences { get; internal set; }
        public int RefCount { get; internal set; }
    }

    public enum GitObjects
    {
        Blob,
        Tree,
        Commit,
        MergeCommit
    }
}