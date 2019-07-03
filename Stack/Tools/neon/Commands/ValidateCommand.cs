//-----------------------------------------------------------------------------
// FILE:	    ValidateCommand.cs
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
    /// Implements the <b>validate</b> command.
    /// </summary>
    public class ValidateCommand : ICommand
    {
        private const string usage = @"
Validates a cluster definition file.

USAGE:

    neon validate CLUSTER-DEF

ARGUMENTS:

    CLUSTER-DEF     - Path to the cluster definition file.
";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "validate" }; }
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
            if (commandLine.Arguments.Length < 1)
            {
                Console.Error.WriteLine("*** ERROR: CLUSTER-DEF is required.");
                Program.Exit(1);
            }

            // Parse and validate the cluster definition.

            ClusterDefinition.FromFile(commandLine.Arguments[0]);

            Console.WriteLine("");
            Console.WriteLine("*** The cluster definition is OK.");
        }
    }
}
