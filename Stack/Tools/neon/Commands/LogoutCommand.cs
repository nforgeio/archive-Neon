//-----------------------------------------------------------------------------
// FILE:	    LogoutCommand.cs
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
    /// Implements the <b>login</b> command.
    /// </summary>
    public class LogoutCommand : ICommand
    {
        private const string usage = @"
Clears any persisted cluster SSH credentials from a previous [login] command.

USAGE:

    neon logout 
";

        /// <inheritdoc/>
        public string[] Words
        {
            get { return new string[] { "logout" }; }
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
            if (File.Exists(Program.CurrentClusterPath))
            {
                File.Delete(Program.CurrentClusterPath);
            }

            Console.WriteLine("*** You are logged out.");
            Console.WriteLine("");
        }
    }
}
