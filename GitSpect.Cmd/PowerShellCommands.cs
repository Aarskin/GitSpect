using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    public class PowerShellCommands
    {
        public const string GET_LAST_5_MINUTES = "ls | where {{$_.LastAccessTime -gt (Get-Date).AddMinutes(-5)}}";

        public const string GET_ALL = "ls";

        public const string OBJECT_TEMPLATE = "ls | where {{$_.Name -eq \"{0}\"}}";

        public static string CD_BASE = "cd " + Program.OBJECT_BASE;
    }
}
