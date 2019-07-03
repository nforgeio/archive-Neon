//-----------------------------------------------------------------------------
// FILE:	    HealthCommand.cs
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
    /// Implements the <b>health</b> command.
    /// </summary>
    public class HealthCommand : ICommand
    {
        private const string usage = @"
Verifies the health of the cluster nodes.

USAGE:

    neon health

";
        private ClusterProxy cluster;

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "health" }; }
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
            if (Program.ClusterSecrets == null)
            {
                Console.Error.WriteLine(Program.MustLoginMessage);
                Program.Exit(1);
            }

            cluster = new ClusterProxy(Program.ClusterSecrets, Program.CreateNodeProxy<NodeDefinition>, RunOptions.LogOutput | RunOptions.FaultOnError);

            // Perform the operation.

            var operation = new SetupController(Program.SafeCommandLine, cluster.Nodes);

            operation.AddWaitUntilOnlineStep();
            operation.AddStep("manager health check",  n => ClusterDiagnostics.CheckClusterManager(n, cluster.Definition), n => n.Metadata.Manager);
            operation.AddStep("worker health check",  n => ClusterDiagnostics.CheckClusterWorker(n, cluster.Definition), n => n.Metadata.Worker);
            operation.AddGlobalStep("logging diagnostics", () => ClusterDiagnostics.CheckLogServices(cluster));

            if (!operation.Run())
            {
                Console.Error.WriteLine("*** ERROR: One or more health checks failed.");
                Program.Exit(1);
            }
        }
    }
}
