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

            var touchedObjects = FindNewConnections(gitObj);

            foreach (var obj in touchedObjects)
            {
                me[obj].UpdateReferences(gitObj);
            }
        }

        private static List<string> FindNewConnections(GitObject gitObj)
        {
            List<string> retVal;

            switch (gitObj.Type)
            {
                case GitObjects.Tree:
                    retVal = UpdateTreeConnections();
                    break;
                case GitObjects.Commit:
                    retVal = UpdateCommitConnections((Commit)gitObj);
                    break;
                default:
                    // nothing to do for blobs
                    retVal = new List<string>();
                    break;
            }

            return retVal;
        }

        private static List<string> UpdateCommitConnections(Commit commitObj)
        {
            List<string> touchedObjects = new List<string>();
            string referencedParent = (commitObj.Parent != null) ? commitObj.Parent : null;
            string referencedTree = commitObj.Tree;

            if(referencedParent != null)
            {
                touchedObjects.Add(referencedParent);
            }
            touchedObjects.Add(referencedTree);

            return touchedObjects;
        }

        private static List<string> UpdateTreeConnections()
        {
            return new List<string>();
        }
    }
}
