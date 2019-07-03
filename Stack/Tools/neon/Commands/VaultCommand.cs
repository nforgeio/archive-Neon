﻿//-----------------------------------------------------------------------------
// FILE:	    VaultCommand.cs
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
using Renci.SshNet.Common;

using Neon.Cluster;
using Neon.Stack.Common;

namespace NeonCluster
{
    /// <summary>
    /// Implements the <b>vault</b> commands.
    /// </summary>
    public class VaultCommand : ICommand
    {
        private const string usage = @"
Runs a HashiCorp Vault command on the cluster.  All command line arguments
and options as well are passed through to the Vault CLI.

USAGE:

    neon vault --help               - Prints Vault (and this) help
    neon vault [OPTIONS] [ARGS...]  - Invokes a Vault command

ARGS: The standard HashCorp Vault command arguments and options.

OPTIONS :

    --neon-node=NODE    - Specifies the target node.  The Vault command will
                          be executed on one of the manager node when this 
                          isn't specified.

NOTE: Vault commands are automtaically provided with the root token from the 
      local cluster secrets.

NOTE: The [unseal] command has been modified to automatically include
      the unseal key saved with the cluster secrets on the local
      workstation.

NOTE: Vault commands may only be submitted to manager nodes.

NOTE: The following Vault commands are not supported:

      init, rekey, server and ssh
";
        private ClusterProxy        cluster;
        private VaultCredentials    vaultCredentials;

        private const string remoteVaultPath = "/usr/local/bin/vault";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "vault" }; }
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
            get { return true; }
        }

        /// <inheritdoc/>
        public void Help()
        {
            Console.WriteLine(usage);
        }

        /// <inheritdoc/>
        public void Run(CommandLine commandLine)
        {
            var clusterSecrets = Program.ClusterSecrets;
            
            if (commandLine.HasHelpOption && clusterSecrets == null)
            {
                Console.WriteLine(usage);
                Program.Exit(0);
            }

            if (clusterSecrets == null)
            {
                Console.Error.WriteLine(Program.MustLoginMessage);
                Program.Exit(1);
            }

            // Initialize the cluster.

            cluster          = new ClusterProxy(Program.ClusterSecrets, Program.CreateNodeProxy<NodeDefinition>);
            vaultCredentials = clusterSecrets.VaultCredentials;

            // Strip out the first item of the command line (note that we're
            // not using Shift() because that will reorder any options).

            commandLine = new CommandLine(commandLine.Items.Skip(1).ToArray());

            // Determine which node we're going to target.

            NodeProxy<NodeDefinition>   node;
            string                      nodeName = commandLine.GetOption("--neon-node", null);
            CommandBundle               bundle;
            CommandResponse             response;

            if (!string.IsNullOrEmpty(nodeName))
            {
                node = cluster.GetNode(nodeName);
            }
            else
            {
                node = cluster.Manager;
            }

            // Strip all of the options starting with "--neon-" from the command line.

            var items = new List<string>();

            foreach (var item in commandLine.Items)
            {
                if (!item.StartsWith("--neon-"))
                {
                    items.Add(item);
                }
            }

            commandLine = new CommandLine(items.ToArray());

            // We're going to print help from Vault first followed by
            // help for the [neon.exe] command.

            if (commandLine.HasHelpOption)
            {
                response = node.SudoCommand($"{remoteVaultPath} {commandLine}", RunOptions.IgnoreRemotePath);

                if (commandLine.Arguments.Length == 0)
                {
                    Console.WriteLine(new string('-', 80));
                    Console.WriteLine("NEON.EXE Help:");
                    Console.WriteLine(usage);

                    Console.WriteLine(new string('-', 80));
                    Console.WriteLine("HashiCorp Vault Help:");
                    Console.WriteLine();
                    Console.WriteLine(response.AllText);

                    Program.Exit(response.ExitCode);
                }
                else
                {
                    Console.WriteLine(response.AllText);
                    Program.Exit(response.ExitCode);
                }
            }

            string command = null;

            if (commandLine.Arguments.Length > 0)
            {
                command = commandLine.Arguments[0];
            }

            switch (command)
            {
                case "init":
                case "rekey":
                case "server":
                case "ssh":

                    Console.Error.WriteLine($"*** ERROR: [neon vault {command}] is not supported.");
                    Program.Exit(1);
                    break;

                case "seal":

                    // We need to seal the Vault instance on every manager node unless a
                    // specific node was requsted via [--neon-node].
                    //
                    // Note also that it's not possible to seal a node that's in standby
                    // mode so we'll restart the Vault container instead.

                    if (!string.IsNullOrEmpty(nodeName))
                    {
                        Console.WriteLine();

                        response = node.SudoCommand($"vault-direct status");

                        if (response.ExitCode != 0)
                        {
                            Console.WriteLine($"[{node.Name}] is already sealed");
                        }
                        else
                        {
                            var standbyMode = response.AllText.Contains("Mode: standby");

                            if (standbyMode)
                            {
                                Console.WriteLine($"[{node.Name}] restaring to seal standby vault...");

                                response = node.SudoCommand($"systemctl restart vault");

                                if (response.ExitCode == 0)
                                {
                                    Console.WriteLine($"[{node.Name}] sealed");
                                }
                                else
                                {
                                    Console.WriteLine($"[{node.Name}] restart failed");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[{node.Name}] sealing...");

                                response = node.SudoCommand($"export VAULT_TOKEN={vaultCredentials.RootToken} && vault-direct seal", RunOptions.Classified);

                                if (response.ExitCode == 0)
                                {
                                    Console.WriteLine($"[{node.Name}] sealed");
                                }
                                else
                                {
                                    Console.WriteLine($"[{node.Name}] seal failed");
                                }
                            }
                        }

                        Program.Exit(response.ExitCode);
                    }
                    else
                    {
                        var failed = false;

                        foreach (var manager in cluster.Managers)
                        {
                            Console.WriteLine();

                            try
                            {
                                response = manager.SudoCommand($"vault-direct status");
                            }
                            catch (SshOperationTimeoutException)
                            {
                                Console.WriteLine($"[{manager.Name}] ** unavailable **");
                                continue;
                            }

                            var standbyMode = response.AllText.Contains("Mode: standby");

                            if (response.ExitCode != 0)
                            {
                                Console.WriteLine($"[{manager.Name}] is already sealed");
                            }
                            else
                            {
                                response = manager.SudoCommand($"vault-direct seal");

                                if (standbyMode)
                                {
                                    Console.WriteLine($"[{manager.Name}] restaring to seal standby vault...");

                                    response = manager.SudoCommand($"systemctl restart vault");

                                    if (response.ExitCode == 0)
                                    {
                                        Console.WriteLine($"[{manager.Name}] restart/seal [standby]");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[{manager.Name}] restart/seal failed [standby]");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"[{manager.Name}] sealing...");

                                    response = manager.SudoCommand($"export VAULT_TOKEN={vaultCredentials.RootToken} && vault-direct seal", RunOptions.Classified);

                                    if (response.ExitCode == 0)
                                    {
                                        Console.WriteLine($"[{manager.Name}] sealed");
                                    }
                                    else
                                    {
                                        failed = true;
                                        Console.WriteLine($"[{manager.Name}] seal failed");
                                    }
                                }
                            }
                        }

                        Program.Exit(failed ? 1 : 0);
                    }
                    break;

                case "status":

                    // We need to obtain the status from the Vault instance on every manager node unless a
                    // specific node was requsted via [--neon-node].

                    Console.WriteLine();

                    if (!string.IsNullOrEmpty(nodeName))
                    {
                        response = node.SudoCommand("vault-direct status");

                        Console.WriteLine(response.AllText);
                        Program.Exit(response.ExitCode);
                    }
                    else
                    {
                        var failed   = false;
                        var allSealed = true;

                        foreach (var manager in cluster.Managers)
                        {
                            try
                            {
                                response = manager.SudoCommand("vault-direct status");
                            }
                            catch (SshOperationTimeoutException)
                            {
                                Console.WriteLine($"[{manager.Name}] ** unavailable **");
                                continue;
                            }

                            var standbyMode = response.AllText.Contains("Mode: standby");
                            var mode        = standbyMode ? "[standby]" : "[leader]  <---";

                            if (response.ExitCode == 0)
                            {
                                allSealed = false;
                                Console.WriteLine($"[{manager.Name}] unsealed {mode}");
                            }
                            else if (response.ExitCode == 2)
                            {
                                Console.WriteLine($"[{manager.Name}] sealed");
                            }
                            else
                            {
                                failed = true;
                                Console.WriteLine($"[{manager.Name}] error getting status");
                            }
                        }

                        if (allSealed)
                        {
                            Program.Exit(2);
                        }
                        else
                        {
                            Program.Exit(failed ? 1 : 0);
                        }
                    }
                    break;

                case "unseal":

                    // We need to unseal the Vault instance on every manager node unless a
                    // specific node was requsted via [--neon-node].

                    if (!string.IsNullOrEmpty(nodeName))
                    {
                        Console.WriteLine();

                        // Verify that the instance isn't already unsealed.

                        response = node.SudoCommand($"vault-direct status");

                        if (response.ExitCode == 2)
                        {
                            Console.WriteLine($"[{node.Name}] unsealing...");
                        }
                        else if (response.ExitCode == 0)
                        {
                            Console.WriteLine($"[{node.Name}] is already unsealed");
                            break;
                        }
                        else
                        {
                            Console.WriteLine($"[{node.Name}] unseal failed");
                            Program.Exit(response.ExitCode);
                        }

                        // Note that we're passing the [-reset] option to ensure that 
                        // any keys from previous attempts have been cleared.

                        node.SudoCommand($"vault-direct unseal -reset");

                        foreach (var key in vaultCredentials.UnsealKeys)
                        {
                            response = node.SudoCommand($"vault-direct unseal {key}", RunOptions.Classified);

                            if (response.ExitCode != 0)
                            {
                                Console.WriteLine($"[{node.Name}] unseal failed");
                                Program.Exit(1);
                            }
                        }

                        Console.WriteLine($"[{node.Name}] unsealed");
                    }
                    else
                    {
                        var commandFailed = false;

                        foreach (var manager in cluster.Managers)
                        {
                            Console.WriteLine();

                            // Verify that the instance isn't already unsealed.

                            try
                            {
                                response = manager.SudoCommand($"vault-direct status");
                            }
                            catch (SshOperationTimeoutException)
                            {
                                Console.WriteLine($"[{manager.Name}] ** unavailable **");
                                continue;
                            }

                            if (response.ExitCode == 2)
                            {
                                Console.WriteLine($"[{manager.Name}] unsealing...");
                            }
                            else if (response.ExitCode == 0)
                            {
                                Console.WriteLine($"[{manager.Name}] is already unsealed");
                                continue;
                            }
                            else
                            {
                                Console.WriteLine($"[{manager.Name}] unseal failed");
                                continue;
                            }

                            // Note that we're passing the [-reset] option to ensure that 
                            // any keys from previous attempts have been cleared.

                            manager.SudoCommand($"vault-direct unseal -reset");

                            var failed = false;

                            foreach (var key in vaultCredentials.UnsealKeys)
                            {
                                response = manager.SudoCommand($"vault-direct unseal {key}", RunOptions.Classified);

                                if (response.ExitCode != 0)
                                {
                                    failed        = true;
                                    commandFailed = true;

                                    Console.WriteLine($"[{manager.Name}] unseal failed");
                                }
                            }

                            if (!failed)
                            {
                                Console.WriteLine($"[{manager.Name}] unsealed");
                            }
                        }

                        Program.Exit(commandFailed ? 1 : 0);
                    }
                    break;

                case "write":

                    {
                        // We need handle any [key=@file] arguments specially by including them
                        // in a command bundle as JSON text files.

                        var files         = new List<CommandFile>();
                        var commandString = commandLine.ToString();

                        foreach (var dataArg in commandLine.Arguments.Skip(2))
                        {
                            var fields = dataArg.Split(new char[] { '=' }, 2);

                            if (fields.Length == 2 && fields[1].StartsWith("@"))
                            {
                                var fileName      = fields[1].Substring(1);
                                var localFileName = $"{files.Count}.json";

                                files.Add(
                                    new CommandFile()
                                    {
                                         Path = localFileName,
                                         Text = File.ReadAllText(fileName)
                                    });

                                commandString = commandString.Replace($"@{fileName}", $"@{localFileName}");
                            }
                        }

                        bundle = new CommandBundle($"export VAULT_TOKEN={vaultCredentials.RootToken} && {remoteVaultPath} {commandString}");

                        foreach (var file in files)
                        {
                            bundle.Add(file);
                        }

                        response = node.SudoCommand(bundle, RunOptions.IgnoreRemotePath);

                        Console.WriteLine(response.AllText);
                        Program.Exit(response.ExitCode);
                    }
                    break;

                case "policy-write":

                    // The last command line item is either:
                    //
                    //      * A "-", indicating that the content should come from standard input.
                    //      * A file name prefixed by "@"
                    //      * A string holding JSON or HCL

                    if (commandLine.Items.Length < 2)
                    {
                        response = node.SudoCommand($"export VAULT_TOKEN={vaultCredentials.RootToken} && {remoteVaultPath} {commandLine}", RunOptions.IgnoreRemotePath);

                        Console.WriteLine(response.AllText);
                        Program.Exit(response.ExitCode);
                    }

                    var lastItem   = commandLine.Items.Last();
                    var policyText = string.Empty;

                    if (lastItem == "-")
                    {
                        using (var stdInData = new MemoryStream())
                        {
                            using (var stdInStream = Console.OpenStandardInput())
                            {
                                var buffer = new byte[8192];
                                int cb;

                                while (true)
                                {
                                    cb = stdInStream.Read(buffer, 0, buffer.Length);

                                    if (cb == 0)
                                    {
                                        break;
                                    }

                                    stdInData.Write(buffer, 0, cb);
                                }
                            }

                            policyText = Encoding.UTF8.GetString(stdInData.ToArray());
                        }
                    }
                    else if (lastItem.StartsWith("@"))
                    {
                        policyText = File.ReadAllText(lastItem.Substring(1), Encoding.UTF8);
                    }
                    else
                    {
                        policyText = lastItem;
                    }

                    // We're going to upload a text file holding the policy text and
                    // then run a script piping the policy file into the Vault command passed, 
                    // with the last item replaced by a "-". 

                    bundle = new CommandBundle("./set-vault-policy.sh.sh");

                    var sbScript = new StringBuilder();

                    sbScript.AppendLine($"export VAULT_TOKEN={vaultCredentials.RootToken}");
                    sbScript.Append($"cat policy | {remoteVaultPath}");

                    for (int i = 0; i < commandLine.Items.Length - 1; i++)
                    {
                        sbScript.Append(' ');
                        sbScript.Append(commandLine.Items[i]);
                    }

                    sbScript.AppendLine(" -");

                    bundle.AddFile("set-vault-policy.sh", sbScript.ToString(), isExecutable: true);
                    bundle.AddFile("policy", policyText);

                    response = node.SudoCommand(bundle, RunOptions.IgnoreRemotePath);

                    Console.WriteLine(response.AllText);
                    Program.Exit(response.ExitCode);
                    break;

                default:

                    // We're going to execute the command using the root Vault token.

                    response = node.SudoCommand($"export VAULT_TOKEN={vaultCredentials.RootToken} && {remoteVaultPath} {commandLine}", RunOptions.IgnoreRemotePath);

                    Console.WriteLine(response.AllText);
                    Program.Exit(response.ExitCode);
                    break;
            }
        }
    }
}
