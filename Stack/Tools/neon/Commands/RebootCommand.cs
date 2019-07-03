//-----------------------------------------------------------------------------
// FILE:	    RebootCommand.cs
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
    /// Implements the <b>reboot</b> command.
    /// </summary>
    public class RebootCommand : ICommand
    {
        private const string usage = @"
Reboots one or more cluster host nodes.

USAGE:

    neon reboot [OPTIONS] NODE...

ARGUMENTS:

    NODE                - One or more target node names, an asterisk
                          to upload to all nodes.
NOTES:

The common [-w/--wait] option specifies the number of seconds to wait
for each node to stablize after it has successfully rebooted.  This
defaults to 60 seconds.
";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "reboot" }; }
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

            // Process the command arguments.

            var nodeDefinitions = new List<NodeDefinition>();

            if (commandLine.Arguments.Length < 1)
            {
                Console.WriteLine("*** Error: At least one NODE must be specified.");
                Program.Exit(1);
            }

            if (commandLine.Arguments.Length == 1 && commandLine.Arguments[0] == "*")
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
                foreach (var name in commandLine.Arguments)
                {
                    NodeDefinition node;

                    if (!clusterSecrets.Definition.NodeDefinitions.TryGetValue(name, out node))
                    {
                        Console.WriteLine($"*** Error: Node [{name}] is not present in the cluster.");
                        Program.Exit(1);
                    }

                    nodeDefinitions.Add(node);
                }
            }

            // Perform the reboots.

            var cluster   = new ClusterProxy(Program.ClusterSecrets, Program.CreateNodeProxy<NodeDefinition>);
            var operation = new SetupController(Program.SafeCommandLine, cluster.Nodes.Where(n => nodeDefinitions.Exists(nd => nd.Name == n.Name)));

            operation.AddWaitUntilOnlineStep();
            operation.AddStep("reboot nodes",
                n =>
                {
                    n.Status = "rebooting";
                    n.Reboot();

                    n.Status = $"stablizing ({Program.WaitSeconds}s)";
                    Thread.Sleep(TimeSpan.FromSeconds(Program.WaitSeconds));
                });

            if (!operation.Run())
            {
                Console.Error.WriteLine("*** ERROR: The reboot for one or more nodes failed.");
                Program.Exit(1);
            }
        }
    }
}
