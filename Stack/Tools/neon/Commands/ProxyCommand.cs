//-----------------------------------------------------------------------------
// FILE:	    ProxyCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Consul;
using Newtonsoft;
using Newtonsoft.Json;

using Neon.Cluster;
using Neon.Stack.Common;
using Neon.Stack.Cryptography;

namespace NeonCluster
{
    /// <summary>
    /// Implements the <b>proxy</b> command.
    /// </summary>
    public class ProxyCommand : ICommand
    {
        private const string proxyManagerPrefix = "neon/service/neon-proxy-manager";
        private const string vaultCertPrefix    = "neon-secret/cert";

        private const string usage = @"
Manages the cluster's public and private proxies.

USAGE:

    neon proxy NAME definition
    neon proxy NAME delete-route ROUTE
    neon proxy NAME get-route ROUTE
    neon proxy NAME list-routes
    neon proxy NAME set-route FILE
    neon proxy NAME set-route -
    neon proxy NAME rebuild
    neon proxy NAME settings FILE
    neon proxy NAME settings -

ARGUMENTS:

    NAME    - Proxy name: [public] or [private].
    ROUTE   - Route name.
    FILE    - Path to a JSON file.
    -       - Indicates that JSON is read from standard input.

COMMANDS:

    definition      - Returns JSON details for all routes and
                      settings.

    delete-route    - Removes a route (if it exists).

    get-route       - Returns a specific route.

    list-routes     - Lists the route names.

    set-route       - Adds or updates a route from a JSON file
                      or by reading standard input.

    rebuild         - Forces the proxy manager to rebuild the 
                      proxy configuration.

    settings        - Updates the proxy global settings from a
                      JSON file or by reading standard input.

ROUTES:

NeonCluster proxies support two types of routes: HTTP/S and TCP.
Each route defines one or more frontend and backends.

HTTP/S frontends handle requests for a hostname for one or more hostname
and port combinations.  HTTPS is enabled by specifying the name of a
certificate loaded into the cluster.  The port defaults to 80 for HTTP
and 443 for HTTPS.   The [https_redirect] option indicates that clients
making HTTP requests should be redirected with the HTTPS scheme.

TCP frontends simply specify one of the TCP ports assigned to the proxy
(note that the first two ports are reserved for HTTP and HTTPS).

Backends specify one or more target servers by IP address or DNS name
and port number.

Routes are specified using JSON.  Here's an example HTTP/S route that
accepts HTTP traffic for [foo.com] and [www.foo.com] and redirects it
to HTTPS and then also accepts HTTPS traffic using the [foo.com] certificate.
Traffic is routed to the [foo_service] on port 80 which could be a Docker
swarm mode service or DNS name.

    {
        ""mode"": ""http"",
        ""https_redirect"": true,
        ""frontends"": [
            { ""host"": ""foo.com"", ""port"": 80 },
            { ""host"": ""www.foo.com"" },
            { ""host"": ""foo.com"", ""cert_name"": ""foo.com"", ""port"": 443 },
            { ""host"": ""www.foo.com"", ""cert_name"": ""foo.com"" }
        ],
        ""backends"": [
            { ""address"": ""foo_service"", ""port"": 80 }
        ]
    }

Here's an example TCP route that load balances TCP connections to the
11102 port to three backend servers on port 1000:

    {
        ""mode"": ""tcp"",
        ""frontends"": [
            { ""port"": 11102 }
        ],
        ""backends"": [
            { ""address"": ""10.0.1.40"", ""port"": 1000 },
            { ""address"": ""10.0.1.41"", ""port"": 1000 },
            { ""address"": ""10.0.1.42"", ""port"": 1000 }
        ]
    }

See the documentation for more proxy route and setting details.
";
        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "proxy" }; }
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
            if (commandLine.HasHelpOption || commandLine.Arguments.Length == 0)
            {
                Console.WriteLine(usage);
                Program.Exit(0);
            }

            Program.ConnectCluster();

            // Process the command arguments.

            ProxyManager proxyManager = null;

            var proxyName = commandLine.Arguments.FirstOrDefault();

            switch (proxyName)
            {
                case "public":

                    proxyManager = NeonClusterHelper.Cluster.PublicProxy;
                    break;

                case "private":

                    proxyManager = NeonClusterHelper.Cluster.PrivateProxy;
                    break;

                default:

                    Console.WriteLine($"*** ERROR: Proxy name must be one of [public] or [private] ([{proxyName}] is not valid).");
                    Program.Exit(1);
                    break;
            }

            commandLine = commandLine.Shift(1);

            var command = commandLine.Arguments.FirstOrDefault();

            if (command == null)
            {
                Console.WriteLine(usage);
                Program.Exit(1);
            }

            commandLine = commandLine.Shift(1);

            string routeName;

            switch (command.ToLowerInvariant())
            {
                case "definition":

                    Console.WriteLine(NeonHelper.JsonSerialize(proxyManager.GetDefinition(), Formatting.Indented));
                    break;

                case "get-route":

                    routeName = commandLine.Arguments.FirstOrDefault();

                    if (string.IsNullOrEmpty(routeName))
                    {
                        Console.WriteLine("*** ERROR: [ROUTE] argument expected.");
                        Program.Exit(1);
                    }

                    if (!ClusterDefinition.IsValidName(routeName))
                    {
                        Console.WriteLine($"*** ERROR: [{routeName}] is not a valid route name.");
                        Program.Exit(1);
                    }

                    // Fetch a specific proxy route and output it.

                    var route = proxyManager.GetRoute(routeName);

                    if (route == null)
                    {
                        Console.WriteLine($"*** ERROR: Proxy [{proxyName}] route [{routeName}] does not exist.");
                        Program.Exit(1);
                    }

                    Console.WriteLine(NeonHelper.JsonSerialize(route, Formatting.Indented));
                    break;

                case "delete-route":

                    routeName = commandLine.Arguments.FirstOrDefault();

                    if (string.IsNullOrEmpty(routeName))
                    {
                        Console.WriteLine("*** ERROR: [ROUTE] argument expected.");
                        Program.Exit(1);
                    }

                    if (!ClusterDefinition.IsValidName(routeName))
                    {
                        Console.WriteLine($"*** ERROR: [{routeName}] is not a valid route name.");
                        Program.Exit(1);
                    }

                    if (proxyManager.DeleteRoute(routeName))
                    {
                        Console.WriteLine($"Deleted proxy [{proxyName}] route [{routeName}].");
                    }
                    else
                    {
                        Console.WriteLine($"*** ERROR: Proxy [{proxyName}] route [{routeName}] does not exist.");
                        Program.Exit(1);
                    }
                    break;

                case "list-routes":

                    var nameList = proxyManager.ListRoutes().ToArray();

                    if (nameList.Length == 0)
                    {
                        Console.WriteLine("No routes exist.");
                    }
                    else
                    {
                        foreach (var name in proxyManager.ListRoutes())
                        {
                            Console.WriteLine(name);
                        }
                    }
                    break;

                case "set-route":

                    if (commandLine.Arguments.Length != 1)
                    {
                        Console.WriteLine("*** ERROR: FILE or [-] argument expected.");
                        Program.Exit(1);
                    }

                    // Load the route.

                    var routeFile = commandLine.Arguments[0];

                    string routeJson;

                    if (routeFile == "-")
                    {
                        using (var input = Console.OpenStandardInput())
                        {
                            using (var reader = new StreamReader(input, detectEncodingFromByteOrderMarks: true))
                            {
                                routeJson = reader.ReadToEnd();
                            }
                        }
                    }
                    else
                    {
                        routeJson = File.ReadAllText(routeFile);
                    }

                    var proxyRoute = ProxyRoute.Parse(routeJson);

                    routeName = proxyRoute.Name;

                    if (!ClusterDefinition.IsValidName(routeName))
                    {
                        Console.WriteLine($"*** ERROR: [{routeName}] is not a valid route name.");
                        Program.Exit(1);
                    }

                    if (proxyManager.SetRoute(proxyRoute))
                    {
                        Console.WriteLine($"Proxy [{proxyName}] route [{routeName}] has been updated.");
                    }
                    else
                    {
                        Console.WriteLine($"Proxy [{proxyName}] route [{routeName}] has been added.");
                    }
                    break;

                case "rebuild":

                    proxyManager.Rebuild();
                    break;

                case "settings":

                    var settingsFile = commandLine.Arguments.FirstOrDefault();

                    if (string.IsNullOrEmpty(settingsFile))
                    {
                        Console.WriteLine("*** ERROR: [-] or FILE argument expected.");
                        Program.Exit(1);
                    }

                    string settingsJson;

                    if (settingsFile == "-")
                    {
                        using (var input = Console.OpenStandardInput())
                        {
                            using (var reader = new StreamReader(input, detectEncodingFromByteOrderMarks: true))
                            {
                                settingsJson = reader.ReadToEnd();
                            }
                        }
                    }
                    else
                    {
                        settingsJson = File.ReadAllText(settingsFile);
                    }

                    var proxySettings = NeonHelper.JsonDeserialize<ProxySettings>(settingsJson);

                    proxyManager.UpdateSettings(proxySettings);
                    Console.WriteLine($"Proxy [{proxyName}] settings have been updated.");
                    break;

                default:

                    Console.Error.WriteLine($"*** ERROR: Unknown subcommand [{command}].");
                    Program.Exit(1);
                    break;
            }
        }
    }
}
