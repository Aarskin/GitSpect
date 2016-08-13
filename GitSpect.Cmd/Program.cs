using System;
using System.Collections.Generic;
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

        public static void Main(string[] args)
        {
            _graphDictionary = new Dictionary<string, GitObject>();
            IEnumerable<PSObject> gitObjectHeaders;

            // Get the first two letters of all the git objects 
            // (also path and info, but we don't care about those yet)
            string command = string.Format(@"cd {0}; ls", OBJECT_BASE);
            gitObjectHeaders = ExecuteCommand(command);

            foreach (var obj in gitObjectHeaders)
            {
                IEnumerable<GitObject> gitObjs = ProcessPSObjectIntoGitObjects(obj);

                foreach (var gitObj in gitObjs)
                {
                    _graphDictionary.Add(gitObj.SHA, gitObj);
                    Console.WriteLine(gitObj.SHA);
                }
            }

            Console.WriteLine("Press any key to close this window");
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
                var restOfLettersSet = ExecuteCommand(lsNameCommand);
                
                foreach (var theRestOfTheLetters in restOfLettersSet)
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
                    Blob.WriteRawBlobToDisk(catFileNiceResult);
                    newObject = new Blob()
                    {
                        SHA = fullName
                    };
                    break;
                default:
                    newObject = new Blob
                    {

                    };
                    break;
            }

            return newObject;
        }

        private static GitObject CreateNewTree(PSObject[] catFileNiceResult)
        {
            return new Blob();
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
