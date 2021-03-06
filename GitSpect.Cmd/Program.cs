﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using static GitSpect.Cmd.CommandProcessor;

namespace GitSpect.Cmd
{
    public class Program
    {
        public const string OBJECT_BASE = @"C:\Users\mwiem\OneDrive\Projects\GitSpect.Cmd\.git\objects";
        public const string REPO_BASE = @"C:\Users\mwiem\OneDrive\Projects\GitSpect.Cmd";
        public const string ONE_LINE_TO_RULE_THEM_ALL = "-----------------------------------------------------------------";
        private static GitObjectGraph _objectGraph;

        public static void Main(string[] args)
        {
            // Toggles
            bool quickDebug = Convert.ToBoolean(args[0]);
            bool headStart = args[1] != null && !string.IsNullOrEmpty(args[1]);
            string headStartSha = headStart ? args[1] : string.Empty;

            #region Object Graph Loading

            string quickDebugStatus = quickDebug ? "On" : "Off";
            string headStartStatus = headStart ? args[1].Substring(0,5) : "Off";
            string welcomeHeader = string.Format("GitSpect v0.0.1 | QuickDebug {0} | HeadStart {1}", quickDebugStatus, headStartStatus);

            Console.WriteLine(ONE_LINE_TO_RULE_THEM_ALL);
            Console.WriteLine(welcomeHeader);
            Console.WriteLine(ONE_LINE_TO_RULE_THEM_ALL);
            _objectGraph = headStart ? new GitObjectGraph(args[1]) : new GitObjectGraph();
            IEnumerable<PSObject> gitObjectHints;

            // Get the first two letters of all the git objects 
            // (also path and info, but we don't care about those yet)
            string poshCommand = quickDebug ? PowerShellCommands.GET_LAST_5_MINUTES : PowerShellCommands.GET_ALL;
            string headStartLookup = string.Format(PowerShellCommands.CD_BASE + "; " + PowerShellCommands.OBJECT_TEMPLATE, headStartSha.Substring(0,2));
            poshCommand = headStart ? headStartLookup : poshCommand; 

            // Note : HeadStart setting overrides QuickDebug 

            gitObjectHints = ExecuteCommand(poshCommand);
            
            Stopwatch allObjsTimer = new Stopwatch();
            int totalSizeOfAllGraphObjects = 0;
            int totalNumberOfGraphObjects = 0;
            allObjsTimer.Start();

            foreach (var hint in gitObjectHints)
            {
                bool firstObjInDirectory = true;
                Stopwatch objTimer = new Stopwatch();
                objTimer.Start();
                IEnumerable<GitObject> gitObjs = headStart ? _objectGraph.TraverseStartingAtCommit(headStartSha, 15) : _objectGraph.ProcessPSObjectIntoGitObjects(hint.ToString());
                objTimer.Stop();

                foreach (var gitObj in gitObjs)
                {
                    if (!firstObjInDirectory) Console.WriteLine();

                    _objectGraph.CacheGitObject(gitObj);

                    // Track stats
                    totalSizeOfAllGraphObjects += gitObj.Size;
                    totalNumberOfGraphObjects++;

                    // Report to the console
                    string reportTemplate = "SHA: {0} Size: {1} Type: {2} ";
                    string type;
                    switch (gitObj.Type)
                    {
                        case GitObjects.Blob:
                            type = " B ";
                            break;
                        case GitObjects.Tree:
                            type = " T ";
                            break;
                        case GitObjects.Commit:
                            type = " C ";
                            break;
                        case GitObjects.MergeCommit:
                            type = " M ";
                            break;
                        default:
                            type = " | ";
                            break;
                    }

                    string report = string.Format(reportTemplate, gitObj.SHA.Substring(0, 5),
                                                    gitObj.Size.ToString("D5"), type);

                    Console.Write(report);
                    firstObjInDirectory = false;
                }

                string stats = string.Format("Directory {0} parsed. Took {1} ms", hint, objTimer.ElapsedMilliseconds);
                Console.Write(stats);
                Console.WriteLine();
            }

            allObjsTimer.Stop();

            int elapsedHours = allObjsTimer.Elapsed.Hours;
            int elapsedMinutes = allObjsTimer.Elapsed.Minutes;
            int elapsedSeconds = allObjsTimer.Elapsed.Seconds;
            int elapsedMilliSeconds = allObjsTimer.Elapsed.Milliseconds;
            string overallReport = string.Format("--- Objects loaded --- {0}:{1}:{2}.{3}. Bytes: {4}, # Objs: {5}",
                elapsedHours, elapsedMinutes, elapsedSeconds, elapsedMilliSeconds, totalSizeOfAllGraphObjects, totalNumberOfGraphObjects);
            Console.WriteLine(ONE_LINE_TO_RULE_THEM_ALL);
            Console.WriteLine(overallReport);
            Console.WriteLine(ONE_LINE_TO_RULE_THEM_ALL);

            var reportingPath = Path.Combine(REPO_BASE, "SizeLog.txt");
            var writer = File.AppendText(reportingPath);
            writer.Write(DateTime.Now.ToString() + " " + overallReport);
            writer.Flush();
            writer.Close();
#endregion

            // Finally, the actual command loop
            CommandProcessor processor = new CommandProcessor(_objectGraph);

            while (true)
            {
                string handle = processor.CurrentHandle;
                Console.Write("{0}> ", handle);
                string[] cmdArgs = null;
                string[] command = GetCommand();
                Commands mainCommand = Commands.Unknown;
                Enum.TryParse(command[0], out mainCommand);

                if(command.Length > 1)
                {
                    cmdArgs = new string[command.Length - 1];

                    for(int i = 1; i < command.Length; i++)
                    {
                        cmdArgs[i-1] = command[i];
                    }
                }

                var result = processor.Process(mainCommand, cmdArgs);
                string report = result == null || string.IsNullOrEmpty(result.SHA) ? "No Object Found" : result.ToString();
                Console.WriteLine(report);
            }
        }

        private static string[] GetCommand()
        {
            string[] retVal = null;

            string stringCommand = Console.ReadLine();
            retVal = stringCommand.Split(' ');

            return retVal;
        }       

        /// <summary>
        /// Execute a command string using PowerShell, synchronously
        /// </summary>
        /// <param name="command"></param>
        /// <returns>The PSObjects returned by the command invocation</returns>
        public static IEnumerable<PSObject> ExecuteCommand(string command)
        {
            IEnumerable<PSObject> results;

            using (PowerShell posh = PowerShell.Create())
            {
                posh.AddScript(command);
                results = posh.Invoke();
            }

            return results;
        }
    }
}
