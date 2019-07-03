﻿//-----------------------------------------------------------------------------
// FILE:	    NeonHostPorts.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Cluster
{
    /// <summary>
    /// Defines the Docker host network ports in the <b>5000-5499</b> range reserved 
    /// by NeonCluster used by local services, containters and services on the mesh betwork.
    /// </summary>
    /// <remarks>
    /// <note>
    /// <b>IMPORTANT:</b> Do not change any of these values without really knowing what
    /// you're doing.  It's likely that these values have been literally embedded
    /// in cluster configuration scripts as well as Docker images.  Any change is likely
    /// to break things.
    /// </note>
    /// <note>
    /// <b>IMPORTANT:</b> These definitions must match those in the <b>$\Stack\Docker\Images\neoncluster.sh</b>
    /// file.  You must manually update that file and then rebuild and push the containers
    /// as well as redeploy all clusters from scratch.
    /// </note>
    /// <para>
    /// These ports are organized into the following ranges:
    /// </para>
    /// <list type="table">
    /// <item>
    ///     <term><b>5000-5099</b></term>
    ///     <description>
    ///     Reserved for various native Linux services and Docker containers running
    ///     on the host.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>5100-5299</b></term>
    ///     <description>
    ///     Reserved for services proxied by the <b>neon-proxy-public</b> service
    ///     on the <b>neon-cluster-public</b> network.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>5300-5499</b></term>
    ///     <description>
    ///     Reserved for services proxied by the <b>neon-proxy-private</b> service
    ///     on the <b>neon-cluster-private</b> network.
    ///     </description>
    /// </item>
    /// </list>
    /// </remarks>
    public static class NeonHostPorts
    {
        /// <summary>
        /// The first reserved NeonCluster port.
        /// </summary>
        public const int First = 5000;

        /// <summary>
        /// The last reserved NeonCluster port.
        /// </summary>
        public const int Last = 5499;

        //---------------------------------------------------------------------
        // Cluster dashboard ports.

        /// <summary>
        /// The main NeonCluster dashboard.
        /// </summary>
        public const int Dashboard = 5000;

        /// <summary>
        /// The <b>neon-log-kibana</b> (Kibana) log analysis dashboard.
        /// </summary>
        public const int Kibana = 5001;

        /// <summary>
        /// The HTTP port exposed by the manager <b>neon-registry-cache</b> containers.
        /// </summary>
        public const int RegistryCache = 5002;

        /// <summary>
        /// The <b>neon-proxy-vault</b> service port used for routing HTTP traffic to the
        /// Vault servers running on the manager nodes.
        /// </summary>
        public const int ProxyVault = 5003;

        /// <summary>
        /// The public HTTP API port exposed by individual <b>neon-log-esdata-#</b>
        /// Elasticsearch log repository containers.
        /// </summary>
        public const int LogEsDataHttp = 5004;

        /// <summary>
        /// The TCP port exposed by individual <b>neon-log-esdata-#</b> Elasticsearch
        /// log repository containers for internal inter-node communication.
        /// </summary>
        public const int LogEsDataTcp = 5005;

        /// <summary>
        /// The UDP port exposed by the <b>neon-log-host</b> containers that receives
        /// SYSLOG events from the HAProxy based services and perhaps other sources.
        /// </summary>
        public const int LogHostSysLog = 5006;

        //---------------------------------------------------------------------
        // Ports [5100-5299] are reserved for the public proxy that routes
        // external traffic into the cluster.
        //
        // [5100-5102] are used to route general purpose HTTP/S traffic
        //             to both NeonCluster and user services.
        //
        // [5102-5109] are reserved for internal NeonCluster TCP routes.
        //
        // [5110-5299] are available for use by user services for TCP or
        //             HTTP traffic.

        /// <summary>
        /// The first port reserved for the public proxy.
        /// </summary>
        public const int ProxyPublicFirst = 5100;

        /// <summary>
        /// The last port reserved for the public proxy.
        /// </summary>
        public const int ProxyPublicLast = 5299;

        /// <summary>
        /// The first non-reserved public proxy port available for user services.
        /// </summary>
        public const int ProxyPublicFirstUser = 5110;

        /// <summary>
        /// The <b>neon-proxy-public</b> service port for routing external HTTP
        /// (e.g. Internet) requests to services within the cluster.
        /// </summary>
        public const int ProxyPublicHttp = 5100;

        /// <summary>
        /// The <b>neon-proxy-public</b> service port for routing external HTTPS
        /// (e.g. Internet) requests to services within the cluster.
        /// </summary>
        public const int ProxyPublicHttps = 5101;

        //---------------------------------------------------------------------
        // Ports [5300-5499] are reserved for the private cluster proxy.
        //
        // [5300-5301] are used to route general purpose HTTP/S traffic
        //             to both NeonCluster and user services.
        //
        // [5302-5309] are reserved for internal NeonCluster TCP routes.
        //
        // [5310-5499] are available for use by user services for TCP or
        //             HTTP traffic.

        /// <summary>
        /// The first port reserved for the private proxy.
        /// </summary>
        public const int ProxyPrivateFirst = 5300;

        /// <summary>
        /// The last port reserved for the private proxy.
        /// </summary>
        public const int ProxyPrivateLast = 5499;

        /// <summary>
        /// The first non-reserved private proxy port available for user services.
        /// </summary>
        public const int ProxyPrivateFirstUser = 5310;

        /// <summary>
        /// The <b>neon-proxy-private</b> service port for routing internal HTTP traffic.  
        /// This typically used to load balance traffic to stateful services that
        /// can't be deployed as Docker swarm mode services.
        /// </summary>
        public const int ProxyPrivateHttp = 5300;

        /// <summary>
        /// The <b>neon-proxy-private</b> service port for routing internal HTTPS traffic.  
        /// This typically used to load balance traffic to stateful services that
        /// can't be deployed as Docker swarm mode services.
        /// </summary>
        public const int ProxyPrivateHttps = 5301;

        /// <summary>
        /// The <b>neon-proxy-private</b> service port for routing internal TCP traffic
        /// to forward log events from the <b>neon-log-host</b> containers running on 
        /// the nodes to the <b>neon-log-collector</b> service.
        /// </summary>
        public const int ProxyPrivateTcpLogCollector = 5302;

        /// <summary>
        /// The <b>neon-proxy-private</b> service port for routing internal HTTP traffic
        /// to the logging Elasticsearch cluster.
        /// </summary>
        public const int ProxyPrivateHttpLogEsData = 5303;
    }
}
