//-----------------------------------------------------------------------------
// FILE:	    SshCommand.cs
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
    /// Implements the <b>ssh</b> command.
    /// </summary>
    public class SshCommand : ICommand
    {
        private const string usage = @"
Opens a PuTTY SSH connection to the named node in the current cluster
or the first manager node if no node is specified.

USAGE:

    neon ssh [NODE]

ARGUMENTS:

    NODE        - Optionally names the target cluster node.
                  Otherwise, the first manager node will be opened.
";
        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "ssh" }; }
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

            NodeDefinition node;

            if (commandLine.Arguments.Length == 0)
            {
                node = clusterSecrets.Definition.SortedManagers.First();
            }
            else
            {
                var name = commandLine.Arguments[0];

                node = clusterSecrets.Definition.Nodes.SingleOrDefault(n => n.Name == name);

                if (node == null)
                {
                    Console.WriteLine($"*** ERROR: The node [{name}] does not exist.");
                    Program.Exit(1);
                }
            }

            // The host's SSH key fingerprint looks something like the example below.  We
            // need to extract the MD5 HEX part to generate a PuTTY compatible fingerprint.
            //
            //      2048 MD5:cb:2f:f1:68:4b:aa:b3:8a:72:4d:53:f6:9f:5f:6a:fa root@manage-0 (RSA)

            const string    md5Pattern = "MD5:";
            string          fingerprint;
            int             startPos;
            int             endPos;

            startPos = node.SshKeyFingerprint.IndexOf(md5Pattern);

            if (startPos == -1)
            {
                Console.WriteLine($"*** ERROR: Cannot parse host's SSH key fingerprint [{node.SshKeyFingerprint}].");
                Program.Exit(1);
            }

            startPos += md5Pattern.Length;

            endPos = node.SshKeyFingerprint.IndexOf(' ', startPos);

            if (endPos == -1)
            {
                fingerprint = node.SshKeyFingerprint.Substring(startPos).Trim();
            }
            else
            {
                fingerprint = node.SshKeyFingerprint.Substring(startPos, endPos - startPos).Trim();
            }

            // Launch PuTTY.

            if (!File.Exists(Program.PuttyPath))
            {
                Console.WriteLine($"*** ERROR: PuTTY is application not installed at [{Program.PuttyPath}].");
                Program.Exit(1);
            }

            switch (clusterSecrets.Definition.Host.SshAuth)
            {
                case AuthMethods.Tls:

                    // We're going write the private key to the cluster temp folder.  For Windows
                    // workstations, this is probably encrypted and hopefully Linux/OSX is configured
                    // to encrypt user home directories.  We want to try to avoid persisting unencrypted
                    // cluster credentials.

                    var keyPath = Path.Combine(Program.ClusterTempFolder, $"{clusterSecrets.Name}.key");

                    File.WriteAllText(keyPath, clusterSecrets.SshClientKey.PrivatePPK);

                    try
                    {
                        Process.Start(Program.PuttyPath, $"-l {clusterSecrets.RootAccount} -i \"{keyPath}\" {node.DnsName}:22 -hostkey \"{fingerprint}\"");
                    }
                    finally
                    {
                        // Wait a bit for WinSCP to start and then delete the key.

                        while (true)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(5));

                            try
                            {
                                File.Delete(keyPath);
                                break;
                            }
                            catch
                            {
                                // Intentionally ignoring this.
                            }
                        }
                    }
                    break;

                case AuthMethods.Password:

                    Process.Start(Program.PuttyPath, $"-l {clusterSecrets.RootAccount} -pw {clusterSecrets.RootPassword} {node.DnsName}:22 -hostkey \"{fingerprint}\"");
                    break;

                default:

                    throw new NotSupportedException($"Unsupported SSH authentication method [{clusterSecrets.Definition.Host.SshAuth}].");
            }
        }
    }
}
