//-----------------------------------------------------------------------------
// FILE:	    LoginCommand.cs
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
    /// Implements the <b>login</b> command.
    /// </summary>
    public class LoginCommand : ICommand
    {
        private const string usage = @"
Logs into the named cluster, making that cluster the current cluster for
subsequent commands.  Pass a CLUSTER to login or ommit it to print the
current login status.

Use [neon logout] to clear the persisted credentials.

USAGE:

    neon login [CLUSTER]

ARGUMENTS:

    CLUSTER     - The cluster name.
";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "login" }; }
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
            ClusterProxy    clusterProxy;

            if (commandLine.HasHelpOption)
            {
                Console.WriteLine(usage);
                Program.Exit(0);
            }

            var clusterSecrets = Program.ClusterSecrets;

            // Just print the current login status if no cluster name was passed.

            if (commandLine.Arguments.Length < 1)
            {
                if (Program.ClusterSecrets == null)
                {
                    Console.WriteLine("*** You are not logged in.");
                    Program.Exit(1);
                }

                // Parse and validate the cluster definition.

                clusterProxy = new ClusterProxy(clusterSecrets,
                    (dnsName, nodeName) =>
                    {
                        return new NodeProxy<NodeDefinition>(nodeName, dnsName, clusterSecrets.GetSshCredentials(), TextWriter.Null);
                    });

                // Verify the credentials by logging into a manager node.

                Console.WriteLine($"*** Checking login status for [{clusterSecrets.Name}]...");

                try
                {
                    clusterProxy.Manager.Connect();

                    Console.WriteLine("*** You are logged in.");
                }
                catch
                {
                    Console.WriteLine("*** ERROR: Login failed.");
                    Console.WriteLine("");
                }

                return;
            }

            // Logout from the current cluster (if any).

            var clusterName = commandLine.Arguments[0];

            if (clusterSecrets != null && !string.Equals(clusterSecrets.Name, clusterName, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"*** Logging out of [{Program.ClusterSecrets.Name}].");
                File.Delete(Program.CurrentClusterPath);
            }

            var clusterSecretsPath = Program.GetClusterSecretsPath(clusterName);

            if (!File.Exists(clusterSecretsPath))
            {
                Console.Error.WriteLine($"*** ERROR: Unknown cluster [{clusterName}].");
                Program.Exit(1);
            }

            clusterSecrets = NeonHelper.JsonDeserialize<ClusterSecrets>(File.ReadAllText(clusterSecretsPath));

            clusterProxy = new ClusterProxy(clusterSecrets,
                (dnsName, nodeName) =>
                {
                    return new NodeProxy<NodeDefinition>(nodeName, dnsName, clusterSecrets.GetSshCredentials(), TextWriter.Null);
                });

            // Verify the credentials by logging into a manager node.

            try
            {
                clusterProxy.Manager.Connect();

                File.WriteAllText(Program.CurrentClusterPath, clusterSecrets.Name);

                Console.WriteLine($"*** You are logged into [{clusterSecrets.Name}].");
                Console.WriteLine("");
            }
            catch
            {
                Console.WriteLine("*** ERROR: Login failed.");
                Console.WriteLine("");
            }
        }
    }
}
