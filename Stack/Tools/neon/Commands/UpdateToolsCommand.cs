//-----------------------------------------------------------------------------
// FILE:	    UpdateToolsCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;

using Neon.Cluster;
using Neon.Stack.Common;
using Neon.Stack.Time;

namespace NeonCluster
{
    /// <summary>
    /// Implements the <b>update tools</b> command.
    /// </summary>
    public class UpdateToolsCommand : ICommand
    {
        private const string usage = @"
Uploads the current versions of the neon tools and scripts to servers, 
deleting and overwriting the existing scripts.  The command has two
versions:

    * The first loads the cluster definition file and uploads
      the tools and scripts to all nodes in the cluster.

    * The second uploads the tools and scripts to specific nodes
      using their DNS name or IP address.

USAGE:

    neon update tools
    neon update tools SERVER1 [SERVER2...]

ARGUMENTS:

    CLUSTER-DEF     - Path to the cluster definition file.  This is
                      not required if you're logged in.

    SERVER1...      - IP addresses or FQDN of the servers
";
        private List<NodeProxy<NodeDefinition>>     nodes;

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "update-tools" }; }
        }

        /// <inheritdoc/>
        public string[] ExtendedOptions
        {
            get { return new string[0]; }
        }

        /// <inheritdoc/>
        public bool NeedsSshCredentials
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public bool IsPassThru
        {
            get { return false; }
        }

        /// <inheritdoc/>
        public void Help()
        {
            Console.WriteLine(usage);
        }

        /// <inheritdoc/>
        public void Run(CommandLine commandLine)
        {
            // Determine the target nodes.

            nodes = new List<NodeProxy<NodeDefinition>>();

            if (commandLine.Arguments.Length == 0)
            {
                if (Program.ClusterSecrets == null)
                {
                    Console.Error.WriteLine("*** ERROR: You must be logged in or specify one or more server addresses.");
                    Program.Exit(1);
                }
                else
                {
                    foreach (var node in Program.ClusterSecrets.Definition.SortedNodes)
                    {
                        nodes.Add(Program.CreateNodeProxy<NodeDefinition>(node.DnsName, node.Name));
                    }
                }
            }
            else
            {
                foreach (var serverHost in commandLine.GetArguments(0))
                {
                    nodes.Add(Program.CreateNodeProxy<NodeDefinition>(serverHost));
                }
            }

            // Perform the operation.

            var operation = new SetupController(Program.SafeCommandLine, nodes);

            operation.AddWaitUntilOnlineStep("waiting for nodes");
            operation.AddStep("updating tools",
                server =>
                {
                    server.InitializeNeonFolders();
                    server.UploadTools();
                });

            if (!operation.Run())
            {
                Console.Error.WriteLine("*** ERROR: One or more configuration steps failed.");
                Program.Exit(1);
            }
        }

        /// <summary>
        /// Displays server status.
        /// </summary>
        /// <param name="waitForReady">Indicates that status should be displayed continuously until all nodes indicate they're ready or any are faulted.</param>
        private void DisplayStatus(bool waitForReady = false)
        {
            if (waitForReady)
            {
                while (true)
                {
                    DisplayStatus();

                    Thread.Sleep(TimeSpan.FromSeconds(2));

                    if (!nodes.Exists(n => !n.IsReady))
                    {
                        break;
                    }

                    // Display status for another minute if any nodes are faulted 
                    // as a diagnostic and then terminate.  The delay may provide
                    // useful additional diagnostics.

                    if (nodes.Exists(n => n.IsFaulted))
                    {
                        var timer = new PolledTimer(TimeSpan.FromMinutes(1));

                        while (!timer.HasFired)
                        {
                            DisplayStatus();
                            Thread.Sleep(TimeSpan.FromSeconds(2));
                        }
                    }
                }

                DisplayStatus();
            }
            else
            {
                Console.Clear();
                Console.WriteLine(Words);
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($"{"Server",-20}Status");
                Console.WriteLine("-----------------------------------------------------------");

                foreach (var server in nodes)
                {
                    Console.WriteLine($"{server.Name,-20}{server.Status}");
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Sets the <see cref="NodeProxy{TMetadata}.IsReady"/> state for all nodes.
        /// </summary>
        /// <param name="isReady">The new state.</param>
        private void SetServerReady(bool isReady)
        {
            foreach (var server in nodes)
            {
                server.IsReady = isReady;
            }
        }
    }
}
