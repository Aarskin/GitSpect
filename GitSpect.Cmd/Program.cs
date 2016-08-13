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

            using (PowerShell posh = PowerShell.Create())
            {
                string command = string.Format(@"cd {0}; ls", OBJECT_BASE);
                posh.AddScript(command);

                var results = posh.Invoke();

                foreach (var result in results)
                {
                    ProcessGitObject(result);
                    Console.WriteLine(result);
                }
            }

            Console.WriteLine("Press any key to close this window");
            Console.ReadKey();
        }

        private static void ProcessGitObject(PSObject firstTwoLetters)
        {
            
        }
    }
}
