using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    public class GitObjectGraph : IEnumerable<GitObject>
    {
        private Dictionary<string, GitObject> _underlyingDictionary;
        public const string OBJECT_BASE = @"C:\Users\mwiem\OneDrive\Projects\GitSpect.Cmd\.git\objects";
        private string _headSha;

        public GitObjectGraph() : this("no_head") { }

        public GitObjectGraph(string headSha)
        {
            _headSha = headSha;
            _underlyingDictionary = new Dictionary<string, GitObject>();
        }

        internal void Store(GitObject gitObj)
        {
            _underlyingDictionary.Add(gitObj.SHA, gitObj);
        }

        /// <summary>
        /// Looks up an object, returning true and populating the out variable if it exists.
        /// Returns false and a null object if it does not exist
        /// </summary>
        /// <param name="objSha"></param>
        /// <param name="found"></param>
        /// <returns></returns>
        public bool LookupObject(string objSha, out GitObject found)
        {
            bool retVal = false;
            found = null;

            if (_underlyingDictionary.ContainsKey(objSha))
            {
                retVal = true;
                found = _underlyingDictionary[objSha];
            }

            return retVal;
        }

        public IEnumerator<GitObject> GetEnumerator()
        {
            return _underlyingDictionary.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _underlyingDictionary.Values.GetEnumerator();
        }

        internal GitObject Get(string identifier)
        {
            GitObject found = null;
            LookupObject(identifier, out found);
            return found;
        }

        internal IList<GitObject> TraverseStartingAtCommit(string headStartSha, int depth)
        {
            List<GitObject> traversedObjects = new List<GitObject>();

            var gitObjectsInDirectory = ProcessPSObjectIntoGitObjects(headStartSha.Substring(0, 2));
            traversedObjects.AddRange(gitObjectsInDirectory);
            // The where clause says "If this is a commit object, cast it and match on the _headSha"
            // There can and will only be one match so First() is fine here
            Commit nextCommit = (Commit)traversedObjects.Where(
                x => x.Type == GitObjects.Commit ? ((Commit)x).SHA == _headSha : false).ToList().First();

            for(int i = 0; i < depth; i++)
            {
                var gitObjectsInNewDirectory = ProcessPSObjectIntoGitObjects(nextCommit.SHA.Substring(0, 2));
                traversedObjects.AddRange(gitObjectsInNewDirectory);

                nextCommit = (Commit)traversedObjects.Where(
                x => x.Type == GitObjects.Commit ? ((Commit)x).SHA == _headSha : false).ToList().First();
            }

            return traversedObjects;
        }

        /// <summary>
        /// Turns a /a5/ style directory name into the list of gitobjects contained within
        /// </summary>
        /// <param name="firstTwoLetters">A directory containing one or more git objects</param>
        /// <returns>an enumeration of the objects in the directory, empty list if no results</returns>
        public IList<GitObject> ProcessPSObjectIntoGitObjects(string firstTwoLetters)
        {
            IList<GitObject> results = new List<GitObject>();

            // Filter out pack and info
            if (firstTwoLetters.ToString().Length == 2)
            {
                string lsNameCommand = string.Format(@"cd {0}\{1}; ls", OBJECT_BASE, firstTwoLetters);
                var directoryContents = Program.ExecuteCommand(lsNameCommand);
                // Filter out existing raw txt files (probably need to get more sophisticated here)
                IEnumerable<string> gitObjects = directoryContents
                    .Select(x => x.ToString()).Where(x => !(x.ToString().EndsWith(".txt")));

                foreach (var theRestOfTheLetters in gitObjects)
                {
                    string fullName = firstTwoLetters.ToString() + theRestOfTheLetters.ToString();

                    // Run all the relevant commands on each object
                    string catFileNiceCommand = string.Format(@"git cat-file {0} -p", fullName);
                    var catFileNiceResult = Program.ExecuteCommand(catFileNiceCommand).ToArray();
                    string catFileTypeCommand = string.Format(@"git cat-file {0} -t", fullName);
                    var catFileTypeResult = Program.ExecuteCommand(catFileTypeCommand).ToArray();
                    string catFileSizeCommand = string.Format(@"git cat-file {0} -s", fullName);
                    var catFileSizeResult = Program.ExecuteCommand(catFileSizeCommand).ToArray();

                    // Parse metadata for GitObject
                    int sizeInBytes = int.Parse(((string)catFileSizeResult[0].BaseObject));
                    string objectType = (string)catFileTypeResult[0].BaseObject;
                    GitObject newObject = CreateNewObject(fullName, catFileNiceResult, objectType, sizeInBytes);

                    results.Add(newObject);
                }
            }

            return results;
        }

        private GitObject CreateNewObject(string fullName, PSObject[] catFileNiceResult, string objectType, int sizeInBytes)
        {
            GitObject newObject;

            // Yeah this could be an enum, but this is the ugly parsing code
            switch (objectType)
            {
                case "commit":
                    newObject = this.CreateNewCommit(fullName, catFileNiceResult, sizeInBytes);
                    break;
                case "tree":
                    newObject = this.CreateNewTree(fullName, catFileNiceResult, sizeInBytes);
                    break;
                case "blob":
                    newObject = this.CreateNewBlob(fullName, catFileNiceResult, sizeInBytes);

                    break;
                default:
                    newObject = new Blob()
                    {

                    };
                    break;
            }

            return newObject;
        }
    }
}
