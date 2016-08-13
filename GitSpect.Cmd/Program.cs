using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            using (PowerShell posh = PowerShell.Create())
            {
                posh.AddScript(@"cd C:\Users\mwiem\OneDrive\Projects\GitSpect.Console\.git\objects; ls");

                var results = posh.Invoke();

                foreach (var result in results)
                {
                    System.Console.WriteLine(result);
                }
            }

            System.Console.ReadKey();
        }
    }
}
