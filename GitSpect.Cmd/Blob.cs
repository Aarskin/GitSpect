using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    class Blob : GitObject
    {
        public Blob() : base()
        {
            Type = GitObjects.Blob;
        }

        internal static void WriteRawBlobToDisk(string sha, PSObject[] catFileNiceResult)
        {
            string folder = sha.Substring(0, 2);
            string name = sha.Substring(2) + "_raw.txt";
            string path = Path.Combine(Program.OBJECT_BASE, folder, name);
            string[] lines = catFileNiceResult.Select(x => x.ToString()).ToArray();

            if(!File.Exists(path))
            {
                // Only create the path if necessary
                File.AppendAllLines(path, lines);
            }
        }
    }
}
