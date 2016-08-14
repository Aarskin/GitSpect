using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    class CommandProcessor
    {
        private Dictionary<string, GitObject> _objectGraph;

        public CommandProcessor(Dictionary<string, GitObject> graphToSearch)
        {
            _objectGraph = graphToSearch;
        }

        /// <summary>
        /// Process the command, returning a GitObject. Null may be returned.
        /// </summary>
        /// <param name="command">Enum</param>
        /// <returns></returns>
        public GitObject Process(Commands command)
        {
            GitObject retVal = null;

            switch (command)
            {
                case Commands.MostConnected:
                    retVal = FindMostConnectedObject();
                    break;
                default:
                    Console.WriteLine("Unknown command: '{0}'", command);
                    break;
            }

            return retVal;
        }

        private GitObject FindMostConnectedObject()
        {
            GitObject mostConnected = null;



            return mostConnected;
        }

        public enum Commands
        {
            MostConnected
        }
    }
}
