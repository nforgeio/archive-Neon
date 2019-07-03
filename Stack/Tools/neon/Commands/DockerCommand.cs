//-----------------------------------------------------------------------------
// FILE:	    DockerCommand.cs
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

namespace NeonCluster
{
    /// <summary>
    /// Implements the <b>docker</b> commands.
    /// </summary>
    public class DockerCommand : ICommand
    {
        private const string usage = @"
Runs a Docker command on the cluster.  All command line arguments
and options as well are passed through to the Docker CLI.

USAGE:

    neon docker --help                  - Prints Docker (and this) help
    neon docker [OPTIONS] [ARGS...]     - Invokes a Docker command

ARGS: The standard HashCorp Vault command arguments and options.

OPTIONS :

    --neon-node=NODE    - Specifies the target node.  The Vault command will
                          be executed on one of the manager node when this 
                          isn't specified.

NOTE: This command makes no special attempts to upload or download
      any argument or result files between the local workstation and
      the remote server.  Any file operations will occur on the server.
";
        private ClusterProxy        cluster;

        private const string remoteDockerPath = "/usr/bin/docker";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "docker" }; }
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
            var clusterSecrets = Program.ClusterSecrets;
            
            if (commandLine.HasHelpOption && clusterSecrets == null)
            {
                Console.WriteLine(usage);
                Program.Exit(0);
            }

            if (clusterSecrets == null)
            {
                Console.Error.WriteLine(Program.MustLoginMessage);
                Program.Exit(1);
            }

            // Initialize the cluster and connect to a manager.

            cluster = new ClusterProxy(Program.ClusterSecrets, Program.CreateNodeProxy<NodeDefinition>);

            // Strip out the first item of the command line (note that we're
            // not using Shift() because that will reorder any options).

            commandLine = new CommandLine(commandLine.Items.Skip(1).ToArray());

            // Determine which node we're going to target.

            NodeProxy<NodeDefinition>   node;
            var                         nodeName = commandLine.GetOption("--neon-node", null);

            if (!string.IsNullOrEmpty(nodeName))
            {
                node = cluster.GetNode(nodeName);
            }
            else
            {
                node = cluster.Manager;
            }

            // Strip all of the options starting with "--neon-" from the command line.

            var items = new List<string>();

            foreach (var item in commandLine.Items)
            {
                if (!item.StartsWith("--neon-"))
                {
                    items.Add(item);
                }
            }

            commandLine = new CommandLine(items.ToArray());

            // We're going to print help from Vault first followed by
            // help for the [neon.exe] command.

            if (commandLine.HasHelpOption)
            {
                var response = node.SudoCommand($"{remoteDockerPath} {commandLine}", RunOptions.IgnoreRemotePath);

                if (commandLine.Arguments.Length == 0)
                {
                    Console.WriteLine(new string('-', 80));
                    Console.WriteLine("NEON.EXE Help:");
                    Console.WriteLine(usage);

                    Console.WriteLine(new string('-', 80));
                    Console.WriteLine("Docker Help:");
                    Console.WriteLine();
                    Console.WriteLine(response.AllText);

                    Program.Exit(response.ExitCode);
                }
                else
                {
                    Console.WriteLine(response.AllText);
                    Program.Exit(response.ExitCode);
                }
            }

            // We're just going to execute the command as is.

            {
                var response = node.SudoCommand($"{remoteDockerPath} {commandLine}", RunOptions.IgnoreRemotePath);

                Console.WriteLine(response.AllText);
                Program.Exit(response.ExitCode);
            }
        }
    }
}
