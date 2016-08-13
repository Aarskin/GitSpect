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
        internal static void WriteRawBlobToDisk(PSObject[] catFileNiceResult)
        {
            string[] lines = catFileNiceResult.Select(x => x.ToString()).ToArray();
            string path = string.Format(@"{0}\{1}");
            File.AppendAllLines(path, lines);
        }
    }
}
