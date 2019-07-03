//-----------------------------------------------------------------------------
// FILE:	    ExampleCommand.cs
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

using Newtonsoft;
using Newtonsoft.Json;

using Neon.Cluster;
using Neon.Stack.Common;

namespace NeonCluster
{
    /// <summary>
    /// Implements the <b>example</b> command.
    /// </summary>
    public class ExampleCommand : ICommand
    {
        private const string usage = @"
Writes a sample cluster definition file to the standard output.  You
can use this as a starting point for creating a customized definition.

USAGE:

    neon example
";
        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "example" }; }
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
            const string sampleJson =
@"//-----------------------------------------------------------------------------
// This is a sample cluster definition file.  This can be a good starting point 
// for creating a custom cluster.
//
// The file format is JSON with some preprocessing capabilities.  Scroll down to
// the bottom of the file for more information.

//-----------------------------------------------------------------------------
// The JSON below defines a Docker cluster with three manager and ten worker
// host nodes.
//
// Naming: Cluster, datacenter and node names may be one or more characters 
//         including case insensitive letters, numbers, dashes, underscores,
//         or periods.

{
    //  name              Cluster name
    //  datacenter        Datacenter name
    //  environment       Describes the type of environment, one of:
    //
    //                      other, dev, test, stage, or prod
    //
    //  time_sources      The FQDNs or IP addresses of the NTP time sources
    //                    used to synchronize the cluster as an array of
    //                    strings.  Reasonable defaults will be used if
    //                    not specified.
    //
    //  package_proxy     Optionally specifies the HTTP URL including the port 
    //                    (generally [3142]) of the local cluster server used for 
    //                    proxying and caching access to Ubuntu and Debian APT packages.
    //
    //                    This defaults to [false].

    ""name"": ""my-cluster"",
    ""datacenter"": ""Seattle"",
    ""environment"": ""Development"",
    ""time_sources"": [ ""0.pool.ntp.org"", ""1.pool.ntp.org"", ""2.pool.ntp.org"" ],
    ""package_proxy"": null,

    // Cluster host machine options.

    ""host"": {

        // Specifies the authentication method to be used to secure SSH sessions
        // to the cluster host nodes.  The possible values are:
        //
        //      password    - username/password
        //      tls         - mutual TLS via public certificates and private keys
        //
        // This defaults to [tls] for better security.
        
        ""ssh_auth"": ""tls"",

        // Cluster hosts are configured with a random root account password.
        // This defaults to [15] characters.  The minumum non-zero length
        // is [8].  Specify [0] to leave the root password unchanged.
        //
        // IMPORTANT: Setting this to zero will leave the cluster open for
        // password authentication in addition to mutual TLS authentication 
        // (if enabled).  Think very carefully before doing this for a 
        // production cluster.

        ""password_length"": 15,

        /// Enables username/password authentication in addition to TLS authentication
        /// when [ssl_auth=tls].  This defaults to [true].

        ""password_auth"": true
    },

    // Docker related options.

    ""docker"": {

        // The version of Docker to be installed.  This can be a released Docker version
        // like [1.13.0] or [latest] to install the most recent production release.  You 
        // may also  specify [test], [experimental] to install the latest test or experimental
        // release.
        //
        // You can also specify the HTTP/HTTPS URI to the binary package to be installed.
        // This is useful for installing a custom build or a development snapshot copied 
        // from https://master.dockerproject.org/.  Be sure to copy the TAR file from:
        //
        //      linux/amd64/docker-<docker-version>-dev.tgz
        // 
        // This defaults to [latest].
        //
        // IMPORTANT!
        //
        // Production clusters should always install a specific version of Docker so 
        // you will be able to add new hosts in the future that will have the same 
        // Docker version as the rest of the cluster.  This also prevents the package
        // manager from inadvertently upgrading Docker.
        //
        // IMPORTANT!
        //
        // It is not possible for the [neon.exe] tool to upgrade Docker on clusters
        // that deployed the [test] or [experimental]> build.

        ""version"": ""latest"".

        // Optionally specifies the URL of the Docker registry the cluster will use to
        // download Docker images.  This defaults to the Public Docker registry: 
        // [https://registry-1.docker.io].
        //
        ""registry"": ""https://registry-1.docker.io""

        // Optionally specifies the user name used to authenticate with the registry
        // mirror and caches.
        // 
        ""registry_username"": "",

        // Optionally specifies the password used to authenticate with the registry
        // mirror and caches.
        // 
        // ""registry_password"": ""
        
        // Optionally specifies that pull-thru registry caches are to be deployed
        // within the cluster on the manager nodes.  This defaults to [true]. 
        //
        ""registry_cache"": true,

        // The Docker daemon container logging options.  This defaults to:
        //
        //      --log-driver=fluentd --log-opt tag= --log-opt fluentd-async-connect=true
        // 
        // which by default, will forward container logs to the cluster logging pipeline.
        // 
        // IMPORTANT:
        //
        // Always use the [--log-opt fluentd-async-connect=true] option when using 
        // the [fluentd] log driver.  Containers without this will stop if the 
        // logging pipeline is not ready when the container starts.
        //
        // You may have individual services and containers opt out of cluster logging by 
        // setting [--log-driver=json-text] or [-log-driver=none].  This can be handy 
        // while debugging Docker images.
        //
        ""log-options"": ""--log-driver=fluentd --log-opt tag= --log-opt fluentd-async-connect=true""
    },

    // Network related options:

    ""network"": {
        
        //  public_subnet         IP subnet assigned to the standard public cluster
        //                        overlay network.  This defaults to [10.249.0.0/16].
        //
        //  public_attachable     Allow non-Docker swarm mode service containers to 
        //                        attach to the standard public cluster overlay network.
        //                        This defaults to [true] for flexibility but you may 
        //                        consider disabling this for better security.
        //
        //  private_subnet        IP subnet assigned to the standard private cluster
        //                        overlay network.  This defaults to [10.248.0.0/16].
        //
        //  private_attachable    Allow non-Docker swarm mode service containers to 
        //                        attach to the standard private cluster overlay network.
        //                        This defaults to [true] for flexibility but you may 
        //                        consider disabling this for better security.
        //
        //  nameservers           The IP addresses of the upstream DNS nameservers to be 
        //                        used by the cluster.  This defaults to the Google Public
        //                        DNS servers: [ ""8.8.8.8"", ""8.8.4.4"" ] when the
        //                        property is NULL or empty.
    },

    // Options describing the default overlay network created for the 

    // HashiCorp Consul distributed service discovery and key/valuestore settings.
    // Note that Consul is available in every cluster.
    
    ""consul"": {

        //  version               The version to be installed.  This defaults to
        //                        a reasonable recent version.
        //
        //  encryption_key        16-byte shared secret (Base64) used to encrypt 
        //                        Consul network traffic.  This defaults to
        //                        a cryptographically generated key.  Use the 
        //                        command below to generate a custom key:
        //
        //                              neon create key 
    },

    // HashiCorp Vault secret server options.
    //
    // Note: Vault depends on Consul which must be enabled.

    ""vault"": {

        //  version               The version to be installed.  This defaults to
        //                        a reasonable recent version.
        //
        //  key_count             The number of unseal keys to be generated by 
        //                        Vault when it is initialized.  This defaults to [1].
        //
        //  key_threshold         The minimum number of unseal keys that will be 
        //                        required to unseal Vault.  This defaults to [1].
        //
        //  maximum_lease         The maximum allowed TTL for a Vault token or secret.  
        //                        This limit will be silently enforced by Vault.  This 
        //                        can be expressed as hours with an [h] suffix, minutes 
        //                        using [m] and seconds using [s].  You can also combine
        //                        these like [10h30m10s].  This defaults to [0] which
        //                        specifies about 290 years (essentially infinity).
        //
        //  default_lease         The default allowed TTL for a new Vault token or secret 
        //                        if no other duration is specified .  This can be expressed
        //                        as hours with an [h] suffix, minutes using [m] and seconds
        //                        using [s].  You can also combine these like [10h30m10s].  
        //                        This defaults to [0] which specifies about 290 years 
        //                        (essentially infinity).

        ""key_count"": 1,
        ""key_threshold"": 1,
        ""maximum_lease"": ""0"",
        ""default_lease"": ""0""
    },

    // Cluster logging options.
    //
    // Logging requires that Weave Network also be enabled.

    ""log"": {

        //  enabled               Indicates that the cluster logging pipeline will be enabled.
        //                        This defaults to [true].
        //
        //  es_image              The [Elasticsearch] Docker image to be used
        //                        to persist cluster log events.  This defaults to 
        //                        [neoncluster/elasticsearch:latest].
        //
        //  es_shards             The number of Elasticsearch shards. This defaults to 1.
        //
        //  es_replication        The number of times Elasticsearch will replicate 
        //                        data within the logging cluster for fault tolerance.
        //                        This defaults to 1 which ensures that the greatest 
        //                        data capacity at the cost of no fault tolerance.
        //
        //  es_memory             The amount of RAM to dedicate to each cluster log
        //                        related Elasticsearch container.  This can be expressed
        //                        as ### or ###B (bytes), ###K (kilobytes), ###M (megabytes),
        //                        or ###G (gigabytes).  This defaults to 2G.
        //
        //  kibana_image          The [Kibana] Docker image to be used to present the
        //                        cluster log user interface.  This defaults to
        //                        [neoncluster/kibana:latest].
        //
        //  host_image            The Docker image to be run as a local container on
        //                        every node to forward host log events to the cluster
        //                        log aggregator.  This defaults to
        //                        [neoncluster/neon-log-host:latest].
        //
        //  collector_image       The Docker image to be run as a service on the 
        //                        cluster that aggregates log events from the node
        //                        log forwarders and pushes them into Elasticsearch.
        //                        This defaults to  [neoncluster/neon-log-collector:latest].
        //
        //  collector_instances   The number of TD-Agent based collectors to be deployed
        //                        to receive, transform, and persist events collected by
        //                        the cluster nodes.  This defaults to 1.
        //
        //  collector_constraints Zero or more Docker Swarm style container placement
        //                        constraints referencing built-in or custom
        //                        node labels used to locate TD-Agent collector
        //                        containers.
        //
        //  metricbeat_image      Identifies the [Elastic Metricbeat] container image 
        //                        to be run as a service on every node of the cluster to
        ///                       capture Docker host node metrics.  This defaults to
        //                        [neoncluster/metricbeat:latest].

        // IMPORTANT: At least one node must have [Labels.LogEsData=true]
        //            when logging is enabled.  This specifies where cluster
        //            log data is to be stored.

        // IMPORTANT: The Elasticsearch and Kibana images must deploy compatible
        //            versions of these service.

        ""Enabled"": true
    },

    // Dashboard options.

    ""dashboard"": {
    
        // Install the Elastic Kibana dashboard if cluster logging is enabled.
        // This defaults to [true].

        ""kibana"": true,

        // Install the Consul user interface.  This defaults to [true].

        ""consul"": true
    },

    // .NET Core options.

    ""dotnet"": {

        //  enabled           Indicates that .NET Core should be installed on
        //                    all nodes.  This defaults to [false].
        //
        //  version           The version to be installed.  This defaults to
        //                    a reasonable recent version.

        ""enabled"": false
    },

    //-------------------------------------------------------------------------
    // This section describes the physical and/or virtual machines that 
    // will host your cluster.  There are two basic types of nodes:
    //
    //      * Manager Nodes
    //      * Worker Nodes
    //
    // Manager nodes handle the cluster management tasks.  Both types of
    // nodes can host application containers.
    //
    // Node Properties
    // ---------------
    //
    //      dna_name          IP address or FQDN of the node
    //      manager           true for manager nodes (default=false)
    //      swapping          allow swapping of RAM to disk (default=false)
    //
    // Node Labels
    // -----------
    // Node details can be specified using Docker labels.  These labels
    // will be passed to the Docker daemon when it is launched so they
    // will be available for Swarm filtering operations.  Some labels
    // are also used during cluster configuration.
    //
    // You'll use the [Labels] property to specifiy labels.  NeonCluster
    // predefines several labels.  You may extend these using [Labels.Custom].
    //
    // The following reserved labels are currently supported (see the documentation
    // for more details):
    //
    //      storage_capacity              Storage in MB (int)
    //      storage_local                 Storage is local (bool)
    //      storage_ssd                   Storage is backed by SSD (bool)
    //      storage_redundant             Storage is redundant (bool)
    //      storage_ephemeral             Storage is ephemeral (bool)
    //
    //      compute_cores                 Number of CPU cores (int)
    //      compute_architecture          x32, x64, arm32, arm64
    //      compute_ram                   RAM in MB
    //
    //      physical_location             Location (string)
    //      physical_machine              Computer model (string)
    //      physical_fault_domain         Fault domain (string)
    //      physical_power                Power details (string)
    //
    //      log_es_data                   Host Elasticsearch node for cluster
    //                                    logging data (bool)
    //
    // IMPORTANT: Be sure to set [StorageSSD=true] if your node is backed 
    //            by a SSD so that cluster setup will tune Linux for better 
    //            performance.
    //
    // NOTE:      Docker does not support whitespace in label values.
    //
    // NOTE:      These labels will be provisioned as Docker node labels
    //            (not engine labels).  The built-in labels can be referenced
    //            in Swarm constraint expressions as:
    //
    //                  node.labels.io.neoncluster.[built-in name (lowercase)]
    //
    //            Custom labels can be referenced via:
    //
    //                  node.labels[custom name (lowercase)]
    //
    //            Note that the prefix The [node.labels.io.neoncluster] prefix
    //            is reserved for NeonCluster related labels.

    ""nodes"": {

        //---------------------------------------------------------------------
        // Describe the cluster management nodes by setting [Manager=true].
        // Management nodes host Consul service discovery, Vault secret 
        // management, and the Docker Swarm managers.
        // 
        // NeonClusters must have at least one manager node.  To have
        // high availability, you may deploy three or five management node.
        // Only an odd number of management nodes are allowed up to a
        // maximum of five.  A majority of these must be healthy for the 
        // cluster as a whole to function.

        ""Manage-0"": {
            ""dns_name"": ""manage-0.lilltek.net"",
            ""manager"": true,
            ""labels"": {
                ""log_es_data"": true,
                ""storage_ssd"": true,
                ""custom"": {
                    ""mylabel"": ""Hello-World!""
                }
            }
        },
        ""Manage-1"": {
            ""dns_name"": ""manage-1.lilltek.net"",
            ""manager"": true,
            ""labels"": {
                ""storage_ssd"": true
            }
        },
        ""Manage-2"": {
            ""dns_name"": ""manage-2.lilltek.net"",
            ""manager"": true,
            ""labels"": {
                ""storage_ssd"": true
            }
        },

        //---------------------------------------------------------------------
        // Describe the worker cluster nodes by leaving [Manager=false].
        // Swarm will schedule containers to run on these nodes.

        ""Node-0"": {
            ""dns_name"": ""node-0.lilltek.net"",
            ""labels"": {
                ""storage_ssd"": true
            }
        },
        ""Node-1"": {
            ""dns_name"": ""node-1.lilltek.net"",
            ""labels"": {
                ""storage_ssd"": true
            }
        },
        ""Node-2"": {
            ""dns_name"": ""node-2.lilltek.net"",
            ""labels"": {
                ""storage_ssd"": true
            }
        },
        ""Node-3"": {
            ""dns_name"": ""node-3.lilltek.net"",
            ""labels"": {
                ""storage_ssd"": true
            }
        },
        ""Node-4"": {
            ""dns_name"": ""node-4.lilltek.net"",
            ""labels"": {
                ""storage_ssd"": true
            }
        },
        ""Node-5"": {
            ""dns_name"": ""node-5.lilltek.net"",
            ""labels"": {
                ""storage_ssd"": true
            }
        },
        ""Node-6"": {
            ""dns_name"": ""node-6.lilltek.net"",
            ""labels"": {
                ""storage_ssd"": true
            }
        },
        ""Node-7"": {
            ""dns_name"": ""node-7.lilltek.net"",
            ""labels"": {
                ""storage_ssd"": true
            }
        },
        ""Node-8"": {
            ""dns_name"": ""node-8.lilltek.net"",
            ""labels"": {
                ""storage_ssd"": true
            }
        },
        ""Node-9"": {
            ""dns_name"": ""node-9.lilltek.net"",
            ""labels"": {
                ""storage_ssd"": true
            }
        }
    }
}

//-----------------------------------------------------------------------------
// Cluster definition files are preprocessed to remove comments as well as to
// implement variables and conditionals:
//
//      * Comment lines
//
//      * Variables defined like: 
//
//          #define myvar1
//          #define myvar2=Hello
//
//      * Variables referenced via:
//
//          $<myvar1>
//
//      * Environment variables referenced via:
//
//          $<<ENV_VAR>>
//
//      * If statements:
//
//          #define DEBUG=TRUE
//          #if $<DEBUG>==TRUE
//              Do something
//          #else
//              Do something else
//          #endif
//
//          #if defined(DEBUG)
//              Do something
//          #endif
//
//          #if undefined(DEBUG)
//              Do something
//          #endif
//
//      * Switch statements:
//
//          #define datacenter=uswest
//          #switch $<datacenter>
//              #case uswest
//                  Do something
//              #case useast
//                  Do something else
//              #default
//                  Do the default thing
//          #endswitch
";
            Console.Write(sampleJson);
        }
    }
}
