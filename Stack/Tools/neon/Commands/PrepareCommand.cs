//-----------------------------------------------------------------------------
// FILE:	    PrepareCommand.cs
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

using Neon.Cluster;
using Neon.Stack.Common;

namespace NeonCluster
{
    /// <summary>
    /// Implements the <b>prepare</b> command.
    /// </summary>
    public class PrepareCommand : ICommand
    {
        private const string usage = @"
Configures near virgin Linux servers so that they are prepared to join a NeonCluster.
You can pass a cluster definition file or the IP addresses or FQDNs of the hosts.

USAGE:

    neon prepare [OPTIONS] [CLUSTER-DEF]
    neon prepare [OPTIONS] SERVER1 [SERVER2...]

ARGUMENTS:

    CLUSTER-DEF     - Path to the cluster definition file.  This is
                      not required when you're logged in.

    SERVER1...      - IP addresses or FQDN of the servers

OPTIONS:

    --package-cache=CACHE-URI   - Optionally specifies an APT Package cache
                                  server to improve setup performance.

Server Requirements:
--------------------

    * Supported version of Linux server.
    * Known admin credentials.
    * OpenSSH installed (or another SSH server)
    * [sudo] configured to elevate permissions without
      asking for a password.
";
        private List<NodeProxy<NodeDefinition>>     nodes;
        private string                              packageCacheUri;

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "prepare" }; }
        }

        /// <inheritdoc/>
        public string[] ExtendedOptions
        {
            get { return new string[] { "--package-cache" }; }
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
            packageCacheUri = commandLine.GetOption("--package-cache");     // This overrides the cluster definition, if specified.

            nodes = new List<NodeProxy<NodeDefinition>>();

            ClusterDefinition clusterDefinition = null;

            if (commandLine.Arguments.Length < 1)
            {
                Console.Error.WriteLine("*** ERROR: A cluster configuration file or at least one server IP address or FQDN must be specified.");
                Program.Exit(1);
            }
            else
            {
                // If there's only one argument and it refers to a file that exists, then
                // assume it's a cluster definition file and initialize the nodes from
                // there.
                //
                // Otherwise, we'll expect the arguments to be server DNS names or IP
                // addresses.

                if (commandLine.Arguments.Length == 1 && File.Exists(commandLine.Arguments[0]))
                {
                    clusterDefinition = ClusterDefinition.FromFile(commandLine.Arguments[0]);

                    if (!string.IsNullOrEmpty(packageCacheUri))
                    {
                        clusterDefinition.PackageCache = packageCacheUri;
                    }

                    foreach (var node in clusterDefinition.SortedNodes)
                    {
                        nodes.Add(Program.CreateNodeProxy<NodeDefinition>(node.DnsName, node.Name));
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(packageCacheUri))
                    {
                        clusterDefinition = new ClusterDefinition()
                        {
                            PackageCache = packageCacheUri
                        };
                    }

                    foreach (var serverHost in commandLine.GetArguments(0))
                    {
                        nodes.Add(Program.CreateNodeProxy<NodeDefinition>(serverHost));
                    }
                }
            }

            // Perform the setup operations.

            var controller = new SetupController(Program.SafeCommandLine, nodes);

            controller.AddWaitUntilOnlineStep();
            controller.AddStep("verify OS", n => CommonSteps.VerifyOS(n));
            controller.AddStep("verify pristine", n => CommonSteps.VerifyPristine(n));
            controller.AddStep("preparing", server => CommonSteps.PrepareNode(server, clusterDefinition, shutdown: true));

            if (!controller.Run())
            {
                Console.Error.WriteLine("*** ERROR: One or more configuration steps failed.");
                Program.Exit(1);
            }
        }
    }
}
