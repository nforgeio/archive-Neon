﻿//-----------------------------------------------------------------------------
// FILE:	    UploadCommand.cs
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
using Neon.Stack.IO;

namespace NeonCluster
{
    /// <summary>
    /// Implements the <b>upload</b> command.
    /// </summary>
    public class UploadCommand : ICommand
    {
        private const string usage = @"
Uploads a file to one or more cluster hosts.

USAGE:

    neon upload [OPTIONS] SOURCE TARGET [NODE...]

ARGUMENTS:

    SOURCE              - Path to the source file on the local workstation.
    TARGET              - Path to the destination file on the nodes.
    NODE                - Zero are more target node names, an asterisk
                          to upload to all nodes.  Uploads to the first
                          manager if no node is specified.
OPTIONS:

    --text              - Converts TABs to spaces and line endings to Linux
    --chmod=PERMISSIONS - Linux target file permissions

NOTES:

    * Any required destination folders will be created if missing.
    * TARGET must be the full destination path including the file name.
    * Files will be uploaded with 440 permissions if [--chmod] is not present.
";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "upload" }; }
        }

        /// <inheritdoc/>
        public string[] ExtendedOptions
        {
            get { return new string[] { "--text", "--chmod" }; }
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

            // Process the command options.

            var isText      = false;
            var permissions = new LinuxPermissions("440");

            if (commandLine.GetOption("--text") != null)
            {
                isText = true;
            }

            var chmod = commandLine.GetOption("--chmod");

            if (!string.IsNullOrEmpty(chmod))
            {
                if (!LinuxPermissions.TryParse(chmod, out permissions))
                {
                    Console.WriteLine("*** Error: Invalid Linux file permissions.");
                    Program.Exit(1);
                }
            }

            // Process the command arguments.

            List<NodeDefinition>    nodeDefinitions = new List<NodeDefinition>();
            string                  source;
            string                  target;

            if (commandLine.Arguments.Length < 1)
            {
                Console.WriteLine("*** Error: SOURCE file was not specified.");
                Program.Exit(1);
            }

            source = commandLine.Arguments[0];

            if (commandLine.Arguments.Length < 2)
            {
                Console.WriteLine("*** Error: TARGET file was not specified.");
                Program.Exit(1);
            }

            target = commandLine.Arguments[1];

            if (commandLine.Arguments.Length == 2)
            {
                nodeDefinitions.Add(clusterSecrets.Definition.Managers.First());
            }
            else if (commandLine.Arguments.Length == 3 && commandLine.Arguments[2] == "*")
            {
                foreach (var manager in clusterSecrets.Definition.SortedManagers)
                {
                    nodeDefinitions.Add(manager);
                }

                foreach (var worker in clusterSecrets.Definition.SortedWorkers)
                {
                    nodeDefinitions.Add(worker);
                }
            }
            else
            {
                foreach (var name in commandLine.Shift(2).Arguments)
                {
                    NodeDefinition node;

                    if (!clusterSecrets.Definition.NodeDefinitions.TryGetValue(name, out node))
                    {
                        Console.WriteLine($"*** Error: Node [{name}] is not present in the cluster.");
                        Program.Exit(1);
                    }

                    nodeDefinitions.Add(node);
                }
            }

            if (!File.Exists(source))
            {
                Console.WriteLine($"*** Error: File [{source}] does not exist.");
                Program.Exit(1);
            }

            // Perform the upload.

            var cluster   = new ClusterProxy(Program.ClusterSecrets, Program.CreateNodeProxy<NodeDefinition>);
            var operation = new SetupController(Program.SafeCommandLine, cluster.Nodes.Where(n => nodeDefinitions.Exists(nd => nd.Name == n.Name)));

            operation.AddWaitUntilOnlineStep();
            operation.AddStep("upload",
                node =>
                {
                    node.Status = "uploading";

                    if (isText)
                    {
                        node.UploadText(target, File.ReadAllText(source, Encoding.UTF8), tabStop: 4, outputEncoding: Encoding.UTF8);
                    }
                    else
                    {
                        using (var stream = new FileStream(source, FileMode.Open, FileAccess.Read))
                        {
                            node.Upload(target, stream);
                        }
                    }

                    node.Status = $"set permissions: {permissions}";
                    node.SudoCommand("chmod", permissions, target);
                });

            if (!operation.Run())
            {
                Console.Error.WriteLine("*** ERROR: The upload to one or more nodes failed.");
                Program.Exit(1);
            }
        }
    }
}
