using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    public static class DictionaryExtensions
    {
        public static void CacheGitObject(this Dictionary<string, GitObject> me, string sha, GitObject gitObj)
        {
            me.Add(sha, gitObj);

            RefreshConnections(gitObj);
        }

        private static void RefreshConnections(GitObject gitObj)
        {
            switch (gitObj.Type)
            {
                case GitObjects.Tree:
                    UpdateTreeConnections((Tree)gitObj);
                    break;
                case GitObjects.Commit:
                    UpdateCommitConnections();
                    break;
                default:
                    // nothing to do for blobs
                    break;
            }
        }

        private static void UpdateCommitConnections()
        {
            throw new NotImplementedException();
        }

        private static void UpdateTreeConnections(Tree treeObj)
        {
            
        }
    }
}
