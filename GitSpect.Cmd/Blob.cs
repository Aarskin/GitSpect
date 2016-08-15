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
        // These reflect the blob most recently written to disk by this instance...
        // I have a feeling this might be weird to debug if something comes up
        private string _rawPath;
        private string _fileName;

        public Blob() : base()
        {
            Type = GitObjects.Blob;
        }

        internal void WriteRawBlobToDisk(string sha, PSObject[] catFileNiceResult)
        {
            string folder = sha.Substring(0, 2);
            string name = sha.Substring(2) + "_raw.txt";
            string path = Path.Combine(Program.OBJECT_BASE, folder, name);
            _rawPath = path;
            _fileName = Path.GetFileName(_rawPath);
            string[] lines = catFileNiceResult.Select(x => x.ToString()).ToArray();
            if(!File.Exists(path))
            {
                // Only create the path if necessary
                File.AppendAllLines(path, lines);
            }
        }

        public override string ToString()
        {
            var baseString = base.ToString();
            StringBuilder prettyPrint = new StringBuilder(baseString);

            return prettyPrint.ToString();
        }
    }
}
