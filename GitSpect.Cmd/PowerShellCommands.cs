using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    public class PowerShellCommands
    {
        public const string GET_LAST_15_MINUTES = "ls | where {$_.LastAccessTime -gt (Get-Date).AddMinutes(-35)}";

        public const string GET_ALL = "ls";
    }
}
