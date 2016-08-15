﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    class CommandProcessor
    {
        private Dictionary<string, GitObject> _objectGraph;
        private Random _rng;
        private GitObject _currentObjectHandle;

        public string CurrentHandle { get
            {
                string handle = _currentObjectHandle != null ? 
                    _currentObjectHandle.SHA.Substring(0, 5) : "NULL";
                return handle;
            }
        }

        public CommandProcessor(Dictionary<string, GitObject> graphToSearch)
        {
            _objectGraph = graphToSearch;
            _rng = new Random();
        }

        /// <summary>
        /// Process the command, returning a GitObject. Null may be returned.
        /// </summary>
        /// <param name="command">Enum</param>
        /// <returns></returns>
        public GitObject Process(Commands command, params string[] args)
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
            
            switch (command)
            {
                case Commands.MostConnected:
                    retVal = FindMostConnectedObject();
                    break;
                case Commands.Random:
                    retVal = FindRandomObject(objectType);
                    break;
                case Commands.Follow:
                    retVal = FollowObject(args[0]);
                    break;
                case Commands.Invalid:
                    Console.WriteLine("Invalid command. Type ? for help.");
                    break;
                default:
                    Console.WriteLine("Unknown command: '{0}'", command);
                    break;
            }

            _currentObjectHandle = retVal == null ? _currentObjectHandle : retVal;

            return retVal;
        }

        private GitObject FollowObject(string identifier)
        {
            GitObject followedObject = null;
            Commit currentCommit;
            Blob currentBlob;// = (Blob)_currentObjectHandle;
            Tree currentTree;// = (Tree)_currentObjectHandle;

            // Dangerous assumption, but whatever for now
            if (identifier.Length == 40)
            {
              followedObject = _objectGraph[identifier];
            }

            switch (identifier)
            {
                case "Parent":
                case "parent":
                    currentCommit = (Commit)_currentObjectHandle;

                    if(_objectGraph.ContainsKey(currentCommit.Parent))
                    {
                        followedObject = _objectGraph[currentCommit.Parent];
                    }
                    else
                    {
                        followedObject = null;
                    }
                    break;
                default:
                    break;
            }

            return followedObject;
        }

        private GitObject FindRandomObject(GitObjects objType)
        {
            GitObject randomObject;

            List<GitObject> listOfType = _objectGraph.Select(x => x.Value).Where(x => x.Type == objType).ToList();
            int maxIndex = listOfType.Count > 0 ? listOfType.Count - 1 : 0;
            int randomIndex = _rng.Next(0, maxIndex);

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
            Follow,
            Unknown,
            Invalid
        }
    }
}
