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
                ProcessPSObject(obj);
                Console.WriteLine(obj);
            }

            Console.WriteLine("Press any key to close this window");
            Console.ReadKey();
        }

        private static GitObject ProcessPSObject(PSObject firstTwoLetters)
        {
            // Filter out pack and info
            if(firstTwoLetters.ToString().Length == 2)
            {
                List<string> fullGitObjectNames = new List<string>();
                IEnumerable<PSObject> results;
                string command = string.Format(@"cd {0}; ls", firstTwoLetters);
                results = ExecuteCommand(command);
                
                foreach (var result in results)
                {
                    string fullName = firstTwoLetters.ToString() + result.ToString();
                    fullGitObjectNames.Add(fullName);
                }
            }

            GitObject newObject = new Blob();

            return newObject;
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
