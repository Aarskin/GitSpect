using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    public static class DictionaryExtensions
    {
        private static Lazy<Dictionary<string, GitObject>> _stalledConnections
            = new Lazy<Dictionary<string, GitObject>>();

        public static void CacheGitObject(this Dictionary<string, GitObject> me, GitObject gitObj)
        {
            // Put this the main datastore
            me.Add(gitObj.SHA, gitObj);

            var touchedObjects = FindNewConnections(gitObj);

            foreach (var objSha in touchedObjects)
            {
                if (me[objSha] != null)
                {
                    me[objSha].UpdateReferences(gitObj);
                }
                else
                {
                    // Save this connection, and recall it when the referenced object comes in
                    _stalledConnections.Value.Add(objSha, gitObj);
                }
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
                case GitObjects.MergeCommit:
                    retVal = UpdateCommitConnections((MergeCommit)gitObj);
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
            // For MergeCommits only
            string referencedParentA, referencedParentB;
            // This is a guard against the root commit, which has no parent
            string referencedParent = (commitObj.Parent != null) ? commitObj.Parent : null;
            string referencedTree = commitObj.Tree;            

            if (referencedParent != null)
            {
                touchedObjects.Add(referencedParent);
            }
            else if (commitObj.Type == GitObjects.MergeCommit)
            {
                MergeCommit merge = (MergeCommit)commitObj;
                referencedParentA = merge.ParentA;
                referencedParentB = merge.ParentB;

                touchedObjects.Add(referencedParentA);
                touchedObjects.Add(referencedParentB);
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
