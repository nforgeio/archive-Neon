//-----------------------------------------------------------------------------
// FILE:	    ConsulCommand.cs
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
    /// Implements the <b>consul</b> commands.
    /// </summary>
    public class ConsulCommand : ICommand
    {
        private const string usage = @"
Runs a HashiCorp Consul command on the cluster.  All command line arguments
and options as well are passed through to the Consul CLI.

USAGE:

    neon consul --help                  - Prints Consul (and this) help
    neon consul [OPTIONS] [ARGS...]     - Invokes a Consul command

ARGS: The standard HashCorp Consul command arguments and options.

OPTIONS :

    --neon-node=NODE    - Specifies the target node.  The Consul command will
                          be executed on a manager node when this isn't specified.

NOTE: [neon consul watch] command is not supported.

NOTE: [neon consul snapshot ...] commands reads or writes files on the remote
      cluster host, not the local workstation and you'll need to specify
      a fully qualified path.
";
        private ClusterProxy cluster;

        private const string remoteConsulPath = "/usr/local/bin/consul";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "consul" }; }
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

            cluster = new ClusterProxy(Program.ClusterSecrets, Program.CreateNodeProxy<NodeDefinition>);

            // Strip out the first item of the command line (note that we're
            // not using Shift() because that will reorder any options).

            commandLine = new CommandLine(commandLine.Items.Skip(1).ToArray());

            // Determine which node we're going to target.

            NodeProxy<NodeDefinition>   node;
            var                         nodeName = commandLine.GetOption("--neon-node", null);

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

            // We're going to print help from Consul first followed by
            // help for the [neon.exe] command.

            if (commandLine.HasHelpOption)
            {
                var response = node.SudoCommand($"{remoteConsulPath} {commandLine}", RunOptions.IgnoreRemotePath);

                if (commandLine.Arguments.Length == 0)
                {
                    Console.WriteLine(new string('-', 80));
                    Console.WriteLine("NEON.EXE Help:");
                    Console.WriteLine(usage);

                    Console.WriteLine(new string('-', 80));
                    Console.WriteLine("HashiCorp Consul Help:");
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
                case "watch":

                    Console.Error.WriteLine("*** ERROR: [neon consul watch] is not supported.");
                    Program.Exit(1);
                    break;

                case "monitor":

                    // We'll just relay the output we receive from the remote command
                    // until the user kills this process.

                    using (var shell = node.CreateSudoShell())
                    {
                        shell.WriteLine($"sudo {remoteConsulPath} {commandLine}");

                        while (true)
                        {
                            var line = shell.ReadLine();

                            if (line == null)
                            {
                                break; // Just being defensive
                            }

                            Console.WriteLine(line);
                        }
                    }
                    break;

                default:

                    if (commandLine.Items.LastOrDefault() == "-")
                    {
                        // This is the special case where we need to pipe the standard input sent
                        // to this command on to Consul on the remote machine.  We're going to use
                        // a CommandBundle by uploading the standard input data as a file.

                        var bundle = new CommandBundle($"cat stdin.dat | {remoteConsulPath} {commandLine}");

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

                            bundle.AddFile("stdin.dat", stdInData.ToArray());
                        }

                        var response = node.SudoCommand(bundle, RunOptions.IgnoreRemotePath);

                        Console.WriteLine(response.AllText);
                        Program.Exit(response.ExitCode);
                    }
                    else if (commandLine.StartsWithArgs("kv", "put") && commandLine.Arguments.Length == 4 && commandLine.Arguments[3].StartsWith("@"))
                    {
                        // We're going to special case PUT when saving a file
                        // whose name is prefixed with "@".

                        var fileName = commandLine.Arguments[3].Substring(1);
                        var bundle = new CommandBundle($"{remoteConsulPath} {commandLine}");

                        bundle.AddFile(fileName, File.ReadAllBytes(fileName));

                        var response = node.SudoCommand(bundle, RunOptions.IgnoreRemotePath);

                        Console.Write(response.AllText);
                        Program.Exit(response.ExitCode);
                    }
                    else
                    {
                        // All we need to do is to execute the command remotely.  We're going to special case
                        // the [consul kv get ...] command to process the result as binary.

                        CommandResponse response;

                        if (commandLine.ToString().StartsWith("kv get"))
                        {
                            response = node.SudoCommand($"{remoteConsulPath} {commandLine}", RunOptions.IgnoreRemotePath | RunOptions.BinaryOutput);

                            using (var remoteStandardOutput = response.OpenOutputBinaryStream())
                            {
                                if (response.ExitCode != 0)
                                {
                                    // Looks like Consul writes its errors to standard output, so 
                                    // I'm going to open a text reader and write those lines
                                    // to standard error.

                                    using (var reader = new StreamReader(remoteStandardOutput))
                                    {
                                        foreach (var line in reader.Lines())
                                        {
                                            Console.Error.WriteLine(line);
                                        }
                                    }
                                }
                                else
                                {
                                    // Write the remote binary output to standard output.

                                    using (var output = Console.OpenStandardOutput())
                                    {
                                        var buffer = new byte[8192];
                                        int cb;

                                        while (true)
                                        {
                                            cb = remoteStandardOutput.Read(buffer, 0, buffer.Length);

                                            if (cb == 0)
                                            {
                                                break;
                                            }

                                            output.Write(buffer, 0, cb);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            response = node.SudoCommand($"{remoteConsulPath} {commandLine}", RunOptions.IgnoreRemotePath);

                            Console.WriteLine(response.AllText);
                        }

                        Program.Exit(response.ExitCode);
                    }
                    break;
            }
        }
    }
}
