//-----------------------------------------------------------------------------
// FILE:	    SetupCommand.cs
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

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Cluster;
using Neon.Stack.Common;
using Neon.Stack.Cryptography;
using Neon.Stack.IO;
using Neon.Stack.Net;
using Neon.Stack.Time;

namespace NeonCluster
{
    /// <summary>
    /// Implements the <b>setup</b> command.
    /// </summary>
    public class SetupCommand : ICommand
    {
        private const string usage = @"
Configures a NeonCluster as described in the cluster definition file.

USAGE: 

    neon setup [OPTIONS] [CLUSTER-DEF]

ARGUMENTS:

    CLUSTER-DEF     - Path to the cluster definition file.  This is not 
                      required when you're logged in.

OPTIONS:

    --no-prep       - Indicates that the node has already been prepared
                      with the [neon prep-node] so it won't be
                      prepared again.

NOTE: Preparing a node takes quite some time and includes substantial
      Internet downloads.

      For development and production environments, we recommend that
      you prepare a single bootable image to be used for all nodes
      and then instantiate base nodes using virtualization, PXE boot,
      etc.  Then use the [--no-prep] option when setting up your
      NeonCluster.  A prepared Ubuntu-16.04 VHDX can be obtained
      from here:

      https://s3.amazonaws.com/neon-research/images/ubuntu-16.04-prep.vhdx
";
        private ClusterProxy        cluster;
        private string              managerNodeNames     = string.Empty;
        private string              managerNodeAddresses = string.Empty;
        private int                 managerCount         = 0;
        private string              swarmManagerToken;
        private string              swarmWorkerToken;
        private TlsCertificate      vaultCertificate;
        private VaultCredentials    vaultCredentials;
        private string              rawVaultCredentials;
        private bool                sshTlsAuth;
        private SshClientKey        sshClientKey;
        private string              strongPassword;

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "setup" }; }
        }

        /// <inheritdoc/>
        public string[] ExtendedOptions
        {
            get { return new string[] { "--no-prep" }; }
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
            if (Program.ClusterSecrets != null)
            {
                Console.Error.WriteLine("*** ERROR: You are logged into a cluster.  Logoff before setting up another.");
                Program.Exit(1);
            }

            if (commandLine.Arguments.Length < 1)
            {
                Console.Error.WriteLine("*** ERROR: CLUSTER-DEF is required.");
                Program.Exit(1);
            }

            cluster = new ClusterProxy(ClusterDefinition.FromFile(commandLine.Arguments[0]), Program.CreateNodeProxy<NodeDefinition>, RunOptions.LogOutput | RunOptions.FaultOnError);

            if (File.Exists(Program.GetClusterSecretsPath(cluster.Definition.Name)))
            {
                Console.Error.WriteLine($"*** ERROR: A cluster named [{cluster.Definition.Name}] already exists.");
                Console.Error.WriteLine($"***        Remove it before setting ip up again.");
                Program.Exit(1);
            }

            // Ensure that the nodes have unique IP addresses.

            var ipAddressToServer = new Dictionary<IPAddress, NodeProxy<NodeDefinition>>();

            foreach (var node in cluster.Nodes.OrderBy(n => n.Name))
            {
                NodeProxy<NodeDefinition> duplicateServer;

                if (ipAddressToServer.TryGetValue(node.Metadata.Address, out duplicateServer))
                {
                    throw new ArgumentException($"Nodes [{duplicateServer.Name}] and [{node.Name}] have the same IP address [{node.Metadata.Address}].");
                }

                ipAddressToServer.Add(node.Metadata.Address, node);
            }

            // Generate a string with the IP addresses of the management nodes separated
            // by spaces.  We'll need this when we initialize the management nodes.
            //
            // We're also going to select the management address that we'll use to for
            // joining regular nodes to the cluster.  We'll use the first management
            // node when sorting in ascending order by name for this.

            foreach (var managerNodeDefinition in cluster.Definition.SortedManagers)
            {
                managerCount++;

                if (managerNodeNames.Length > 0)
                {
                    managerNodeNames     += " ";
                    managerNodeAddresses += " ";
                }

                managerNodeNames     += managerNodeDefinition.Name;
                managerNodeAddresses += managerNodeDefinition.Address.ToString();
            }

            // Perform the setup operations.

            var controller = new SetupController(Program.SafeCommandLine, cluster.Nodes);

            controller.AddWaitUntilOnlineStep("connect");

            if (!commandLine.HasOption("--no-prep"))
            {
                controller.AddStep("preparing", server => CommonSteps.PrepareNode(server, cluster.Definition, shutdown: false));
            }

            switch (cluster.Definition.Host.SshAuth)
            {
                case AuthMethods.Password:

                    sshTlsAuth = false;
                    break;

                case AuthMethods.Tls:

                    sshTlsAuth = true;
                    break;

                default:

                    throw new NotSupportedException($"Unsupported SSH authentication method [{cluster.Definition.Host.SshAuth}].");
            }

            if (sshTlsAuth)
            {
                controller.AddStep("ssh client key", n => GenerateClientSshKey(n), n => n.Metadata.Manager);
            }

            controller.AddStep("verify OS", n => CommonSteps.VerifyOS(n));
            controller.AddStep("verify pristine", n => CommonSteps.VerifyPristine(n));
            controller.AddGlobalStep("create certs", () => CreateCertificates());
            controller.AddStep("common config", n => ConfigureCommon(n));
            controller.AddStep("manager config", n => ConfigureManager(n), n => n.Metadata.Manager);
            controller.AddStep("swarm create", n => CreateSwarm(n), n => n == cluster.Manager);
            controller.AddStep("worker config", n => ConfigureWorker(n), n => n.Metadata.Worker);
            controller.AddStep("swarm join", n => JoinSwarm(n), n => n != cluster.Manager);
            controller.AddStep("networks", n => CreateClusterNetworks(n), n => n == cluster.Manager);
            controller.AddStep("node labels", n => AddNodeLabels(n), n => n == cluster.Manager);

            if (cluster.Definition.Docker.RegistryCache)
            {
                controller.AddGlobalStep("registry cache", () => new RegistryCache(cluster).Configure());
            }

            controller.AddStep("pull images", n => PullImages(n));
            controller.AddGlobalStep("vault proxy", () => VaultProxy());
            controller.AddGlobalStep("vault initialize", () => VaultInitialize());
            controller.AddGlobalStep("cluster connect", () => NeonClusterHelper.ConnectCluster(cluster), quiet: true);
            controller.AddGlobalStep("proxy services", () => new ProxyServices(cluster).Configure());

            if (cluster.Definition.Log.Enabled)
            {
                controller.AddGlobalStep("log services", () => new LogServices(cluster).Configure());
                controller.AddStep("metricbeat", n => DeployMetricbeat(n));
                controller.AddGlobalStep("metricbeat dashboards", () => InstallMetricbeatDashboards(cluster));
            }

            controller.AddDelayStep($"cluster stabilize ({Program.WaitSeconds}s)", TimeSpan.FromSeconds(Program.WaitSeconds), "stabilizing");

            controller.AddStep("check managers", n => ClusterDiagnostics.CheckClusterManager(n, cluster.Definition), n => n.Metadata.Manager);
            controller.AddStep("check workers", n => ClusterDiagnostics.CheckClusterWorker(n, cluster.Definition), n => n.Metadata.Worker);

            if (cluster.Definition.Log.Enabled)
            {
                controller.AddGlobalStep("check logging", () => ClusterDiagnostics.CheckLogServices(cluster));
            }

            // Change the root account's password to something very strong.  This
            // step should be very close to the last one so it will still be
            // possible to log into nodes with the old password to diagnose
            // setup issues.

            if (cluster.Definition.Host.PasswordLength > 0)
            {
                strongPassword = NeonHelper.GetRandomPassword(cluster.Definition.Host.PasswordLength);

                controller.AddStep("strong password", n => SetStrongPassword(n));
            }
            else
            {
                strongPassword = Program.Password;
            }

            // This needs to be run last because it will likely disable
            // SSH username/password authentication which may block
            // connection attempts.
            //
            // It's also good to do this last so it'll be possible to 
            // manually login node with the original credentials to 
            // diagnose setup issues.

            controller.AddStep("ssh secured", n => ConfigureSsh(n));

            // Start setup.

            if (!controller.Run())
            {
                Console.Error.WriteLine("*** ERROR: One or more configuration steps failed.");
                Program.Exit(1);
            }

            if (vaultCredentials != null && !string.IsNullOrEmpty(rawVaultCredentials))
            {
                Console.WriteLine("IMPORTANT: HashiCorp Vault Information");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(rawVaultCredentials);
                Console.WriteLine();
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine();
            }

            // Write the cluster admin file.

            var clusterInfoPath = Program.GetClusterSecretsPath(cluster.Definition.Name);
            var clusterSecrets = new ClusterSecrets()
            {
                Definition       = cluster.Definition,
                RootAccount      = Program.UserName,
                RootPassword     = strongPassword,
                VaultCredentials = vaultCredentials,
                VaultCertificate = vaultCertificate.Clone()
            };

            if (sshTlsAuth)
            {
                clusterSecrets.SshClientKey = sshClientKey;
            }

            File.WriteAllText(clusterInfoPath, NeonHelper.JsonSerialize(clusterSecrets, Formatting.Indented));

            var title = $"File: {clusterInfoPath}";

            Console.WriteLine(title);
            Console.WriteLine(new string('-', title.Length));
            Console.WriteLine("Sensitive cluster administration info has been saved to to the file above.");
            Console.WriteLine("You will need this file to perform any further cluster administration.  Take");
            Console.WriteLine("care to back this up to a secure location.");
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine($"*** Logging into [{cluster.Definition.Name}].");

            File.WriteAllText(Program.CurrentClusterPath, cluster.Definition.Name);
            Console.WriteLine();
        }

        /// <summary>
        /// Configures the global environment variables that describe the configuration 
        /// of the server within the cluster.
        /// </summary>
        /// <param name="node">The server to be updated.</param>
        private void ConfigureEnvironmentVariables(NodeProxy<NodeDefinition> node)
        {
            node.Status = "setup: environment...";

            // We're going to append the new variables to the existing Linux [/etc/environment] file.

            var sb = new StringBuilder();

            // Append all of the existing environment variables except for those
            // whose names start with "NEON_" to make the operation idempotent.
            //
            // Note that we're going to special case PATH to add any Neon
            // related directories.

            using (var currentEnvironmentStream = new MemoryStream())
            {
                node.Download("/etc/environment", currentEnvironmentStream);

                currentEnvironmentStream.Position = 0;

                using (var reader = new StreamReader(currentEnvironmentStream))
                {
                    foreach (var line in reader.Lines())
                    {
                        if (line.StartsWith("PATH="))
                        {
                            if (!line.Contains(NodeHostFolders.Tools))
                            {
                                sb.AppendLine(line + $":{NodeHostFolders.Tools}");
                            }
                            else
                            {
                                sb.AppendLine(line);
                            }
                        }
                        else if (!line.StartsWith("NEON_"))
                        {
                            sb.AppendLine(line);
                        }
                    }
                }
            }

            // Add the global Neon host and cluster related environment variables. 

            sb.AppendLine($"NEON_CLUSTER={cluster.Definition.Name}");
            sb.AppendLine($"NEON_DATACENTER={cluster.Definition.Datacenter.ToLowerInvariant()}");
            sb.AppendLine($"NEON_ENVIRONMENT={cluster.Definition.Environment.ToString().ToLowerInvariant()}");
            sb.AppendLine($"NEON_NODE_NAME={node.Name}");
            sb.AppendLine($"NEON_NODE_ROLE={node.Metadata.Role}");
            sb.AppendLine($"NEON_NODE_DNSNAME={node.DnsName.ToLowerInvariant()}");
            sb.AppendLine($"NEON_NODE_IP={node.Metadata.Address}");
            sb.AppendLine($"NEON_NODE_SSD={node.Metadata.Labels.StorageSSD.ToString().ToLowerInvariant()}");
            sb.AppendLine($"NEON_NODE_SWAPPING={node.Metadata.Swapping.ToString().ToLowerInvariant()}");
            sb.AppendLine($"NEON_APT_CACHE={cluster.Definition.PackageCache ?? string.Empty}");

            // Append Consul and Vault addresses.

            // All nodes will be configured such that host processes using the HashiCorp Consul 
            // CLI will access the Consul cluster via local Consul instance.  This will be a 
            // server for manager nodes and a proxy for workers.

            sb.AppendLine($"CONSUL_HTTP_ADDR=" + $"{NeonHosts.Consul}:{cluster.Definition.Consul.Port}");
            sb.AppendLine($"CONSUL_HTTP_FULLADDR=" + $"http://{NeonHosts.Consul}:{cluster.Definition.Consul.Port}");

            // All nodes will be configured such that host processes using the HashiCorp Vault 
            // CLI will access the Vault cluster via the [neon-proxy-vault] proxy service
            // by default.

            sb.AppendLine($"VAULT_ADDR={cluster.Definition.Vault.Uri}");

            if (node.Metadata.Manager)
            {
                // Manager hosts may use the [VAULT_DIRECT_ADDR] environment variable to 
                // access Vault without going through the [neon-proxy-vault] proxy.  This
                // points to the Vault instance running locally.
                //
                // This is useful when configuring Vault.

                sb.AppendLine($"VAULT_DIRECT_ADDR={cluster.Definition.Vault.GetDirectUri(node.Name)}");
            }
            else
            {
                sb.AppendLine($"VAULT_DIRECT_ADDR=");
            }

            // Upload the new environment to the server.

            node.UploadText("/etc/environment", sb.ToString(), tabStop: 4);
        }

        /// <summary>
        /// Reboots the server and waits for it to restart.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        private void Reboot(NodeProxy<NodeDefinition> node)
        {
            node.Status = "rebooting...";
            node.Reboot(wait: true);
        }

        /// <summary>
        /// Generates the cluster certificates.
        /// </summary>
        private void CreateCertificates()
        {
            const int bitCount  = 2048;
            const int validDays = 365000;

            vaultCertificate = TlsCertificate.CreateSelfSigned(NeonHosts.Vault, bitCount, validDays);

            // $todo(jeff.lill): Generate the Consul cert.
        }

        /// <summary>
        /// Performs common node configuration.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        private void ConfigureCommon(NodeProxy<NodeDefinition> node)
        {
            ConfigureEnvironmentVariables(node);

            // Upload the setup and configuration files.

            node.InitializeNeonFolders();
            node.UploadConfigFiles(cluster.Definition);
            node.UploadTools(cluster.Definition);

            // Configure the APT proxy server early, if there is one.

            if (!string.IsNullOrEmpty(cluster.Definition.PackageCache))
            {
                node.Status = "run: setup-apt-proxy.sh";
                node.SudoCommand("setup-apt-proxy.sh");
            }

            // Perform basic node setup including changing the host name.

            node.Status = "run: setup-apt-ready.sh";
            node.SudoCommand("setup-apt-ready.sh");

            UploadHostsFile(node);
            UploadHostEnvFile(node);

            node.Status = "run: setup-node.sh";
            node.SudoCommand("setup-node.sh");

            // Create and configure the internal cluster self-signed certificates.

            node.Status = "install certs";

            // Install the Vault certificate.

            node.UploadText($"/usr/local/share/ca-certificates/{NeonHosts.Vault}.crt", vaultCertificate.Cert);
            node.SudoCommand("mkdir -p /etc/vault");
            node.UploadText($"/etc/vault/vault.crt", vaultCertificate.Cert);
            node.UploadText($"/etc/vault/vault.key", vaultCertificate.Key);
            node.SudoCommand("chmod 600 /etc/vault/*");

            // $todo(jeff.lill): Install the Consul certificate.

            node.SudoCommand("update-ca-certificates");

            // Make sure we have the latest packages.

            node.Status = "update packages";
            node.SudoCommand("apt-get update -yq");
            node.SudoCommand("apt-get dist-upgrade -yq");

            // Reboot to pick up the host name change and the new global
            // environment variables.

            node.Status = "rebooting...";
            node.Reboot(wait: true);

            node.Status = "run: setup-apt-ready.sh";
            node.SudoCommand("setup-apt-ready.sh");

            // Perform other common configuration.

            node.Status = "run: setup-ssd.sh";
            node.SudoCommand("setup-ssd.sh");

            node.Status = "run: setup-dotnet.sh";
            node.SudoCommand("setup-dotnet.sh");
        }

        /// <summary>
        /// Returns the IP address for a node suitable for including in the
        /// <b>/etc/hosts</b> file.  
        /// </summary>
        /// <param name="node">The cluster node.</param>
        /// <returns>
        /// The IP address, left adjusted with necessary spaces so that the
        /// host definitions will align nicely.
        /// </returns>
        private string GetHostsFormattedAddress(NodeProxy<NodeDefinition> node)
        {
            const string ip4Max = "255.255.255.255";

            var address = node.Metadata.Address.ToString();

            if (address.Length < ip4Max.Length)
            {
                address += new string(' ', ip4Max.Length - address.Length);
            }

            return address;
        }

        /// <summary>
        /// Generates the custom portion of the <b>/etc/hosts</b> file to be configured
        /// for a cluster node or in a container via the <b>/etc/neoncluster/env-host</b>
        /// script.
        /// </summary>
        /// <param name="node">The target node.</param>
        /// <returns>The host definitions.</returns>
        private string GetClusterHostMappings(NodeProxy<NodeDefinition> node)
        {
            var sbHosts = new StringBuilder();

            sbHosts.AppendLine();
            sbHosts.AppendLine("# Internal cluster node mappings:");
            sbHosts.AppendLine();

            foreach (var clusterNode in cluster.Nodes)
            {
                sbHosts.AppendLine($"{GetHostsFormattedAddress(clusterNode)} {clusterNode.Name}.{NeonHosts.ClusterNode}");
            }

            sbHosts.AppendLine();
            sbHosts.AppendLine("# Internal cluster Consul mappings:");
            sbHosts.AppendLine();

            sbHosts.AppendLine($"{GetHostsFormattedAddress(node)} {NeonHosts.Consul}");

            sbHosts.AppendLine();
            sbHosts.AppendLine("# Internal cluster Vault mappings:");
            sbHosts.AppendLine();
            sbHosts.AppendLine($"{GetHostsFormattedAddress(node)} {NeonHosts.Vault}");

            foreach (var manager in cluster.Managers)
            {
                sbHosts.AppendLine($"{GetHostsFormattedAddress(manager)} {manager.Name}.{NeonHosts.Vault}");
            }

            if (cluster.Definition.Docker.RegistryCache)
            {
                sbHosts.AppendLine();
                sbHosts.AppendLine("# Internal cluster registry cache related mappings:");
                sbHosts.AppendLine();

                foreach (var manager in cluster.Managers)
                {
                    sbHosts.AppendLine($"{GetHostsFormattedAddress(manager)} {manager.Name}.{NeonHosts.RegistryCache}");
                }
            }

            if (cluster.Definition.Log.Enabled)
            {
                sbHosts.AppendLine();
                sbHosts.AppendLine("# Internal cluster log pipeline related mappings:");
                sbHosts.AppendLine();

                sbHosts.AppendLine($"{GetHostsFormattedAddress(node)} {NeonHosts.LogEsData}");
            }

            return sbHosts.ToString();
        }

        /// <summary>
        /// Generates and uploads the <b>/etc/hosts</b> file for a node.
        /// </summary>
        /// <param name="node">The target node.</param>
        private void UploadHostsFile(NodeProxy<NodeDefinition> node)
        {
            var sbHosts = new StringBuilder();

            sbHosts.Append(
$@"
127.0.0.1	    localhost
127.0.1.1	    {node.Name}

# The following lines are desirable for IPv6 capable hosts:

::1             localhost ip6-localhost ip6-loopback
ff02::1         ip6-allnodes
ff02::2         ip6-allrouters
");
            sbHosts.Append(GetClusterHostMappings(node));

            node.UploadText("/etc/hosts", sbHosts.ToString(), 4, Encoding.UTF8);
        }

        /// <summary>
        /// Generates and uploads the <b>/etc/neoncluster/env-host</b> file for a node.
        /// </summary>
        /// <param name="node">The target node.</param>
        private void UploadHostEnvFile(NodeProxy<NodeDefinition> node)
        {
            var sbEnvHost = new StringBuilder();

            sbEnvHost.AppendLine(
$@"#------------------------------------------------------------------------------
# FILE:         /etc/neoncluster/env-host
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# This script can be mounted into containers that required extended knowledge
# about the cluster and host node.  This will be mounted to [/etc/neoncluster/env-host]
# such that the container entrypoint script can execute it.

# Define the cluster and Docker host related environment variables.

export NEON_CLUSTER={cluster.Definition.Name}
export NEON_DATACENTER={cluster.Definition.Datacenter}
export NEON_ENVIRONMENT={cluster.Definition.Environment}
export NEON_NODE_NAME={node.Name}
export NEON_NODE_ROLE={node.Metadata.Role}
export NEON_NODE_DNSNAME={node.Metadata.DnsName}
export NEON_NODE_IP={node.Metadata.Address}
export NEON_NODE_SSD={node.Metadata.Labels.StorageSSD.ToString().ToLowerInvariant()}
export NEON_APT_CACHE={cluster.Definition.PackageCache ?? string.Empty}

export VAULT_ADDR={cluster.Definition.Vault.Uri}
export CONSUL_HTTP_ADDR={NeonHosts.Consul}:{cluster.Definition.Consul.Port}
export CONSUL_HTTP_FULLADDR=http://{NeonHosts.Consul}:{cluster.Definition.Consul.Port}

# Append internal cluster DNS mappings to the container's [/etc/hosts] file.

");
            sbEnvHost.AppendLine($"cat <<EOF >> /etc/hosts");
            sbEnvHost.AppendLine(GetClusterHostMappings(node));
            sbEnvHost.AppendLine("EOF");

            node.UploadText($"{NodeHostFolders.Config}/env-host", sbEnvHost.ToString(), 4, Encoding.UTF8);
        }

        /// <summary>
        /// Generates the Consul configuration file for a cluster node.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        /// <returns>The configuration file text.</returns>
        private string GetConsulConfig(NodeProxy<NodeDefinition> node)
        {
            var consulTlsDisabled = true;   // $todo(jeff.lill): Remove this once we support Consul TLS.

            var consulConf = new JObject();

            consulConf.Add("log_level", "info");
            consulConf.Add("datacenter", cluster.Definition.Datacenter);
            consulConf.Add("node_name", node.Name);
            consulConf.Add("data_dir", "/var/consul");
            consulConf.Add("advertise_addr", node.Metadata.Address.ToString());
            consulConf.Add("client_addr", "0.0.0.0");

            var ports = new JObject();

            ports.Add("http", consulTlsDisabled ? 8500 : -1);
            ports.Add("https", consulTlsDisabled ? -1 : 8500);
            ports.Add("dns", 53);

            consulConf.Add("ports", ports);

            if (!consulTlsDisabled)
            {
                consulConf.Add("cert_file", "/etc/consul.d/consul.crt");
                consulConf.Add("key_file", "/etc/consul.d/consul.key");
            }

            consulConf.Add("ui", true);
            consulConf.Add("leave_on_terminate", false);
            consulConf.Add("skip_leave_on_interrupt", true);
            consulConf.Add("disable_remote_exec", true);
            consulConf.Add("domain", "cluster");

            var recursors = new JArray();

            foreach (var nameserver in cluster.Definition.Network.Nameservers)
            {
                recursors.Add(nameserver);
            }

            consulConf.Add("recursors", recursors);

            if (node.Metadata.Manager)
            {
                consulConf.Add("bootstrap_expect", cluster.Definition.Managers.Count());

                var performance = new JObject();

                performance.Add("raft_multiplier", 1);

                consulConf.Add("performance", performance);
            }
            else
            {
                var managerAddresses = new JArray();

                foreach (var manager in cluster.Managers)
                {
                    managerAddresses.Add(manager.Metadata.Address.ToString());
                }

                consulConf.Add("retry_join", managerAddresses);
                consulConf.Add("retry_interval", "30s");
            }

            return consulConf.ToString(Formatting.Indented);
        }

        /// <summary>
        /// Complete a manager node configuration.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        private void ConfigureManager(NodeProxy<NodeDefinition> node)
        {
            // Setup NTP.

            node.Status = "run: setup-ntp.sh";
            node.SudoCommand("setup-ntp.sh");

            // Setup the Consul server and join it to the cluster.

            node.Status = "upload: consul.json";
            node.SudoCommand("mkdir -p /etc/consul.d");
            node.SudoCommand("chmod 770 /etc/consul.d");
            node.UploadText("/etc/consul.d/consul.json", GetConsulConfig(node));

            node.Status = "run: setup-consul-server.sh";
            node.SudoCommand("setup-consul-server.sh", cluster.Definition.Consul.EncryptionKey);

            // Bootstrap Consul cluster discovery.

            var discoveryTimer = new PolledTimer(TimeSpan.FromMinutes(2));

            node.Status = "consul cluster bootstrap";

            while (true)
            {
                if (node.SudoCommand($"consul join {managerNodeAddresses}", RunOptions.None).ExitCode == 0)
                {
                    break;
                }

                if (discoveryTimer.HasFired)
                {
                    node.Fault($"Unable to form Consul cluster within [{discoveryTimer.Interval}].");
                    break;
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }

            // Install Vault server.

            node.Status = "run: setup-vault-server.sh";
            node.SudoCommand("setup-vault-server.sh");

            // Setup Docker

            node.Status = "run: setup-docker.sh";
            node.SudoCommand("setup-docker.sh");

            if (!string.IsNullOrEmpty(cluster.Definition.Docker.RegistryUserName))
            {
                // We need to log into the registry and/or cache.

                node.Status = "docker login";

                var loginCommand = new CommandBundle("./docker-login.sh");

                loginCommand.AddFile("docker-login.sh",
$@"docker login \
    -u ""{cluster.Definition.Docker.RegistryUserName}"" \
    -p ""{cluster.Definition.Docker.RegistryPassword}"" \
    {cluster.Definition.Docker.Registry}", isExecutable: true);

                node.SudoCommand(loginCommand);
            }

            // Cleanup any cached APT files.

            node.Status = "cleanup";
            node.SudoCommand("apt-get clean -yq");
            node.SudoCommand("rm -rf /var/lib/apt/lists");
        }

        /// <summary>
        /// Creates the initial swarm on the bootstrap manager node passed and 
        /// captures the manager and worker swarm tokens required to join additional
        /// nodes to the cluster.
        /// </summary>
        /// <param name="bootstrapManager">The target bootstrap manager server.</param>
        private void CreateSwarm(NodeProxy<NodeDefinition> bootstrapManager)
        {
            bootstrapManager.Status = "create swarm";

            var response = bootstrapManager.DockerCommand($"docker swarm init --advertise-addr {bootstrapManager.Metadata.Address}:{cluster.Definition.Docker.SwarmPort}");

            swarmWorkerToken = ExtractSwarmToken(response.OutputText);

            response = bootstrapManager.DockerCommand($"docker swarm join-token manager");

            swarmManagerToken = ExtractSwarmToken(response.OutputText);
        }

        /// <summary>
        /// Extracts the Swarm token from a <b>docker swarm init...</b> or <b>docker swarm join-token manager</b>
        /// commands.  This token will be used when adding additional nodes to the cluster.
        /// </summary>
        /// <param name="commandResponse">The command response string.</param>
        /// <returns>The swarm token.</returns>
        private string ExtractSwarmToken(string commandResponse)
        {
            const string tokenOpt = "--token";

            int startPos = commandResponse.IndexOf(tokenOpt);

            if (startPos == -1)
            {
                throw new NeonClusterException("Cannot extract swarm token.");
            }

            startPos += tokenOpt.Length;

            int endPos = commandResponse.IndexOf("\\", startPos);

            if (startPos == -1)
            {
                throw new NeonClusterException("Cannot extract swarm token.");
            }

            return commandResponse.Substring(startPos, endPos - startPos).Trim();
        }

        /// <summary>
        /// Configures a worker node.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        private void ConfigureWorker(NodeProxy<NodeDefinition> node)
        {
            // Configure the worker.

            node.Status = "run: setup-ntp.sh";
            node.SudoCommand("setup-ntp.sh");

            // Setup the Consul proxy and join it to the cluster.

            node.Status = "upload: consul.json";
            node.SudoCommand("mkdir -p /etc/consul.d");
            node.SudoCommand("chmod 770 /etc/consul.d");
            node.UploadText("/etc/consul.d/consul.json", GetConsulConfig(node));

            node.Status = "run: setup-consul-proxy.sh";
            node.SudoCommand("setup-consul-proxy.sh", cluster.Definition.Consul.EncryptionKey);

            // Join this node's Consul agent with the master(s).

            var discoveryTimer = new PolledTimer(TimeSpan.FromMinutes(2));

            node.Status = "join consul cluster";

            while (true)
            {
                if (node.SudoCommand($"consul join {managerNodeAddresses}", RunOptions.None).ExitCode == 0)
                {
                    break;
                }

                if (discoveryTimer.HasFired)
                {
                    node.Fault($"Unable to join Consul cluster for [{discoveryTimer.Interval}].");
                    break;
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }

            // Setup Docker.

            node.Status = "run: setup-docker.sh";
            node.SudoCommand("setup-docker.sh");

            if (!string.IsNullOrEmpty(cluster.Definition.Docker.RegistryUserName))
            {
                // We need to log into the registry and/or cache.

                node.Status = "docker login";

                var loginCommand = new CommandBundle("./docker-login.sh");

                loginCommand.AddFile("docker-login.sh", 
$@"docker login \
    -u ""{cluster.Definition.Docker.RegistryUserName}"" \
    -p ""{cluster.Definition.Docker.RegistryPassword}"" \
    {cluster.Definition.Docker.Registry}", isExecutable: true);

                node.SudoCommand(loginCommand);
            }

            // Configure Vault client.

            node.Status = "run: setup-vault-client.sh";
            node.SudoCommand("setup-vault-client.sh");

            // Cleanup any cached APT files.

            node.Status = "cleanup";
            node.SudoCommand("apt-get clean -yq");
            node.SudoCommand("rm -rf /var/lib/apt/lists");
        }

        /// <summary>
        /// Creates the standard cluster overlay networks.
        /// </summary>
        /// <param name="manager">The manager node.</param>
        private void CreateClusterNetworks(NodeProxy<NodeDefinition> manager)
        {
            manager.DockerCommand(
                "docker network create",
                    "--driver", "overlay",
                    "--opt", "encrypt",
                    "--subnet", cluster.Definition.Network.PublicSubnet,
                    cluster.Definition.Network.PublicAttachable ? "--attachable" : null,
                    NeonClusterConst.ClusterPublicNetwork);

            manager.DockerCommand(
                "docker network create",
                    "--driver", "overlay",
                    "--opt", "encrypt",
                    "--subnet", cluster.Definition.Network.PrivateSubnet,
                    cluster.Definition.Network.PrivateAttachable ? "--attachable" : null,
                    NeonClusterConst.ClusterPrivateNetwork);
        }

        /// <summary>
        /// Adds the node labels.
        /// </summary>
        /// <param name="manager">The manager node.</param>
        private void AddNodeLabels(NodeProxy<NodeDefinition> manager)
        {
            foreach (var node in cluster.Nodes)
            {
                node.Status = "labeling";

                var labelDefinitions = new List<string>();

                labelDefinitions.Add($"{NodeLabels.LabelDatacenter}={cluster.Definition.Datacenter}");
                labelDefinitions.Add($"{NodeLabels.LabelEnvironment}={cluster.Definition.Environment}");

                foreach (var item in node.Metadata.Labels.Standard)
                {
                    labelDefinitions.Add($"{item.Key.ToLowerInvariant()}={item.Value}");
                }

                foreach (var item in node.Metadata.Labels.Custom)
                {
                    labelDefinitions.Add($"{item.Key.ToLowerInvariant()}={item.Value}");
                }

                foreach (var labelDefinition in labelDefinitions)
                {
                    manager.DockerCommand("docker node update --label-add", labelDefinition, node.Name);
                }

                node.Status = node == manager ? "labeling" : "done";
            }
        }

        /// <summary>
        /// Pulls common images to the node.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        private void PullImages(NodeProxy<NodeDefinition> node)
        {
            var images = new List<string>()
                {
                    "neoncluster/alpine",
                    "neoncluster/ubuntu-16.04",
                    "neoncluster/neon-proxy-vault"
            };

            if (cluster.Definition.Log.Enabled)
            {
                images.Add(cluster.Definition.Log.HostImage);
                images.Add(cluster.Definition.Log.CollectorImage);
                images.Add(cluster.Definition.Log.EsImage);
                images.Add(cluster.Definition.Log.MetricbeatImage);
            }

            foreach (var image in images)
            {
                var command = $"docker pull {image}";

                node.Status = $"run: {command}";
                node.DockerCommand(command);
            }
        }

        /// <summary>
        /// Adds the node to the swarm cluster.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        private void JoinSwarm(NodeProxy<NodeDefinition> node)
        {
            if (node == cluster.Manager)
            {
                // This one has implictly joined.

                node.Status = "joined";
                return;
            }

            node.Status = "joining";

            if (node.Metadata.Manager)
            {
                node.DockerCommand($"docker swarm join --token {swarmManagerToken} {cluster.Manager.Metadata.Address}:2377");
            }
            else
            {
                // Must be a worker node.

                node.DockerCommand($"docker swarm join --token {swarmWorkerToken} {cluster.Manager.Metadata.Address}:2377");
            }

            node.Status = "joined";
        }

        /// <summary>
        /// Initializes the cluster's HashiCorp Vault.
        /// </summary>
        private void VaultInitialize()
        {
            var firstManager = cluster.Manager;

            try
            {
                // Initialize the Vault cluster using the first manager.

                firstManager.Status = "vault: init";

                var response = firstManager.SudoCommand(
                    "vault-direct init",
                    RunOptions.LogOnErrorOnly | RunOptions.Classified,
                    $"-key-shares={cluster.Definition.Vault.KeyCount}",
                    $"-key-threshold={cluster.Definition.Vault.KeyThreshold}");

                if (response.ExitCode > 0)
                {
                    firstManager.Fault($"[vault init] exit code ={response.ExitCode}");
                    return;
                }

                rawVaultCredentials              = response.OutputText;
                vaultCredentials                 = VaultCredentials.FromInit(rawVaultCredentials, cluster.Definition.Vault.KeyThreshold);
                cluster.Secrets.VaultCredentials = vaultCredentials;

                // Wait for the Vault instance on each manager node to become ready and then 
                // unseal it.

                foreach (var manager in cluster.Managers)
                {
                    // Wait up to two minutes for Vault to initialize.

                    var timer   = new Stopwatch();
                    var timeout = TimeSpan.FromMinutes(5);

                    while (true)
                    {
                        if (timer.Elapsed > timeout)
                        {
                            manager.Fault($"[Vault] did not become ready after [{timeout}].");
                            return;
                        }

                        response = manager.SudoCommand("vault-direct status", RunOptions.LogOutput);

                        if (response.ExitCode == 2 /* sealed */ &&
                            response.OutputText.Contains("High-Availability Enabled: true"))
                        {
                            break;
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }

                    // Unseal the Vault instance.

                    manager.Status = "vault: unseal";

                    manager.SudoCommand($"vault-direct unseal -reset");     // This clears any previous unseal attempts

                    for (int i = 0; i < vaultCredentials.KeyThreshold; i++)
                    {
                        manager.SudoCommand($"vault-direct unseal", RunOptions.Classified, vaultCredentials.UnsealKeys[i]);
                    }
                }

                // Configure the audit backend so that it sends events to syslog.

                firstManager.Status = "vault: audit enable";
                cluster.VaultCommand("vault audit-enable syslog tag=\"vault\" facility=\"AUTH\"");

                // Mount a [generic] backend dedicated to NeonCluster related secrets.

                firstManager.Status = "vault: cluster secrets backend";
                cluster.VaultCommand("vault mount", $"-path=neon-secret", "-description=Reserved for NeonCluster secrets", "generic");

                // Mount the [transit] backend and create the cluster key.

                firstManager.Status = "vault: transit backend";
                cluster.VaultCommand("vault mount transit");
                cluster.VaultCommand($"vault write -f transit/keys/{NeonClusterConst.VaultTransitKey}");

                // Mount the [approle] backend.

                firstManager.Status = "vault: approle backend";
                cluster.VaultCommand("vault auth-enable approle");

                // Initialize the standard policies.

                firstManager.Status = "vault: policies";

                var writeCapabilities = VaultCapabilies.Create | VaultCapabilies.Read | VaultCapabilies.Update | VaultCapabilies.Delete | VaultCapabilies.List;
                var readCapabilities = VaultCapabilies.Read | VaultCapabilies.List;

                cluster.CreateVaultPolicy(new VaultPolicy("neon-reader", "neon-secret/*", readCapabilities));
                cluster.CreateVaultPolicy(new VaultPolicy("neon-writer", "neon-secret/*", writeCapabilities));
                cluster.CreateVaultPolicy(new VaultPolicy("neon-share-reader", "neon-secret/share/*", readCapabilities));
                cluster.CreateVaultPolicy(new VaultPolicy("neon-share-writer", "neon-secret/share/*", writeCapabilities));
                cluster.CreateVaultPolicy(new VaultPolicy("neon-cert-reader", "neon-secret/cert/*", readCapabilities));
                cluster.CreateVaultPolicy(new VaultPolicy("neon-cert-writer", "neon-secret/cert/*", writeCapabilities));
                cluster.CreateVaultPolicy(new VaultPolicy("neon-service-reader", "neon-secret/service/*", readCapabilities));
                cluster.CreateVaultPolicy(new VaultPolicy("neon-service-writer", "neon-secret/service/*", writeCapabilities));

                // Initialize the [neon-proxy-*] related service roles.  Each of these services 
                // need read access to the TLS certificates.

                firstManager.Status = "vault: roles";

                cluster.CreateVaultAppRole("neon-proxy-manager", "neon-cert-reader");
                cluster.CreateVaultAppRole("neon-proxy-public", "neon-cert-reader");
                cluster.CreateVaultAppRole("neon-proxy-private", "neon-cert-reader");
            }
            finally
            {
                cluster.Manager.Status = string.Empty;
            }
        }

        /// <summary>
        /// Configures the Vault load balancer service: <b>neon-proxy-vault</b>.
        /// </summary>
        private void VaultProxy()
        {
            // Create the comma separated list of Vault manager endpoints formatted as:
            //
            //      NODE:IP:PORT

            var sbEndpoints = new StringBuilder();

            foreach (var manager in cluster.Definition.SortedManagers)
            {
                sbEndpoints.AppendWithSeparator($"{manager.Name}:{manager.Address}:{NetworkPorts.Vault}", ",");
            }

            // Deploy [neon-proxy-vault] on all manager nodes.

            var steps   = new ConfigStepList();
            var command = CommandStep.CreateDocker(cluster.Manager.Name,
                "docker service create",
                    "--name", "neon-proxy-vault",
                    "--mode", "global",
                    "--endpoint-mode", "vip",
                    "--network", NeonClusterConst.ClusterPrivateNetwork,
                    "--constraint", $"node.role==manager",
                    "--publish", $"{NeonHostPorts.ProxyVault}:{NetworkPorts.Vault}",
                    "--mount", "type=bind,source=/etc/neoncluster/env-host,destination=/etc/neoncluster/env-host,readonly=true",
                    "--env", $"VAULT_ENDPOINTS={sbEndpoints}",
                    "neoncluster/neon-proxy-vault");

            steps.Add(command);
            steps.Add(cluster.GetFileUploadSteps(cluster.Managers, LinuxPath.Combine(NodeHostFolders.Scripts, "neon-proxy-vault.sh"), command.ToBash()));

            cluster.Configure(steps);
        }

        /// <summary>
        /// Deploys <b>Elastic Metricbeat</b> to the node.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        private void DeployMetricbeat(NodeProxy<NodeDefinition> node)
        {
            node.Status = "deploying metricbeat";

            node.DockerCommand(
                "docker run",
                    "--name", "neon-log-metricbeat",
                    "--detach",
                    "--restart", "always",
                    "--volume", "/etc/neoncluster/env-host:/etc/neoncluster/env-host:ro",
                    "--volume", "/proc:/hostfs/proc:ro",
                    "--volume", "/:/hostfs:ro",
                    "--net", "host",
                    "--log-driver", "json-file",
                    "neoncluster/metricbeat");
        }

        /// <summary>
        /// Installs the <b>Elastic Metricbeat</b> dashboards to the log Elasticsearch cluster.
        /// </summary>
        /// <param name="cluster">The cluster proxy.</param>
        private void InstallMetricbeatDashboards(ClusterProxy cluster)
        {
            // Note that we're going to add the Metricbeat dashboards to Elasticsearch
            // even when the Kibana dashboard isn't enabled because it doesn't cost
            // much and to make it easy for operators that wish to install Kibana
            // themselves.

            cluster.Manager.Status = "metricbeat dashboards";

            cluster.Manager.DockerCommand(
                "docker run --rm",
                    "--name", "neon-log-metricbeat-dash-init",
                    "--volume", "/etc/neoncluster/env-host:/etc/neoncluster/env-host:ro",
                    "neoncluster/metricbeat", "import-dashboards");
        }

        /// <summary>
        /// Generates the SSH key to be used for authenticating SSH client connections.
        /// </summary>
        /// <param name="manager">A cluster manager node.</param>
        private void GenerateClientSshKey(NodeProxy<NodeDefinition> manager)
        {
            // Here's some information explaining what how I'm doing this:
            //
            //      https://help.ubuntu.com/community/SSH/OpenSSH/Configuring
            //      https://help.ubuntu.com/community/SSH/OpenSSH/Keys

            if (!sshTlsAuth)
            {
                return;
            }

            sshClientKey = new SshClientKey();

            // $hack(jeff.lill): 
            //
            // We're going to generate a 2048 bit key pair on one of the
            // manager nodes and then download and then delete it.  This
            // means that the private key will be persisted to disk (tmpfs)
            // for a moment but I'm going to worry about this too much.
            //
            // Technically, I could have installed OpenSSL or something
            // on Windows or figured out the .NET Crypto libraries but
            // but OpenSSL didn't support generating the PUB format
            // SSH expects for the client public key.

            const string keyGenScript =
@"
# Generate a 2048-bit key without a passphrase (the -N """" option).

ssh-keygen -t rsa -b 2048 -N """" -C ""neon-cluster"" -f /run/ssh-key

# Relax permissions so we can download the key parts.

chmod 666 /run/ssh-key*
";
            var bundle = new CommandBundle("./keygen.sh");

            bundle.AddFile("keygen.sh", keyGenScript, isExecutable: true);

            manager.SudoCommand(bundle);

            using (var stream = new MemoryStream())
            {
                manager.Download("/run/ssh-key.pub", stream);

                sshClientKey.PublicPUB = Encoding.UTF8.GetString(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                manager.Download("/run/ssh-key", stream);

                sshClientKey.PrivatePEM = Encoding.UTF8.GetString(stream.ToArray());
            }

            manager.SudoCommand("rm /run/ssh-key*");

            // We're going to use WinSCP to convert the OpenSSH PUB formatted key
            // to the PPK format PuTTY/WinSCP require.

            var pemKeyPath = Path.Combine(Program.ClusterTempFolder, Guid.NewGuid().ToString("D"));
            var ppkKeyPath = Path.Combine(Program.ClusterTempFolder, Guid.NewGuid().ToString("D"));

            try
            {
                File.WriteAllText(pemKeyPath, sshClientKey.PrivatePEM);

                var result = NeonHelper.ExecuteCaptureStreams("winscp.com", $@"/keygen ""{pemKeyPath}"" /comment=""{cluster.Definition.Name} Key"" /output=""{ppkKeyPath}""");

                if (result.ExitCode != 0)
                {
                    Console.WriteLine(result.StandardOutput);
                    Console.Error.WriteLine(result.StandardError);
                    Program.Exit(result.ExitCode);
                }

                sshClientKey.PrivatePPK = File.ReadAllText(ppkKeyPath);
            }
            finally
            {
                File.Delete(pemKeyPath);
                File.Delete(ppkKeyPath);
            }
        }

        /// <summary>
        /// Changes the admin account password on a node.
        /// </summary>
        /// <param name="node">The target node.</param>
        private void SetStrongPassword(NodeProxy<NodeDefinition> node)
        {
            node.Status = "set strong password";

            var script =
$@"
echo '{Program.UserName}:{strongPassword}' | chpasswd
";
            var bundle = new CommandBundle("./set-strong-password.sh");

            bundle.AddFile("set-strong-password.sh", script, isExecutable: true);

            var response = node.SudoCommand(bundle);

            if (response.ExitCode != 0)
            {
                Console.WriteLine($"*** ERROR: Unable to set a strong password [exitcode={response.ExitCode}].");
                Program.Exit(response.ExitCode);
            }
        }

        /// <summary>
        /// Configures SSH on a node.
        /// </summary>
        /// <param name="node">The target node.</param>
        private void ConfigureSsh(NodeProxy<NodeDefinition> node)
        {
            CommandBundle bundle;

            // Here's some information explaining what how I'm doing this:
            //
            //      https://help.ubuntu.com/community/SSH/OpenSSH/Configuring
            //      https://help.ubuntu.com/community/SSH/OpenSSH/Keys

            if (sshTlsAuth)
            {
                node.Status = "set client SSH key";

                // Enable the public key by appending it to [$HOME/.ssh/authorized_keys],
                // creating the file if necessary.  Note that we're allowing only a single
                // authorized key.

                var addKeyScript =
$@"
chmod go-w ~/
mkdir -p $HOME/.ssh
chmod 700 $HOME/.ssh
touch $HOME/.ssh/authorized_keys
cat ssh-key.pub > $HOME/.ssh/authorized_keys
chmod 600 $HOME/.ssh/authorized_keys
";
                bundle = new CommandBundle("./addkeys.sh");

                bundle.AddFile("addkeys.sh", addKeyScript, isExecutable: true);
                bundle.AddFile("ssh-key.pub", sshClientKey.PublicPUB);

                // NOTE: I'm explictly not running the bundle as [sudo] because the OpenSSH
                //       server is very picky about the permissions on the user's [$HOME]
                //       and [$HOME/.ssl] folder and contents.  This took me a couple 
                //       hours to figure out.

                node.RunCommand(bundle);

                if (!cluster.Definition.Host.PasswordAuth)
                {
                    // Use SED to disable password authentication.
                    //
                    // This command DOES need to be run under [sudo].

                    var disablePasswordScript =
@"
sed -i 's!^\#PasswordAuthentication yes$!PasswordAuthentication no!g' /etc/ssh/sshd_config
";
                    bundle = new CommandBundle("./config.sh");

                    bundle.AddFile("config.sh", disablePasswordScript, isExecutable: true);
                    node.SudoCommand(bundle);
                }
            }

            // These steps are required for both password and public key authentication.

            // We're need to generate new SSH server keys because it's likely the machine
            // was created from a cloned image and we don't want all of the cluster
            // machines to have the same key.  We're also going to edit the SSHD config
            // to disable all host keys except for RSA.

            node.Status = "regenerate server SSH key";

            var configScript =
@"
# Regenerate the SSH server keys.

rm -f /etc/ssh/ssh_host_rsa_key
ssh-keygen -f /etc/ssh/ssh_host_rsa_key -N '' -t rsa

rm -f /etc/ssh/ssh_host_dsa_key
ssh-keygen -f /etc/ssh/ssh_host_dsa_key -N '' -t dsa

rm -f /etc/ssh/ssh_host_ecdsa_key
ssh-keygen -f /etc/ssh/ssh_host_ecdsa_key -N '' -t ecdsa -b 521

# Extract the host's SSL RSA key fingerprint to temporary files
# so [neon.exe] can download it.

ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key > /tmp/ssh.fingerprint

# Disable all host keys except for RSA.

sed -i 's!^\HostKey /etc/ssh/ssh_host_dsa_key$!#HostKey /etc/ssh/ssh_host_dsa_key!g' /etc/ssh/sshd_config
sed -i 's!^\HostKey /etc/ssh/ssh_host_ecdsa_key$!#HostKey /etc/ssh/ssh_host_ecdsa_key!g' /etc/ssh/sshd_config
sed -i 's!^\HostKey /etc/ssh/ssh_host_ed25519_key$!#HostKey /etc/ssh/ssh_host_ed25519_key!g' /etc/ssh/sshd_config

# Restart SSHD to pick up the changes.

systemctl restart sshd
";
            bundle = new CommandBundle("./config.sh");
            
            bundle.AddFile("config.sh", configScript, isExecutable: true);
            node.SudoCommand(bundle);

            node.Status = "download server SSH key";

            using (var stream = new MemoryStream())
            {
                node.Download("/tmp/ssh.fingerprint", stream);

                node.Metadata.SshKeyFingerprint = Encoding.UTF8.GetString(stream.ToArray());
            }

            node.SudoCommand("rm -f /tmp/ssh.fingerprint");
        }
    }
}
