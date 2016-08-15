using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
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

        public static GitObject CreateNewBlob(this GitObjectGraph me, string fullName, PSObject[] catFileNiceResult, int bytes)
        {
            Blob newObject;
            newObject = new Blob()
            {
                SHA = fullName,
                Size = bytes
            };

            // This is what caches the path for the blobs path... kinda gross
            newObject.WriteRawBlobToDisk(fullName, catFileNiceResult);

            return newObject;
        }

        public static GitObject CreateNewCommit(this GitObjectGraph me, string sha, PSObject[] rawCommit, int sizeInBytes)
        {
            Commit retVal;
            bool rootCommit = rawCommit[1].BaseObject.ToString().StartsWith("author");
            bool mergeCommit = rawCommit[1].BaseObject.ToString().StartsWith("parent")
                && rawCommit[2].BaseObject.ToString().StartsWith("parent");

            if (rootCommit)
            {
                retVal = new Commit()
                {
                    SHA = sha,
                    Size = sizeInBytes,
                    Tree = rawCommit[0].BaseObject.ToString().Split(' ')[1],
                    Parent = null,
                    Author = rawCommit[1].BaseObject.ToString().Split(' ')[1],
                    Committer = rawCommit[2].BaseObject.ToString().Split(' ')[1],
                    Message = rawCommit[4].BaseObject.ToString().Split(' ')[1],
                };
            }
            else if(mergeCommit)
            {
                retVal = new MergeCommit()
                {
                    SHA = sha,
                    Size = sizeInBytes,
                    Tree = rawCommit[0].BaseObject.ToString().Split(' ')[1],
                    ParentA = rawCommit[1].BaseObject.ToString().Split(' ')[1],
                    ParentB = rawCommit[2].BaseObject.ToString().Split(' ')[1],
                    Author = rawCommit[3].BaseObject.ToString().Split(' ')[1],
                    Committer = rawCommit[4].BaseObject.ToString().Split(' ')[1],
                    Message = rawCommit[6].BaseObject.ToString().Split(' ')[1]
                };
            }
            else
            {
                retVal = new Commit()
                {
                    SHA = sha,
                    Size = sizeInBytes,
                    Tree = rawCommit[0].BaseObject.ToString().Split(' ')[1],
                    Parent = rawCommit[1].BaseObject.ToString().Split(' ')[1],
                    Author = rawCommit[2].BaseObject.ToString().Split(' ')[1],
                    Committer = rawCommit[3].BaseObject.ToString().Split(' ')[1],
                    Message = rawCommit[5].BaseObject.ToString().Split(' ')[1]
                };
            }

            return retVal;
        }

        public static GitObject CreateNewTree(this GitObjectGraph me, string sha, PSObject[] catFileNiceResult, int sizeInBytes)
        {
            int index = 0;
            int numLines = catFileNiceResult.Length;
            List<TreeInternalData> blobs = new List<TreeInternalData>();
            List<TreeInternalData> trees = new List<TreeInternalData>();

            foreach (var line in catFileNiceResult)
            {
                string[] lineMeta = line.BaseObject.ToString().Split(' ');
                string[] shaName = lineMeta[2].Split('\t');

                TreeInternalData data = new TreeInternalData()
                {
                    ModeCode = lineMeta[0],
                    SHA = shaName[0],
                    FileName = shaName[1]
                };

                // Trees are a collection of trees and blobs
                switch (lineMeta[1])
                {
                    case "blob":
                        blobs.Add(data);
                        break;
                    case "tree":
                        trees.Add(data);
                        break;
                    default:
                        break;
                }

                index++;
            }               

            GitObject newTree = new Tree()
            {
                Blobs = blobs,
                SHA = sha,
                Size = sizeInBytes,
                Trees = trees
            };

            return newTree;
        }
    }
}
