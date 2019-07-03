//-----------------------------------------------------------------------------
// FILE:	    GetCommand.cs
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
    /// Implements the <b>get</b> command.
    /// </summary>
    public class GetCommand : ICommand
    {
        private const string usage = @"
Writes a specified value from the currently logged in cluster to the
standard output.  Global cluster values as well as node specific ones
can be obtained.

USAGE:

    neon get VALUE
    neon get NODE.VALUE

ARGUMENTS:

    VALUE       - identifies the desired value
    NODE        - optionally identifies a specific node.

CLUSTER VALUE IDENTIFIERS:

    username                - root account username
    password                - root account password
    sshkey-client-pem       - client SSH private key (PEM format)
    sshkey-client-ppk       - client SSH private key (PPK format)

NODE VALUE IDENTIFIERS:

    sshkey-fingerprint      - SSH host key fingerprint

";
        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "get" }; }
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
            if (commandLine.HasHelpOption)
            {
                Console.WriteLine(usage);
                Program.Exit(0);
            }

            var clusterSecrets = Program.ClusterSecrets;

            if (clusterSecrets == null)
            {
                Console.Error.WriteLine(Program.MustLoginMessage);
                Program.Exit(1);
            }

            if (commandLine.Arguments.Length != 1)
            {
                Console.Error.WriteLine("*** ERROR: VALUE-EXPR expected.");
                Program.Exit(1);
            }

            var valueExpr = commandLine.Arguments[0];

            if (valueExpr.Contains('.'))
            {
                // Node expression.

                var fields   = valueExpr.Split(new char[] { '.' }, 2);
                var nodeName = fields[0];
                var value    = fields[1];

                if (string.IsNullOrEmpty(nodeName) || string.IsNullOrEmpty(value))
                {
                    Console.Error.WriteLine("*** ERROR: VALUE-EXPR is not valid.");
                    Program.Exit(1);
                }

                var node = clusterSecrets.Definition.Nodes.SingleOrDefault(n => n.Name == nodeName.ToLowerInvariant());

                if (node == null)
                {
                    Console.Error.WriteLine($"*** ERROR: Node [{nodeName}] is not present.");
                    Program.Exit(1);
                }

                switch (value.ToLowerInvariant())
                {
                    case "sshkey-fingerprint":

                        Console.WriteLine(node.SshKeyFingerprint);
                        break;

                    default:

                        Console.Error.WriteLine($"*** ERROR: Unknown value [{value}].");
                        Program.Exit(1);
                        break;
                }
            }
            else
            {
                // Cluster expression.

                switch (valueExpr.ToLowerInvariant())
                {
                    case "username":

                        Console.WriteLine(clusterSecrets.RootAccount);
                        break;

                    case "password":

                        Console.WriteLine(clusterSecrets.RootPassword);
                        break;

                    case "sshkey-client-pem":

                        Console.WriteLine(clusterSecrets.SshClientKey.PrivatePEM);
                        break;

                    case "sshkey-client-ppk":

                        Console.WriteLine(clusterSecrets.SshClientKey.PrivatePPK);
                        break;

                    default:

                        Console.Error.WriteLine($"*** ERROR: Unknown value [{valueExpr}].");
                        Program.Exit(1);
                        break;
                }
            }
        }
    }
}
