using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using static GitSpect.Cmd.CommandProcessor;

namespace GitSpect.Cmd
{
    public class Program
    {
        public const string OBJECT_BASE = @"C:\Users\mwiem\OneDrive\Projects\GitSpect.Cmd\.git\objects";
        public const string REPO_BASE = @"C:\Users\mwiem\OneDrive\Projects\GitSpect.Cmd";
        public const string ONE_LINE_TO_RULE_THEM_ALL = "-----------------------------------------------------------------";
        private static Dictionary<string, GitObject> _graphDictionary;

        public static void Main(string[] args)
        {
            // Toggles
            bool quickDebug = Convert.ToBoolean(args[0]);

            #region Object Graph Loading

            string quickDebugStatus = quickDebug ? "On" : "Off";
            string welcomeHeader = string.Format("GitSpect v0.0.1 | QuickDebug {0}" , quickDebugStatus);

            Console.WriteLine(ONE_LINE_TO_RULE_THEM_ALL);
            Console.WriteLine(welcomeHeader);
            Console.WriteLine(ONE_LINE_TO_RULE_THEM_ALL);
            _graphDictionary = new Dictionary<string, GitObject>();
            IEnumerable<PSObject> gitObjectHints;

            // Get the first two letters of all the git objects 
            // (also path and info, but we don't care about those yet)
            var getGitObjects = quickDebug ? PowerShellCommands.GET_LAST_5_MINUTES : PowerShellCommands.GET_ALL;
            string poshCommand = string.Format(@"cd {0}; {1}", OBJECT_BASE, getGitObjects);
            gitObjectHints = ExecuteCommand(poshCommand);

            Stopwatch allObjsTimer = new Stopwatch();
            int totalSizeOfAllGraphObjects = 0;
            int totalNumberOfGraphObjects = 0;
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

                    // Track stats
                    totalSizeOfAllGraphObjects += gitObj.Size;
                    totalNumberOfGraphObjects++;

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
            string overallReport = string.Format("--- Objects loaded --- {0}:{1}:{2}.{3}. Bytes: {4}, # Objs: {5}",
                elapsedHours, elapsedMinutes, elapsedSeconds, elapsedMilliSeconds, totalSizeOfAllGraphObjects, totalNumberOfGraphObjects);
            Console.WriteLine(ONE_LINE_TO_RULE_THEM_ALL);
            Console.WriteLine(overallReport);
            Console.WriteLine(ONE_LINE_TO_RULE_THEM_ALL);

            var reportingPath = Path.Combine(REPO_BASE, "SizeLog.txt");
            var writer = File.AppendText(reportingPath);
            writer.Write(DateTime.Now.ToString() + " " + overallReport);
            writer.Flush();
            writer.Close();
#endregion

            // Finally, the actual command loop
            CommandProcessor processor = new CommandProcessor(_graphDictionary);

            while (true)
            {
                Console.Write("?> ");
                Commands command = GetCommand();
                var result = processor.Process(command);
                string report = result == null || string.IsNullOrEmpty(result.SHA) ? "No Object Found" : result.ToString();
                Console.WriteLine(report);
            }
        }

        private static Commands GetCommand()
        {
            Commands retVal = Commands.Unknown;
            string stringCommand = Console.ReadLine();
            if (Enum.TryParse(stringCommand, out retVal)) { }

            return retVal;
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
                    newObject = CreateNewBlob(fullName, catFileNiceResult, sizeInBytes);
                    
                    break;
                default:
                    newObject = new Blob()
                    {

                    };
                    break;
            }

            return newObject;
        }

        private static GitObject CreateNewBlob(string fullName, PSObject[] catFileNiceResult, int bytes)
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
