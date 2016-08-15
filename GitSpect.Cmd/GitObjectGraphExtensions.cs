using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    public static class GitObjectGraphExtensions
    {
        // Maps a GitObject's SHA to a list of objects that are referencing it - Shitty mode
        private static Lazy<List<KeyValuePair<string, GitObject>>> _stalledConnections
            = new Lazy<List<KeyValuePair<string, GitObject>>>();

        public static void CacheGitObject(this GitObjectGraph me, GitObject gitObj)
        {
            // Account for anything touching this object
            var referencesToMe = _stalledConnections.Value.Where(x => x.Key == gitObj.SHA).Select(x => x.Value).ToList();
            var referencesAsShas = referencesToMe.Select(x => x.SHA).ToList();
            gitObj.RefCount += referencesToMe.Count;
            gitObj.RefShas.AddRange(referencesAsShas);
            _stalledConnections.Value.RemoveAll(x => x.Key == gitObj.SHA);

            // Put this in the main datastore
            me.Store(gitObj);
            
            // Account for anything this object touches
            var newTouches = FindNewConnections(gitObj);

            foreach (var objSha in newTouches)
            {
                GitObject found = null;
                if (me.LookupObject(objSha, out found))
                {
                    found.UpdateReferences(gitObj);
                }
                else
                {
                    // Save this connection, and recall it when the referenced object comes in
                    _stalledConnections.Value.Add(new KeyValuePair<string, GitObject>(objSha, gitObj));
                }
            }
        }

        private static List<string> FindNewConnections(GitObject gitObj)
        {
            List<string> retVal;

            switch (gitObj.Type)
            {
                case GitObjects.Tree:
                    retVal = UpdateTreeConnections((Tree)gitObj);
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

        private static List<string> UpdateTreeConnections(Tree treeObj)
        {
            List<string> touchedObjects = new List<string>();

            foreach (var data in treeObj.Trees)
            {
                touchedObjects.Add(data.SHA);
            }

            foreach (var data in treeObj.Blobs)
            {
                touchedObjects.Add(data.SHA);
            }

            return touchedObjects;
        }
    }
}
