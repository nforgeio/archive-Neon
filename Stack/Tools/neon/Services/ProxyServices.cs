//-----------------------------------------------------------------------------
// FILE:	    ProxyServices.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Consul;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Cluster;
using Neon.Stack.Common;
using Neon.Stack.IO;
using Neon.Stack.Net;
using Neon.Stack.Time;

namespace NeonCluster
{
    /// <summary>
    /// Handles the provisioning of the cluster proxy related services: <b>neon-proxy-manager</b>,
    /// <b>neon-proxy-public</b> and <b>neon-proxy-private</b>.
    /// </summary>
    public class ProxyServices
    {
        private ClusterProxy cluster;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cluster">The cluster proxy.</param>
        public ProxyServices(ClusterProxy cluster)
        {
            Covenant.Requires<ArgumentNullException>(cluster != null);

            this.cluster = cluster;
        }

        /// <summary>
        /// Configures the cluster proxy related services.
        /// </summary>
        public void Configure()
        {
            // Ensure that Vault has been initialized.

            if (cluster.Secrets.VaultCredentials == null)
            {
                throw new InvalidOperationException("Vault has not been initialized yet.");
            }

            // Obtain the AppRole credentials from Vault for the proxy manager as well as the
            // public and private proxy services and persist these as Docker secrets.

            cluster.DockerSecret.Set("neon-proxy-manager-credentials", NeonHelper.JsonSerialize(cluster.Vault.GetAppRoleCredentialsAsync("neon-proxy-manager").Result, Formatting.Indented));
            cluster.DockerSecret.Set("neon-proxy-public-credentials", NeonHelper.JsonSerialize(cluster.Vault.GetAppRoleCredentialsAsync("neon-proxy-public").Result, Formatting.Indented));
            cluster.DockerSecret.Set("neon-proxy-private-credentials", NeonHelper.JsonSerialize(cluster.Vault.GetAppRoleCredentialsAsync("neon-proxy-private").Result, Formatting.Indented));

            // Deploy the proxy manager service.

            cluster.Manager.DockerCommand(
                "docker service create",
                    "--name", "neon-proxy-manager",
                    "--mount", "type=bind,src=/etc/neoncluster/env-host,dst=/etc/neoncluster/env-host,readonly=true",
                    "--mount", "type=bind,src=/etc/ssl/certs,dst=/etc/ssl/certs,readonly=true",
                    "--env", "VAULT_CREDENTIALS=neon-proxy-manager-credentials",
                    "--env", "LOG_LEVEL=INFO",
                    "--secret", "neon-proxy-manager-credentials",
                    "--constraint", "node.role==manager",
                    "--replicas", 1,
                    "neoncluster/neon-proxy-manager");

            // Initialize the public and private proxies.

            string proxyConstraint;

            cluster.PublicProxy.UpdateSettings(
                new ProxySettings()
                {
                    FirstPort = NeonHostPorts.ProxyPublicFirst,
                    LastPort  = NeonHostPorts.ProxyPublicLast

                });

            cluster.PrivateProxy.UpdateSettings(
                new ProxySettings()
                {
                    FirstPort = NeonHostPorts.ProxyPrivateFirst,
                    LastPort  = NeonHostPorts.ProxyPrivateLast

                });

            if (cluster.Definition.Workers.Count() > 0)
            {
                // Constrain proxies to all worker nodes if there are any.

                proxyConstraint = "node.role!=manager";
            }
            else
            {
                // Constrain proxies to manager nodes nodes if there are no workers.

                proxyConstraint = "node.role==manager";
            }

            cluster.Manager.DockerCommand(
                "docker service create",
                    "--name", "neon-proxy-public",
                    "--mount", "type=bind,src=/etc/neoncluster/env-host,dst=/etc/neoncluster/env-host,readonly=true",
                    "--mount", "type=bind,src=/etc/ssl/certs,dst=/etc/ssl/certs,readonly=true",
                    "--env", "CONFIG_KEY=neon/service/neon-proxy-manager/proxies/public/conf",
                    "--env", "VAULT_CREDENTIALS=neon-proxy-public-credentials",
                    "--env", "LOG_LEVEL=INFO",
                    "--env", "DEBUG=false",
                    "--publish", $"{NeonHostPorts.ProxyPublicFirst}-{NeonHostPorts.ProxyPublicLast}:{NeonHostPorts.ProxyPublicFirst}-{NeonHostPorts.ProxyPublicLast}",
                    "--secret", "neon-proxy-public-credentials",
                    "--constraint", proxyConstraint,
                    "--mode", "global",
                    "--network", NeonClusterConst.ClusterPublicNetwork,
                    "neoncluster/neon-proxy");

            cluster.Manager.DockerCommand(
                "docker service create",
                    "--name", "neon-proxy-private",
                    "--mount", "type=bind,src=/etc/neoncluster/env-host,dst=/etc/neoncluster/env-host,readonly=true",
                    "--mount", "type=bind,src=/etc/ssl/certs,dst=/etc/ssl/certs,readonly=true",
                    "--env", "CONFIG_KEY=neon/service/neon-proxy-manager/proxies/private/conf",
                    "--env", "VAULT_CREDENTIALS=neon-proxy-private-credentials",
                    "--env", "LOG_LEVEL=INFO",
                    "--env", "DEBUG=false",
                    "--publish", $"{NeonHostPorts.ProxyPrivateFirst}-{NeonHostPorts.ProxyPrivateLast}:{NeonHostPorts.ProxyPrivateFirst}-{NeonHostPorts.ProxyPrivateLast}",
                    "--secret", "neon-proxy-private-credentials",
                    "--constraint", proxyConstraint,
                    "--mode", "global",
                    "--network", NeonClusterConst.ClusterPrivateNetwork,
                    "neoncluster/neon-proxy");
        }
    }
}
