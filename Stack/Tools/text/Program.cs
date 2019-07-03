﻿//-----------------------------------------------------------------------------
// FILE:	    Program.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Stack;
using Neon.Stack.Common;

namespace Text
{
    /// <summary>
    /// Text file manipulation utility.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <para>
    /// Usage:
    /// </para>
    /// <code language="none">
    /// text replace -VAR=VALUE... FILE
    /// </code>
    /// </remarks>
    public static class Program
    {
        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="args">The list of files to be processed with optional wildcards.</param>
        public static void Main(string[] args)
        {
            var commandLine = new CommandLine(args);

            if (commandLine.Arguments.Length == 0 ||
                commandLine.HasHelpOption ||
                commandLine.Arguments[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
                commandLine.Arguments[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(
@"
Neon Text File Utility: text [v{Build.Version}]
{Build.Copyright}

usage: text replace     -TEXT=VALUE... FILE
       text replace-var -VAR=VALUE... FILE
       text help   
    
    --help              Print usage

");
                Program.Exit(0);
            }

            Console.WriteLine(string.Empty);

            try
            {
                switch (commandLine.Arguments[0].ToLower())
                {
                    case "replace":

                        Replace(commandLine);
                        break;

                    case "replace-var":

                        ReplaceVar(commandLine);
                        break;

                    default:

                        PrintUsage();
                        Program.Exit(1);
                        break;
                }

                Program.Exit(0);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{NeonHelper.ExceptionError(e)}");
                Console.Error.WriteLine(string.Empty);

                Program.Exit(1);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine(
$@"
Neon Text File Utility: text [v{Build.Version}]
{Build.Copyright}

usage: text replace -VAR=VALUE... FILE
       text help   
    
    --help              Print usage

");
        }

        /// <summary>
        /// Exits the program returning the specified process exit code.
        /// </summary>
        /// <param name="exitCode">The exit code.</param>
        public static void Exit(int exitCode)
        {
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Performs the variable substitutions for variable references like: <b>${variable-name}</b>.
        /// </summary>
        /// <param name="commandLine">The command line.</param>
        private static void ReplaceVar(CommandLine commandLine)
        {
            var sb = new StringBuilder();

            using (var reader = new StreamReader(commandLine.Arguments[1]))
            {
                foreach (var line in reader.Lines())
                {
                    var temp = line;

                    foreach (var variable in commandLine.Options)
                    {
                        temp = temp.Replace($"${{{variable.Key.Substring(1)}}}", variable.Value);
                    }

                    sb.AppendLine(temp);
                }
            }

            using (var writer = new StreamWriter(commandLine.Arguments[1]))
            {
                writer.Write(sb);
            }
        }

        /// <summary>
        /// Performs the text replacement operations specified in a command line.
        /// </summary>
        /// <param name="commandLine">The command line.</param>
        private static void Replace(CommandLine commandLine)
        {
            var sb = new StringBuilder();

            using (var reader = new StreamReader(commandLine.Arguments[1]))
            {
                foreach (var line in reader.Lines())
                {
                    var temp = line;

                    foreach (var variable in commandLine.Options)
                    {
                        temp = temp.Replace(variable.Key.Substring(1), variable.Value);
                    }

                    sb.AppendLine(temp);
                }
            }

            using (var writer = new StreamWriter(commandLine.Arguments[1]))
            {
                writer.Write(sb);
            }
        }
    }
}
