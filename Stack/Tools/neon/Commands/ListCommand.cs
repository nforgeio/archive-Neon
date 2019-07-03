//-----------------------------------------------------------------------------
// FILE:	    ListCommand.cs
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
    /// Implements the <b>list</b> command.
    /// </summary>
    public class ListCommand : ICommand
    {
        private const string usage = @"
Lists the clusters with administrative information saved on the local
computer as well as indicating whether the tool is currently logged
into one of them.

USAGE:

    neon list

";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "list" }; }
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
            var current  = Program.ClusterSecrets?.Name;
            var clusters = new List<string>();

            foreach (var file in Directory.EnumerateFiles(Program.ClusterSecretsFolder, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var clusterInfo = NeonHelper.JsonDeserialize<ClusterSecrets>(File.ReadAllText(file));

                    clusters.Add(clusterInfo.Name.ToLowerInvariant());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"*** ERROR: Cannot read [{file}].  Details: {NeonHelper.ExceptionError(e)}");
                    Program.Exit(1);
                }
            }

            if (clusters.Count == 0)
            {
                Console.WriteLine("*** No known clusters");
            }
            else
            {
                foreach (var cluster in clusters.OrderBy(c => c))
                {

                    if (string.Equals(current, cluster, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.Write(" --> ");
                    }
                    else
                    {
                        Console.Write("     ");
                    }

                    Console.WriteLine(cluster);
                }
            }
        }
    }
}
