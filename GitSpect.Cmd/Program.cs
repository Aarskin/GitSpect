using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    public class Program
    {
        public const string OBJECT_BASE = @"C:\Users\mwiem\OneDrive\Projects\GitSpect.Cmd\.git\objects";
        private static Dictionary<string, GitObject> _graphDictionary;
        private static IEnumerable<PSObject> gitObjectHints;

        public static void Main(string[] args)
        {
            Console.WriteLine("Hey there, gimme a sec to load up your objects!");
            _graphDictionary = new Dictionary<string, GitObject>();
            IEnumerable<PSObject> gitObjectHints;

            // Get the first two letters of all the git objects 
            // (also path and info, but we don't care about those yet)
            string command = string.Format(@"cd {0}; ls", OBJECT_BASE);
            gitObjectHints = ExecuteCommand(command);

            Stopwatch allObjsTimer = new Stopwatch();
            allObjsTimer.Start();

            foreach (var hint in gitObjectHints)
            {
                bool first = true;
                Stopwatch objTimer = new Stopwatch();
                objTimer.Start();
                IEnumerable<GitObject> gitObjs = ProcessPSObjectIntoGitObjects(hint);
                objTimer.Stop();

                foreach (var gitObj in gitObjs)
                {
                    if (!first) Console.WriteLine();

                    _graphDictionary.Add(gitObj.SHA, gitObj);

                    Console.Write(gitObj.SHA + " | ");
                    first = false;
                }

                string stats = string.Format("Hint {0} parsed. Took {1} ms", hint, objTimer.ElapsedMilliseconds);
                Console.Write(stats);
                Console.WriteLine();
            }

            allObjsTimer.Stop();

            int elapsedSeconds = allObjsTimer.Elapsed.Seconds;
            Console.WriteLine("Objects loaded in {0} seconds", elapsedSeconds);
            Console.ReadKey();
        }

        /// <summary>
        /// Turns a /a5/ style directory name into the list of gitobjects contained within
        /// </summary>
        /// <param name="firstTwoLetters">A directory containing one or more git objects</param>
        /// <returns>an enumeration of the objects in the directory, empty list if no results</returns>
        public static IList<GitObject> ProcessPSObjectIntoGitObjects(PSObject firstTwoLetters)
        {
            IList<GitObject> results = new List<GitObject>();

            // Filter out pack and info
            if(firstTwoLetters.ToString().Length == 2)
            {
                string lsNameCommand = string.Format(@"cd {0}\{1}; ls", OBJECT_BASE, firstTwoLetters);
                var directoryContents = ExecuteCommand(lsNameCommand);
                // Filter out existing raw txt files (probably need to get more sophisticated here)
                IEnumerable<string> gitObjects = directoryContents
                    .Select(x => x.ToString()).Where(x => !(x.ToString().EndsWith(".txt")));
                
                foreach (var theRestOfTheLetters in gitObjects)
                {
                    string fullName = firstTwoLetters.ToString() + theRestOfTheLetters.ToString();

                    // Run all the relevant commands on each object
                    string catFileNiceCommand = string.Format(@"git cat-file {0} -p", fullName);
                    var catFileNiceResult = ExecuteCommand(catFileNiceCommand).ToArray();
                    string catFileTypeCommand = string.Format(@"git cat-file {0} -t", fullName);
                    var catFileTypeResult = ExecuteCommand(catFileTypeCommand).ToArray();
                    string catFileSizeCommand = string.Format(@"git cat-file {0} -s", fullName);
                    var catFileSizeResult = ExecuteCommand(catFileSizeCommand).ToArray();

                    // Parse metadata for GitObject
                    int sizeInBytes = int.Parse(((string)catFileSizeResult[0].BaseObject));
                    string objectType = (string)catFileTypeResult[0].BaseObject;
                    GitObject newObject = CreateNewObject(fullName, catFileNiceResult, objectType);                   

                    results.Add(newObject);
                }
            }

            return results;
        }

        private static GitObject CreateNewObject(string fullName, PSObject[] catFileNiceResult, string objectType)
        {
            GitObject newObject;

            // Yeah this could be an enum, but this is the ugly parsing code
            switch (objectType)
            {
                case "commit":
                    newObject = new Commit()
                    {
                        SHA = fullName,
                        Tree = (string)catFileNiceResult[0].BaseObject,
                        Parent = (string)catFileNiceResult[1].BaseObject,
                        Author = (string)catFileNiceResult[2].BaseObject,
                        Committer = (string)catFileNiceResult[3].BaseObject,
                        Message = (string)catFileNiceResult[5].BaseObject,
                    };
                    break;
                case "tree":
                    newObject = CreateNewTree(catFileNiceResult);
                    break;
                case "blob":
                    Blob.WriteRawBlobToDisk(fullName, catFileNiceResult);
                    newObject = new Blob()
                    {
                        SHA = fullName
                    };
                    break;
                default:
                    newObject = new Blob()
                    {

                    };
                    break;
            }

            return newObject;
        }

            
        private static GitObject CreateNewTree(PSObject[] catFileNiceResult)
        {
            int index = 0;
            int numLines = catFileNiceResult.Length;
            //string[,] metadataMatrix = new string[numLines,3];
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
                Trees = trees
            };

            return newTree;
        }

        /// <summary>
        /// Execute a command string using PowerShell, synchronously
        /// </summary>
        /// <param name="command"></param>
        /// <returns>The PSObjects returned by the command invocation</returns>
        public static IEnumerable<PSObject> ExecuteCommand(string command)
        {
            IEnumerable<PSObject> results;

            using (PowerShell posh = PowerShell.Create())
            {
                posh.AddScript(command);
                results = posh.Invoke();
            }

            return results;
        }
    }
}
