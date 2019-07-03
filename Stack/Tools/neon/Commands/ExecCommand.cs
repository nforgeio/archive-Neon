//-----------------------------------------------------------------------------
// FILE:	    ExecCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;

using Neon.Cluster;
using Neon.Stack.Common;
using Neon.Stack.IO;

namespace NeonCluster
{
    /// <summary>
    /// Implements the <b>exec</b> command.
    /// </summary>
    public class ExecCommand : ICommand
    {
        private const string usage = @"
Executes a Bash command or script on one or more cluster host nodes.

USAGE:

    neon exec [OPTIONS] COMMAND

ARGUMENTS:

    COMMAND                 - The command to be executed.
    NODES                   - Zero are more target node names, an asterisk
                              to target to all nodes.  Exdcutes on the first
                              manager if no node is specified'
OPTIONS:

    --neon-nodes            - Zero are more target node names (separated by commas)
                              or an asterisk to target to all nodes.  Executes on 
                              a cluster manager if no node is specified.

    --neon-text=PATH        - Text file path to be uploaded to the node(s) before
                              executing the command.  Multiple are allowed.

    --neon-data=PATH        - Binary file path to be uploaded to the node(s) before
                              executing the command.  Multiple are allowed.

    --neon-script=PATH      - Script file path to be uploaded to the node(s).
                              Uploaded scripts will have 700 permissions.
                              Multiple are allowed.

    --neon-max-parallel=#   - Maximum number of nodes to execute the command on
                              in parallel.  Defaults to 1.

    --neon-wait=SECONDS     - Number of seconds to wait after the command has run.

NOTES:

    * Any files specified by [--neon-text, --neon-data, --neon-script] options 
      will be uploaded to a temporary directory first and then the command 
      will be executed with that as the current working directory.  The 
      temporary directory will removed after the command completes.

    * Other than the above, commands should make no assumption about
      the current working directory.

    * If the command targets a single node, the command output will be
      written to standard output and [neon.exe] will return the command
      exit code.

    * If the command targets multiple nodes, the command output will
      be written to the node log files and [neon.exe] will return a
      0 exit code if all of the node commands returned 0, othwerise 1.

    * Commands are executed with [sudo] privileges. 

EXAMPLES:

List the Docker nodes on a cluster manager:

    neon exec docker node ls

Upgrade Linux packages on all nodes:

    neon exec apt-get update && apt-get dist-upgrade -yq *

Upload the [foo.sh] script and the [bar.txt] text file and then execute
the script on two specific nodes:

    neon exec --neon-script=foo.sh --neon-text=bar.txt --neon-nodes=mynode-1,mynode-2 .\foo.sh

It is not possible to upload and execute a Linux binary directly but
you can change its permissions first and then execute it:

    neon exec --neon-data=myapp chmod 700 myapp && ./myapp
";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "exec" }; }
        }

        /// <inheritdoc/>
        public string[] ExtendedOptions
        {
            get { return new string[0]; }
        }
    
        /// <inheritdoc/>
        public bool NeedsSshCredentials
        {
            get { return false; }
        }

        /// <inheritdoc/>
        public bool IsPassThru
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public void Help()
        {
            Console.WriteLine(usage);
        }

        /// <inheritdoc/>
        public void Run(CommandLine commandLine)
        {
            if (commandLine.HasHelpOption)
            {
                Console.WriteLine(usage);
                Program.Exit(0);
            }

            var clusterSecrets = Program.ClusterSecrets;

            if (clusterSecrets == null)
            {
                Console.Error.WriteLine(Program.MustLoginMessage);
                Program.Exit(1);
            }

            // Process the nodes.

            if (commandLine.Arguments.Length < 2)
            {
                Console.WriteLine(usage);
                Program.Exit(1);
            }

            var nodeDefinitions = new List<NodeDefinition>();
            var nodesOption     = commandLine.GetOption("--neon-node", null);

            if (string.IsNullOrWhiteSpace(nodesOption))
            {
                nodeDefinitions.Add(clusterSecrets.Definition.Managers.First());
            }
            else if (nodesOption == "*")
            {
                foreach (var manager in clusterSecrets.Definition.SortedManagers)
                {
                    nodeDefinitions.Add(manager);
                }

                foreach (var worker in clusterSecrets.Definition.SortedWorkers)
                {
                    nodeDefinitions.Add(worker);
                }
            }
            else
            {
                foreach (var name in nodesOption.Split(','))
                {
                    var trimmedName = name.Trim();

                    NodeDefinition node;

                    if (!clusterSecrets.Definition.NodeDefinitions.TryGetValue(trimmedName, out node))
                    {
                        Console.WriteLine($"*** Error: Node [{trimmedName}] is not present in the cluster.");
                        Program.Exit(1);
                    }

                    nodeDefinitions.Add(node);
                }
            }

            // Process the command line options.

            var maxParallelOption = commandLine.GetOption("--neon-max-parallel", "1");
            int maxParallel;

            if (!int.TryParse(maxParallelOption, out maxParallel) || maxParallel < 1)
            {
                Console.Error.WriteLine($"*** ERROR: [--neon-max-parallel={maxParallelOption}] option is not valid.");
                Program.Exit(1);
            }

            Program.MaxParallel = maxParallel;

            var     waitSecondsOption = commandLine.GetOption("--neon-wait", "0");
            double  waitSeconds;

            if (!double.TryParse(waitSecondsOption, out waitSeconds) || waitSeconds < 0)
            {
                Console.Error.WriteLine($"*** ERROR: [--neon-wait={waitSecondsOption}] option is not valid.");
                Program.Exit(1);
            }

            Program.WaitSeconds = waitSeconds;

            // Create the command bundle by skipping the first command line item (the "exec") and
            // then appending the remaining items, ignoring any options that start with "--neon-".

            string command = null;
            var     items   = new List<string>();

            foreach (var item in commandLine.Items.Skip(1))
            {
                if (item.StartsWith("--neon-"))
                {
                    continue;
                }

                if (command == null)
                {
                    command = item;
                }
                else
                {
                    items.Add(item);
                }
            }

            var bundle = new CommandBundle(command, items.ToArray());

            // Append any script, text, or data files to the bundle.

            foreach (var scriptPath in commandLine.GetOptionValues("--neon-script"))
            {
                if (!File.Exists(scriptPath))
                {
                    Console.WriteLine($"*** Error: Script [{scriptPath}] does not exist.");
                    Program.Exit(1);
                }

                bundle.AddFile(Path.GetFileName(scriptPath), File.ReadAllText(scriptPath), isExecutable: true);
            }

            foreach (var textPath in commandLine.GetOptionValues("--neon-text"))
            {
                if (!File.Exists(textPath))
                {
                    Console.WriteLine($"*** Error: Text file [{textPath}] does not exist.");
                    Program.Exit(1);
                }

                bundle.AddFile(Path.GetFileName(textPath), File.ReadAllText(textPath));
            }

            foreach (var dataPath in commandLine.GetOptionValues("--neon-data"))
            {
                if (!File.Exists(dataPath))
                {
                    Console.WriteLine($"*** Error: Data file [{dataPath}] does not exist.");
                    Program.Exit(1);
                }

                bundle.AddFile(Path.GetFileName(dataPath), File.ReadAllBytes(dataPath));
            }

            // Perform the operation.

            var cluster = new ClusterProxy(Program.ClusterSecrets, Program.CreateNodeProxy<NodeDefinition>);

            if (nodeDefinitions.Count == 1)
            {
                // Run the command on a single node and return the output and exit code.

                var node     = cluster.GetNode(nodeDefinitions.First().Name);
                var response = node.SudoCommand(bundle);

                Console.WriteLine(response.OutputText);

                if (Program.WaitSeconds > 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(Program.WaitSeconds));
                }

                Program.Exit(response.ExitCode);
            }
            else
            {
                // Run the command on multiple nodes and return an overall exit code.

                var operation = new SetupController(Program.SafeCommandLine, cluster.Nodes.Where(n => nodeDefinitions.Exists(nd => nd.Name == n.Name)));

                operation.AddWaitUntilOnlineStep();
                operation.AddStep($"run: {bundle.Command}",
                    node =>
                    {
                        node.Status = "running";
                        node.SudoCommand(bundle, RunOptions.FaultOnError | RunOptions.LogOutput);

                        if (Program.WaitSeconds > 0)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(Program.WaitSeconds));
                        }
                    });

                if (!operation.Run())
                {
                    Console.Error.WriteLine("*** ERROR: [exec] on one or more nodes failed.");
                    Program.Exit(1);
                }
            }
        }
    }
}
