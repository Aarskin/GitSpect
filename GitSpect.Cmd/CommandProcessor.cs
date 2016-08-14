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
        public GitObject Process(Commands command, params string[] args)
        {
            GitObject retVal = null;
            string typeWord = "Unknown";

            // Assumptions that should be formalized somewhere
            if (args != null && !string.IsNullOrEmpty(args[0]))
            {
                typeWord = args[0];
            }

            GitObjects objType = GitObjects.Unknown;
            GitObjects objectType = (Enum.TryParse(typeWord, out objType)) ? objType : GitObjects.Unknown;
            
            switch (command)
            {
                case Commands.MostConnected:
                    retVal = FindMostConnectedObject();
                    break;
                case Commands.Random:
                    retVal = FindRandomObject(objectType);
                    break;
                case Commands.Invalid:
                    Console.WriteLine("Invalid command. Type ? for help.");
                    break;
                default:
                    Console.WriteLine("Unknown command: '{0}'", command);
                    break;
            }

            return retVal;
        }

        private GitObject FindRandomObject(GitObjects objType)
        {
            GitObject randomObject;

            List<GitObject> listOfType = _objectGraph.Select(x => x.Value).Where(x => x.Type == objType).ToList();
            int maxIndex = listOfType.Count > 0 ? listOfType.Count - 1 : 0;
            int randomIndex = new Random().Next(0);

            // yeah, we lose one here, but whatever
            randomObject = maxIndex > 0 ? listOfType[randomIndex] : null;

            return randomObject;
        }

        private GitObject FindMostConnectedObject()
        {
            GitObject mostConnected = null;

            var refCountMax = _objectGraph.Select(x => x.Value.RefCount).Max();
            mostConnected = _objectGraph.Select(x => x.Value).Where(x => x.RefCount == refCountMax).ToList().First();

            return mostConnected;
        }

        public enum Commands
        {
            MostConnected,
            Random,
            Unknown,
            Invalid
        }
    }
}
