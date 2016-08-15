using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    public static class GitObjectExtensions
    {
        public static void UpdateReferences(this GitObject me, GitObject obj)
        {
            me.RefCount++;
            me.RefShas.Add(obj.SHA);
        }
    }
}
