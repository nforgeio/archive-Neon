//-----------------------------------------------------------------------------
// FILE:	    CommonSteps.cs
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
    /// Implements common configuration steps.
    /// </summary>
    public static class CommonSteps
    {
        /// <summary>
        /// Verifies that the node has the correct operating system installed.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        public static void VerifyOS(NodeProxy<NodeDefinition> node)
        {
            node.Status = "verify: operating system";

            var response = node.SudoCommand("lsb_release -a");

            switch (Program.OSProperties.TargetOS)
            {
                case TargetOS.Ubuntu_16_04:

                    if (!response.OutputText.Contains("Ubuntu 16.04"))
                    {
                        node.Fault("Expected [Ubuntu 16.04].");
                    }
                    break;

                default:

                    throw new NotImplementedException($"Support for [{nameof(TargetOS)}.{Program.OSProperties.TargetOS}] is not implemented.");
            }
        }

        /// <summary>
        /// <para>
        /// Verifies that the node is not already fully or partially configured as NeonCluster node.
        /// </para>
        /// <note>
        /// Note that a prepared node still qualifies as pristine.
        /// </note>
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        public static void VerifyPristine(NodeProxy<NodeDefinition> node)
        {
            var bundle = new CommandBundle("./verify-pristine.sh");
            var script =
$@"
if [ -d  {NodeHostFolders.State} ] ; then 

    if ls -l {NodeHostFolders.State} | egrep -v '(total|prep-node)' ; then
        echo ""*** ERROR: This node is partially or fully configured into an existing cluster."" 1>&2
        exit 1
    fi
fi
";
            bundle.AddFile("verify-pristine.sh", script, isExecutable: true);

            node.SudoCommand(bundle);
        }

        /// <summary>
        /// Initializes a near virgin server with the basic capabilities required
        /// for a NeonCluster host node.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        /// <param name="clusterDefinition">The optional cluster definition.</param>
        /// <param name="shutdown">Optionally shuts down the node.</param>
        public static void PrepareNode(NodeProxy<NodeDefinition> node, ClusterDefinition clusterDefinition = null, bool shutdown = false)
        {
            node.InitializeNeonFolders();
            node.UploadConfigFiles(clusterDefinition);
            node.UploadTools(clusterDefinition);

            node.Status = "run: setup-apt-ready.sh";
            node.SudoCommand("setup-apt-ready.sh");

            if (clusterDefinition != null)
            {
                ConfigureEnvironmentVariables(node, clusterDefinition);
            }

            if (clusterDefinition != null && !string.IsNullOrEmpty(clusterDefinition.PackageCache))
            {
                node.Status = "run: setup-apt-proxy.sh";
                node.SudoCommand("setup-apt-proxy.sh");
            }

            node.Status = "run: setup-prep-node.sh";
            node.SudoCommand("setup-prep-node.sh");

            // Wait for the server a chance to perform any post-update updates.

            node.Status = "run: setup-apt-ready.sh";
            node.SudoCommand("setup-apt-ready.sh");

            // Reboot the server to ensure that all changes are live.

            node.Reboot(wait: true);

            // Wait for the server a chance to perform any post-boot updates.

            node.Status = "run: setup-apt-ready.sh";
            node.SudoCommand("setup-apt-ready.sh");

            // Clear any DHCP leases.

            node.Status = "clearing DHCP leases";
            node.SudoCommand("rm -f /var/lib/dhcp/*");

            // Shutdown the node if requested.

            if (shutdown)
            {
                node.Status = "shutdown";
                node.SudoCommand("shutdown 0");
            }
        }

        /// <summary>
        /// Configures the global environment variables that describe the configuration 
        /// of the server within the cluster.
        /// </summary>
        /// <param name="node">The server to be updated.</param>
        /// <param name="clusterDefinition">The optional cluster definition.</param>
        private static void ConfigureEnvironmentVariables(NodeProxy<NodeDefinition> node, ClusterDefinition clusterDefinition = null)
        {
            node.Status = "setup: environment...";

            // We're going to append the new variables to the existing Linux [/etc/environment] file.

            var sb = new StringBuilder();

            // Append all of the existing environment variables except for those
            // whose names start with "NEON_" to make the operation idempotent.

            using (var currentEnvironmentStream = new MemoryStream())
            {
                node.Download("/etc/environment", currentEnvironmentStream);

                currentEnvironmentStream.Position = 0;

                using (var reader = new StreamReader(currentEnvironmentStream))
                {
                    foreach (var line in reader.Lines())
                    {
                        if (!line.StartsWith("NEON_"))
                        {
                            sb.AppendLine(line);
                        }
                    }
                }
            }

            // Add any necessaery Neon related environment variables. 

            sb.AppendLine($"NEON_APT_CACHE={clusterDefinition.PackageCache ?? string.Empty}");

            // Upload the new environment to the server.

            node.UploadText("/etc/environment", sb.ToString(), tabStop: 4);
        }
    }
}
