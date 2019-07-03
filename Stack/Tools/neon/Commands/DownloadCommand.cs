//-----------------------------------------------------------------------------
// FILE:	    DownloadCommand.cs
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
    /// Implements the <b>download</b> command.
    /// </summary>
    public class DownloadCommand : ICommand
    {
        private const string usage = @"
Uploads a file to one or more cluster hosts.

USAGE:

    neon download SOURCE TARGET [NODE]

ARGUMENTS:

    SOURCE              - Path to the source file on the remote node.
    TARGET              - Path to the destination file on the local workstation.
    NODE                - Identifies the source node.  Downloads from the
                          the first manager node otherwise.                            

NOTES:

    * TARGET must be the full destination path including the file name.
    * Any required destination folders will be created if missing.
";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "download" }; }
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

            NodeDefinition      nodeDefinition;
            string              source;
            string              target;

            if (commandLine.Arguments.Length < 1)
            {
                Console.WriteLine("*** Error: SOURCE file was not specified.");
                Program.Exit(1);
            }

            source = commandLine.Arguments[0];

            if (commandLine.Arguments.Length < 2)
            {
                Console.WriteLine("*** Error: TARGET file was not specified.");
                Program.Exit(1);
            }

            target = commandLine.Arguments[1];

            if (commandLine.Arguments.Length == 2)
            {
                nodeDefinition = clusterSecrets.Definition.Managers.First();
            }
            else if (commandLine.Arguments.Length == 3)
            {
                var name = commandLine.Arguments[2];

                if (!clusterSecrets.Definition.NodeDefinitions.TryGetValue(name, out nodeDefinition))
                {
                    Console.WriteLine($"*** Error: Node [{name}] is not present in the cluster.");
                    Program.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("*** Error: A maximum of one node can be specified.");
                Program.Exit(1);
                return;
            }

            // Perform the download.

            var cluster   = new ClusterProxy(Program.ClusterSecrets, Program.CreateNodeProxy<NodeDefinition>);
            var operation = new SetupController(Program.SafeCommandLine, cluster.Nodes.Where(n => n.Name == nodeDefinition.Name));

            operation.AddWaitUntilOnlineStep();
            operation.AddStep("download",
                node =>
                {
                    node.Status = "downloading";

                    node.Download(source, target); 
                });

            if (!operation.Run())
            {
                Console.Error.WriteLine("*** ERROR: The download failed.");
                Program.Exit(1);
            }
        }
    }
}
