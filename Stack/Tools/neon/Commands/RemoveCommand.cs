//-----------------------------------------------------------------------------
// FILE:	    RemoveCommand.cs
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
    /// Implements the <b>remove</b> command.
    /// </summary>
    public class RemoveCommand : ICommand
    {
        private const string usage = @"
Removes the administration information for a cluster from the local computer.

USAGE:

    neon remove CLUSTER

ARGUMENTS:

    CLUSTER         - The cluster name.
";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "remove" }; }
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
            if (commandLine.Arguments.Length < 1)
            {
                Console.Error.WriteLine("*** ERROR: CLUSTER is required.");
                Program.Exit(1);
            }

            var clusterName        = commandLine.Arguments[0];
            var clusterSecretsPath = Program.GetClusterSecretsPath(clusterName);

            if (File.Exists(clusterSecretsPath))
            {
                File.Delete(clusterSecretsPath);

                if (string.Equals(Program.ClusterSecrets.Name, clusterName, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(Program.CurrentClusterPath);
                }

                Console.WriteLine($"*** Removed [{clusterName}].");
            }
            else
            {
                Console.WriteLine($"*** Not found.");
            }
        }
    }
}
