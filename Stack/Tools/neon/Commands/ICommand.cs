//-----------------------------------------------------------------------------
// FILE:	    ICommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Cluster;
using Neon.Stack.Common;

// $todo(jeff.lill):
//
// Support the detection of unknown options.

namespace NeonCluster
{
    /// <summary>
    /// Implements a command.
    /// </summary>
    [ContractClass(typeof(ICommandContract))]
    public interface ICommand
    {
        /// <summary>
        /// Returns the command words.
        /// </summary>
        /// <remarks>
        /// This property is used to map the command line arguments to a command
        /// implemention.  In the simple case, this will be a single word.  You 
        /// may also specify multiple words.
        /// </remarks>
        string[] Words { get; }

        /// <summary>
        /// Returns the array of extended command line options beyond the common options
        /// supported by the command or an empty array if none.  The option names must
        /// include the leading dash(es).
        /// </summary>
        string[] ExtendedOptions { get; }

        /// <summary>
        /// Returns <c>true</c> if the command requires the server SSH credentials.
        /// </summary>
        bool NeedsSshCredentials { get; }

        /// <summary>
        /// Returns <c>true</c> for commands that pass their arguments and options on
        /// to a a cluster node rather than being processed locally.  The options will 
        /// not by verified using the standard mechanisms. 
        /// </summary>
        bool IsPassThru { get; }

        /// <summary>
        /// Displays help for the command.
        /// </summary>
        void Help();

        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <param name="commandLine">The command line.</param>
        void Run(CommandLine commandLine);
    }

    [ContractClassFor(typeof(ICommand))]
    internal abstract class ICommandContract : ICommand
    {
        public string[] Words { get; }
        public string[] ExtendedOptions { get; }
        public bool NeedsSshCredentials { get; }
        public bool IsPassThru { get; }

        public void Help()
        {
        }

        public void Run(CommandLine commandLine)
        {
            Covenant.Requires<ArgumentNullException>(commandLine != null);
        }
    }
}
