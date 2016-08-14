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
            Console.WriteLine("Hey there, gimme a minute to load your object graph...");
            _graphDictionary = new Dictionary<string, GitObject>();
            IEnumerable<PSObject> gitObjectHints;

            // Get the first two letters of all the git objects 
            // (also path and info, but we don't care about those yet)
            string poshCommand = string.Format(@"cd {0}; ls", OBJECT_BASE);
            gitObjectHints = ExecuteCommand(poshCommand);

            Stopwatch allObjsTimer = new Stopwatch();
            allObjsTimer.Start();

            foreach (var hint in gitObjectHints)
            {
                bool firstObjInDirectory = true;
                Stopwatch objTimer = new Stopwatch();
                objTimer.Start();
                IEnumerable<GitObject> gitObjs = ProcessPSObjectIntoGitObjects(hint);
                objTimer.Stop();

                foreach (var gitObj in gitObjs)
                {
                    if (!firstObjInDirectory) Console.WriteLine();

                    _graphDictionary.CacheGitObject(gitObj);

                    // Report to the console
                    string reportTemplate = "SHA: {0} Size: {1} Type: {2} ";
                    string type;
                    switch (gitObj.Type)
                    {
                        case GitObjects.Blob:
                            type = " B ";
                            break;
                        case GitObjects.Tree:
                            type = " T ";
                            break;
                        case GitObjects.Commit:
                            type = " C ";
                            break;
                        case GitObjects.MergeCommit:
                            type = " M ";
                            break;
                        default:
                            type = " | ";
                            break;
                    }

                    string report = string.Format(reportTemplate, gitObj.SHA.Substring(0, 5),
                                                    gitObj.Size.ToString("D4"), type);

                    Console.Write(report);
                    firstObjInDirectory = false;
                }

                string stats = string.Format("Directory {0} parsed. Took {1} ms", hint, objTimer.ElapsedMilliseconds);
                Console.Write(stats);
                Console.WriteLine();
            }

            allObjsTimer.Stop();

            int elapsedHours = allObjsTimer.Elapsed.Hours;
            int elapsedMinutes = allObjsTimer.Elapsed.Minutes;
            int elapsedSeconds = allObjsTimer.Elapsed.Seconds;
            int elapsedMilliSeconds = allObjsTimer.Elapsed.Milliseconds;
            Console.WriteLine("--- Objects loaded --- {0}:{1}:{2}.{3}",
                elapsedHours, elapsedMinutes, elapsedSeconds, elapsedMilliSeconds);
            
            while(true)
            {
                string command = GetCommand();

                switch (command)
                {
                    default:
                        Console.WriteLine("Unknown command: '{0}'", command);
                        break;
                }
            }
        }

        private static string GetCommand()
        {
            return Console.ReadLine();
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
                    GitObject newObject = CreateNewObject(fullName, catFileNiceResult, objectType, sizeInBytes);                   

                    results.Add(newObject);
                }
            }

            return results;
        }

        private static GitObject CreateNewObject(string fullName, PSObject[] catFileNiceResult, string objectType, int sizeInBytes)
        {
            GitObject newObject;

            // Yeah this could be an enum, but this is the ugly parsing code
            switch (objectType)
            {
                case "commit":
                    newObject = CreateNewCommit(fullName, catFileNiceResult, sizeInBytes);
                    break;
                case "tree":
                    newObject = CreateNewTree(fullName, catFileNiceResult, sizeInBytes);
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

        private static GitObject CreateNewCommit(string sha, PSObject[] rawCommit, int sizeInBytes)
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

        private static GitObject CreateNewTree(string sha, PSObject[] catFileNiceResult, int sizeInBytes)
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
