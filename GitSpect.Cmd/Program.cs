using System;
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
            bool headStart = args.Length >= 2 && !string.IsNullOrEmpty(args[1]);
            string headStartSha = headStart ? args[1] : string.Empty;

            #region Object Graph Loading

            string quickDebugStatus = quickDebug ? "On" : "Off";
            string headStartStatus = headStart ? args[1].Substring(0, 5) : "Off";
            string welcomeHeader = string.Format("GitSpect ALPHA | QuickDebug {0} | HeadStart {1}", quickDebugStatus, headStartStatus);
            Stopwatch firstLoad = new Stopwatch();
            firstLoad.Start();

            Console.WriteLine(ONE_LINE_TO_RULE_THEM_ALL);
            Console.WriteLine(welcomeHeader);
            Console.WriteLine(ONE_LINE_TO_RULE_THEM_ALL);
            _objectGraph = headStart ? new GitObjectGraph(args[1]) : new GitObjectGraph();
            IEnumerable<PSObject> gitObjectHints;

            // This could be a lot less wordy
            string classicCommand = quickDebug ?
                string.Format(PowerShellCommands.CD_BASE + "; " +
                PowerShellCommands.GET_LAST_5_MINUTES) :
                string.Format(PowerShellCommands.CD_BASE + "; " +
                PowerShellCommands.GET_ALL);
            string headStartCommand = headStart ?
                string.Format(PowerShellCommands.CD_BASE + "; " +
                PowerShellCommands.OBJECT_TEMPLATE, headStartSha.Substring(0, 2)) :
                string.Empty;
            string poshCommand = headStart ? headStartCommand : classicCommand;

            // Note : HeadStart setting overrides QuickDebug 
            // Get our starting PSObject(s) - two character folder name(s)
            gitObjectHints = ExecuteCommand(poshCommand);

            gitObjectHints = headStart ? 
                gitObjectHints.Where(x => x.ToString() == headStartSha.Substring(0,2)).ToList() : 
                gitObjectHints;

            Stopwatch allObjsTimer = new Stopwatch();
            int totalSizeOfAllGraphObjects = 0;
            int totalNumberOfGraphObjects = 0;
            allObjsTimer.Start();

            // Multiple hints when headstart == false, One when true
            foreach (var hint in gitObjectHints)
            {
                int depthTracker = 0;
                string currentCommitSha = headStart ? headStartSha : string.Empty;
                do
                {
                    bool firstObjInDirectory = true;
                    Stopwatch dirTimer = new Stopwatch();

                    dirTimer.Start();
                    IEnumerable<GitObject> gitObjs = headStart ?
                        _objectGraph.SloppyLoadCommit(currentCommitSha, true) :
                        _objectGraph.ProcessPSObjectIntoGitObjects(hint.ToString());
                    dirTimer.Stop();

                    foreach (var gitObj in gitObjs)
                    {
                        if (!firstObjInDirectory) Console.WriteLine();

                        // Keep track of this object
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

                    string parsedDirectory = headStart ? currentCommitSha.Substring(0, 2) : hint.ToString();
                    string stats = string.Format("Directory {0} parsed. Took {1} ms", parsedDirectory, dirTimer.ElapsedMilliseconds);
                    Console.Write(stats);
                    Console.WriteLine();

                    if (headStart)
                    {
                        GitObject thisObject;
                        _objectGraph.LookupObject(currentCommitSha, out thisObject);
                        Commit thisCommit = (Commit)thisObject;

                        currentCommitSha = thisCommit.Parent;
                    }

                    // Yup, this is how the headstart starting depth limit is hardcoded
                } while (headStart && ++depthTracker < 3);

                allObjsTimer.Stop();

                // Record stats
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

                // Finally, the actual command loop (maintain graph state out here.)
                CommandProcessor processor = new CommandProcessor();

                while (true)
                {
                    string handle = processor.CurrentHandle;
                    Console.Write("{0}> ", handle);

                    // Fragile
                    string[] cmdArgs = null;
                    string command = GetCommand();
                    string hopefullyParseable = command.ToLower().ToTitleCase();
                    string[] parsed = hopefullyParseable.Split(' ');

                    Commands mainCommand = Commands.Unknown;
                    Enum.TryParse(parsed[0], out mainCommand);

                    if (parsed.Length > 1)
                    {
                        cmdArgs = new string[parsed.Length - 1];

                        for (int i = 1; i < parsed.Length; i++)
                        {
                            cmdArgs[i - 1] = parsed[i];
                        }
                    }

                    var result = processor.Process(mainCommand, _objectGraph, cmdArgs);
                    string report = result == null || string.IsNullOrEmpty(result.SHA) ? "No Object Found." : result.ToString();
                    Console.WriteLine(report);
                }
            }
        }

        private static string GetCommand()
        {
            string retVal = null;
            retVal = Console.ReadLine();
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
