using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    class Program
    {
        private const string OBJECT_BASE = @"C:\Users\mwiem\OneDrive\Projects\GitSpect.Cmd\.git\objects";
        private static Dictionary<string, GitObject> _graphDictionary;

        static void Main(string[] args)
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
                string command = string.Format(@"cd {0}\{1}; ls", OBJECT_BASE, firstTwoLetters);
                var restOfLettersSet = ExecuteCommand(command);
                
                foreach (var theRestOfTheLetters in restOfLettersSet)
                {
                    string fullName = firstTwoLetters.ToString() + theRestOfTheLetters.ToString();
                    GitObject newObject = new Blob()
                    {
                        SHA = fullName
                    };

                    results.Add(newObject);
                }
            }

            return results;
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
