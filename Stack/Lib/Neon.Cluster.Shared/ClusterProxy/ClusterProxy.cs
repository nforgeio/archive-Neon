//-----------------------------------------------------------------------------
// FILE:	    ClusterProxy.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Consul;

using Neon.Stack.Common;
using Neon.Stack.IO;
using Neon.Stack.Net;
using Neon.Stack.Retry;
using Neon.Stack.Time;

namespace Neon.Cluster
{
    /// <summary>
    /// Remotely manages a NeonCluster.
    /// </summary>
    public class ClusterProxy : IDisposable
    {
        private object          syncRoot = new object();
        private VaultClient     vaultClient;
        private ConsulClient    consulClient;

        /// <summary>
        /// Constructs a cluster proxy from cluster secrets.
        /// </summary>
        /// <param name="clusterSecrets">The cluster secrets.</param>
        /// <param name="nodeProxyCreator">
        /// The application supplied function that creates a management proxy
        /// given the node address or DNS host and an optional node name.  The
        /// creator accepts to string arguments, the node FQDN/IP address and
        /// the node name.
        /// </param>
        /// <param name="defaultRunOptions">
        /// Optionally specifies the <see cref="RunOptions"/> to be assigned to the 
        /// <see cref="NodeProxy{TMetadata}.DefaultRunOptions"/> property for the
        /// nodes managed by the cluster proxy.  This defaults to <see cref="RunOptions.None"/>.
        /// </param>
        /// <remarks>
        /// The <paramref name="nodeProxyCreator"/> function will be called for each node in
        /// the cluster definition giving the application the chance to create the management
        /// proxy using the node's SSH credentials and also to specify logging.
        /// </remarks>
        public ClusterProxy(ClusterSecrets clusterSecrets, Func<string, string, NodeProxy<NodeDefinition>> nodeProxyCreator, RunOptions defaultRunOptions = RunOptions.None)
            : this(clusterSecrets.Definition, nodeProxyCreator, defaultRunOptions)
        {
            this.Secrets = clusterSecrets;
        }

        /// <summary>
        /// Constructs a cluster proxy from a cluster definition.
        /// </summary>
        /// <param name="clusterDefinition">The cluster secrets.</param>
        /// <param name="nodeProxyCreator">
        /// The application supplied function that creates a management proxy
        /// given the node address or DNS host and an optional node name.  The
        /// creator accepts to string arguments, the node FQDN/IP address and
        /// the node name.
        /// </param>
        /// <param name="defaultRunOptions">
        /// Optionally specifies the <see cref="RunOptions"/> to be assigned to the 
        /// <see cref="NodeProxy{TMetadata}.DefaultRunOptions"/> property for the
        /// nodes managed by the cluster proxy.  This defaults to <see cref="RunOptions.None"/>.
        /// </param>
        /// <remarks>
        /// The <paramref name="nodeProxyCreator"/> function will be called for each node in
        /// the cluster definition giving the application the chance to create the management
        /// proxy using the node's SSH credentials and also to specify logging.
        /// </remarks>
        public ClusterProxy(ClusterDefinition clusterDefinition, Func<string, string, NodeProxy<NodeDefinition>> nodeProxyCreator, RunOptions defaultRunOptions = RunOptions.None)
        {
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);
            Covenant.Requires<ArgumentNullException>(nodeProxyCreator != null);

            this.Definition = clusterDefinition;
            this.Secrets    = new ClusterSecrets();

            Definition.Validate();

            var nodes = new List<NodeProxy<NodeDefinition>>();

            foreach (var nodeDefinition in Definition.SortedNodes)
            {
                var node = nodeProxyCreator(nodeDefinition.DnsName, nodeDefinition.Name);

                node.DefaultRunOptions = defaultRunOptions;
                node.Metadata          = nodeDefinition;
                node.Metadata.Address  = node.ResolveAddress();
                nodes.Add(node);
            }

            this.Nodes        = nodes;
            this.Manager      = Nodes.Where(n => n.Metadata.Manager).OrderBy(n => n.Name.ToLowerInvariant()).First();
            this.DockerSecret = new DockerSecretsManager(this);
            this.Certificate  = new CertiticateManager(this);
            this.PublicProxy  = new ProxyManager(this, "public");
            this.PrivateProxy = new ProxyManager(this, "private");
        }

        /// <summary>
        /// Releases all resources associated with the instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all associated resources.
        /// </summary>
        /// <param name="disposing">Pass <c>true</c> if we're disposing, <c>false</c> if we're finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            lock (syncRoot)
            {
                if (vaultClient != null)
                {
                    vaultClient.Dispose();
                    vaultClient = null;
                }

                if (consulClient != null)
                {
                    consulClient.Dispose();
                    consulClient = null;
                }
            }
        }

        /// <summary>
        /// Returns the cluster secrets.
        /// </summary>
        public ClusterSecrets Secrets { get; private set; }

        /// <summary>
        /// Returns the cluster definition.
        /// </summary>
        public ClusterDefinition Definition { get; private set; }

        /// <summary>
        /// Returns the read-only list of cluster node proxies.
        /// </summary>
        public IReadOnlyList<NodeProxy<NodeDefinition>> Nodes { get; private set; }

        /// <summary>
        /// Returns a manager node that can be used for swarm configuration.
        /// </summary>
        public NodeProxy<NodeDefinition> Manager { get; private set; }

        /// <summary>
        /// Returns the object to be used to manage cluster Docker secrets.
        /// </summary>
        public DockerSecretsManager DockerSecret { get; private set; }

        /// <summary>
        /// Returns the object to be used to manage cluster TLS certificates.
        /// </summary>
        public CertiticateManager Certificate { get; private set; }

        /// <summary>
        /// Manages the cluster's public proxy.
        /// </summary>
        public ProxyManager PublicProxy { get; private set; }

        /// <summary>
        /// Manages the cluster's private proxy.
        /// </summary>
        public ProxyManager PrivateProxy { get; private set; }

        /// <summary>
        /// Enumerates the cluster manager proxies.
        /// </summary>
        public IEnumerable<NodeProxy<NodeDefinition>> Managers
        {
            get { return Nodes.Where(n => n.Metadata.Manager).OrderBy(n => n.Name.ToLowerInvariant()); }
        }

        /// <summary>
        /// Enumerates the cluster worker proxies.
        /// </summary>
        public IEnumerable<NodeProxy<NodeDefinition>> Workers
        {
            get { return Nodes.Where(n => !n.Metadata.Manager).OrderBy(n => n.Name.ToLowerInvariant()); }
        }

        /// <summary>
        /// Returns the <see cref="NodeProxy{TMetadata}"/> instance for a named node.
        /// </summary>
        /// <param name="nodeName">The node name.</param>
        /// <returns>The node proxy instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the name node is not present in the cluster.</exception>
        public NodeProxy<NodeDefinition> GetNode(string nodeName)
        {
            var node = Nodes.SingleOrDefault(n => string.Compare(n.Name, nodeName, StringComparison.OrdinalIgnoreCase) == 0);

            if (node == null)
            {
                throw new KeyNotFoundException($"The node [{nodeName}] is not present in the cluster.");
            }

            return node;
        }

        /// <summary>
        /// Performs cluster configuration steps.
        /// </summary>
        /// <param name="steps">The configuration steps.</param>
        public void Configure(ConfigStepList steps)
        {
            Covenant.Requires<ArgumentNullException>(steps != null);

            foreach (var step in steps)
            {
                step.Run(this);
            }
        }

        /// <summary>
        /// Returns steps that upload a text file to a set of node proxies.
        /// </summary>
        /// <param name="nodes">The node proxies to receive the upload.</param>
        /// <param name="path">The target path on the Linux node.</param>
        /// <param name="text">The input text.</param>
        /// <param name="tabStop">Optionally expands TABs into spaces when non-zero.</param>
        /// <param name="outputEncoding">Optionally specifies the output text encoding (defaults to UTF-8).</param>
        /// <returns>The steps.</returns>
        public IEnumerable<ConfigStep> GetFileUploadSteps(IEnumerable<NodeProxy<NodeDefinition>> nodes, string path, string text, int tabStop = 0, Encoding outputEncoding = null)
        {
            var steps = new ConfigStepList();

            foreach (var node in nodes)
            {
                steps.Add(UploadStep.Text(node.Name, path, text, tabStop, outputEncoding));
            }

            return steps;
        }

        /// <summary>
        /// Returns a Consul client.
        /// </summary>
        /// <returns>The <see cref="ConsulClient"/>.</returns>
        public ConsulClient Consul
        {
            get
            {
                lock (syncRoot)
                {
                    if (consulClient != null)
                    {
                        return consulClient;
                    }

                    consulClient = NeonClusterHelper.OpenConsul();
                }

                return consulClient;
            }
        }

        /// <summary>
        /// Returns a Vault client using the root token.
        /// </summary>
        /// <returns>The <see cref="VaultClient"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Secrets"/> has not yet been intialized with the Vault root token.</exception>
        public VaultClient Vault
        {
            get
            {
                if (Secrets.VaultCredentials == null || string.IsNullOrEmpty(Secrets.VaultCredentials.RootToken))
                {
                    throw new InvalidOperationException($"[{nameof(ClusterProxy)}.{nameof(Secrets)}] has not yet been intialized with the Vault root token.");
                }

                lock (syncRoot)
                {
                    if (vaultClient != null)
                    {
                        return vaultClient;
                    }

                    vaultClient = VaultClient.OpenWithToken(new Uri(Definition.Vault.GetDirectUri(Manager.Name)), Secrets.VaultCredentials.RootToken);
                }

                return vaultClient;
            }
        }

        /// <summary>
        /// Ensure that we have the Vault token.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the root token is not available.</exception>
        private void VerifyVaultToken()
        {
            if (Secrets.VaultCredentials == null || string.IsNullOrEmpty(Secrets.VaultCredentials.RootToken))
            {
                throw new InvalidOperationException($"[{nameof(ClusterProxy)}.{nameof(Secrets)}] has not yet been intialized with the Vault root token.");
            }
        }

        /// <summary>
        /// Executes a command on a cluster manager node using the root Vault token.
        /// </summary>
        /// <param name="command">The command (including the <b>vault</b>).</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The command response.</returns>
        public CommandResponse VaultCommand(string command, params object[] args)
        {
            VerifyVaultToken();

            var scriptBundle = new CommandBundle(command, args);
            var bundle       = new CommandBundle("./vault-command.sh");

            bundle.AddFile("vault-command.sh",
$@"#!/bin/bash
export VAULT_TOKEN={Secrets.VaultCredentials.RootToken}
{scriptBundle}
",
                isExecutable: true);

            return Manager.SudoCommand(bundle, RunOptions.Classified);
        }

        /// <summary>
        /// Creates a Vault access control policy.
        /// </summary>
        /// <param name="policy">The policy.</param>
        /// <returns>The command response.</returns>
        public CommandResponse CreateVaultPolicy(VaultPolicy policy)
        {
            Covenant.Requires<ArgumentNullException>(policy != null);

            VerifyVaultToken();

            var bundle = new CommandBundle("./create-vault-policy.sh");

            bundle.AddFile("create-vault-policy.sh",
$@"#!/bin/bash
export VAULT_TOKEN={Secrets.VaultCredentials.RootToken}
vault policy-write {policy.Name} policy.hcl
",
                isExecutable: true);

            bundle.AddFile("policy.hcl", policy);

            return Manager.SudoCommand(bundle);
        }

        /// <summary>
        /// Removes a Vault access control policy.
        /// </summary>
        /// <param name="policyName">The policy name.</param>
        /// <returns>The command response.</returns>
        public CommandResponse RemoveVaultPolicy(string policyName)
        {
            Covenant.Requires<ArgumentException>(ClusterDefinition.IsValidName(policyName));

            return VaultCommand($"vault policy-delete {policyName}");
        }

        /// <summary>
        /// Creates a Vault AppRole.
        /// </summary>
        /// <param name="roleName">The role name.</param>
        /// <param name="policies">The policy names or HCL details.</param>
        /// <returns>The command response.</returns>
        public CommandResponse CreateVaultAppRole(string roleName, params string[] policies)
        {
            Covenant.Requires<ArgumentNullException>(roleName != null);
            Covenant.Requires<ArgumentNullException>(policies != null);

            var sbPolicies = new StringBuilder();

            if (sbPolicies != null)
            {
                foreach (var policy in policies)
                {
                    if (string.IsNullOrEmpty(policy))
                    {
                        throw new ArgumentNullException("Null or empty policy.");
                    }

                    sbPolicies.AppendWithSeparator(policy, ",");
                }
            }

            // Note that we have to escape any embedded double quotes in the policies
            // because they may include HCL rather than being just policy names.

            return VaultCommand($"vault write auth/approle/role/{roleName} \"policies={sbPolicies.Replace("\"", "\"\"")}\"");
        }

        /// <summary>
        /// Removes a Vault AppRole.
        /// </summary>
        /// <param name="roleName">The role name.</param>
        /// <returns>The command response.</returns>
        public CommandResponse RemoveVaultAppRole(string roleName)
        {
            Covenant.Requires<ArgumentException>(ClusterDefinition.IsValidName(roleName));

            return VaultCommand($"vault delete auth/approle/role/{roleName}");
        }
    }
}
