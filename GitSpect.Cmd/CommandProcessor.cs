using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    class CommandProcessor
    {
        private Random _rng;
        private GitObject _currentObjectHandle;

        public string CurrentHandle { get
            {
                string handle = _currentObjectHandle != null ? 
                    _currentObjectHandle.Type + " | " + _currentObjectHandle.SHA.Substring(0, 5) : 
                    "NULL";
                return handle;
            }
        }

        public CommandProcessor()
        {
            _rng = new Random();
        }

        /// <summary>
        /// Process the command on the given graph, returning a GitObject. Null may be returned.
        /// </summary>
        /// <param name="command">Enum</param>
        /// <returns></returns>
        public GitObject Process(Commands command, GitObjectGraph graph, params string[] args)
        {
            GitObject retVal = null;
            GitObjects objectType = GitObjects.Unknown; 
            string typeArg = "Unknown";
            string followArg = string.Empty;
       
            if(command == Commands.Random)
            {
                typeArg = args[0];
                GitObjects objType = GitObjects.Unknown;
                objectType = (Enum.TryParse(typeArg, out objType)) ? objType : GitObjects.Unknown;
            }

            // Poor man's matching. Types are in the way here.
            string strCommand = command.ToString().ToLower();

            switch (strCommand)
            {
                case "mostconnected":
                case "mc":
                    retVal = FindMostConnectedObject(graph);
                    break;
                case "random":
                case "r":
                    retVal = FindRandomObject(objectType, graph);
                    break;
                case "follow":
                case "f":
                    retVal = FollowObject(args[0], graph);
                    break;
                case "invalid":
                    Console.WriteLine("Invalid command. Type ? for help.");
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }

            // Stay the same if the command failed, otherwise, update to the result.
            _currentObjectHandle = retVal == null ? _currentObjectHandle : retVal;

            return retVal;
        }

        private GitObject FollowObject(string identifier, GitObjectGraph graph)
        {
            GitObject followedObject = null;

            // Dangerous assumption, but whatever for now
            if (identifier.Length == 40)
            {
              followedObject = graph.Get(identifier);
            }


            switch (_currentObjectHandle.Type)
            {
                case GitObjects.Blob:
                    break;
                case GitObjects.Tree:
                    followedObject = FollowTree(identifier, graph);
                    break;
                case GitObjects.Commit:
                    followedObject = FollowCommit(identifier, graph);                    
                    break;
                case GitObjects.MergeCommit:
                    break;
                case GitObjects.Unknown:
                    break;
                default:
                    break;
            }

            return followedObject;
        }

        private GitObject FollowTree(string identifier, GitObjectGraph graph)
        {
            GitObject followedObject = null;
            Tree currentTree = (Tree)_currentObjectHandle;

            // Dangerous assumption
            if (identifier.Length == 2)
            {
                char[] code = identifier.ToCharArray();
                bool blob = (code[0] == 'B') || (code[0] == 'b');
                bool tree = (code[0] == 'T') || (code[0] == 't');
                int index = int.Parse(code[1].ToString());

                if(blob)
                {
                    string followedObjectSha = currentTree.Blobs.Select(x => x.SHA).ToList()[index];
                    graph.LookupObject(followedObjectSha, out followedObject);
                }
                else if(tree)
                {
                    string followedObjectSha = currentTree.Trees.Select(x => x.SHA).ToList()[index];
                    graph.LookupObject(followedObjectSha, out followedObject);
                }
            }

            return followedObject;
        }

        private GitObject FollowCommit(string identifier, GitObjectGraph graph)
        {
            GitObject followedObject = null;
            Commit currentCommit = (Commit)_currentObjectHandle;

            switch (identifier)
            {
                case "Parent":
                case "parent":
                    graph.LookupObject(currentCommit.Parent, out followedObject);
                    break;
                case "Tree":
                case "tree":
                    graph.LookupObject(currentCommit.Tree, out followedObject);
                    break;
                default:
                    break;
            }

            return followedObject;
        }

        private GitObject FindRandomObject(GitObjects objType, GitObjectGraph graph)
        {
            GitObject randomObject;

            List<GitObject> listOfType = graph.Where(x => x.Type == objType).ToList();
            int maxIndex = listOfType.Count > 0 ? listOfType.Count - 1 : 0;
            int randomIndex = _rng.Next(0, maxIndex);

            // yeah, we lose access to only one here, but whatever
            randomObject = maxIndex > 0 ? listOfType[randomIndex] : null;

            return randomObject;
        }

        private GitObject FindMostConnectedObject(GitObjectGraph graph)
        {
            GitObject mostConnected = null;

            var refCountMax = graph.Select(x => x.RefCount).ToList().Max();
            mostConnected = graph.Where(x => x.RefCount == refCountMax).ToList().First();

            return mostConnected;
        }

        public enum Commands
        {
            Unknown,
            MostConnected,
            Random,
            Follow,
            Invalid
        }
    }
}
