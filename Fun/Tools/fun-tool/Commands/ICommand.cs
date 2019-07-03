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

using Neon.Stack.Common;

namespace FunTool
{
    /// <summary>
    /// Implements a command.
    /// </summary>
    [ContractClass(typeof(ICommandContract))]
    public interface ICommand
    {
        /// <summary>
        /// Returns the command name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns <c>true</c> if the command requires the server admin credentials.
        /// </summary>
        bool NeedsCredentials { get; }

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
        public string Name { get; }
        public bool NeedsCredentials { get; }

        public void Help()
        {
        }

        public void Run(CommandLine commandLine)
        {
            Covenant.Requires<ArgumentNullException>(commandLine != null);
        }
    }
}
