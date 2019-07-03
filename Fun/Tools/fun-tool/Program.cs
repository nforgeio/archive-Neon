//-----------------------------------------------------------------------------
// FILE:	    Program.cs
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

using Neon.Fun;

namespace FunTool
{
    /// <summary>
    /// Miscellaneous Fun/Bowling related tools.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Tool entry point.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args)
        {
            string usage = $@"
Neon Fun Tool: fun-tool [v{Build.Version}]
{Build.Copyright}

usage:

fun-tool COMMAND [arg...]

fun-tool help                   COMMAND
fun-tool parse-usbc-centers     ...
fun-tool parse-balls            COMMAND
fun-tool retouch-balls          ...
";

            try
            {
                ICommand command;

                CommandLine = new CommandLine(args);

                if (CommandLine.Arguments.Length == 0)
                {
                    Console.WriteLine(usage);
                    Program.Exit(0);
                }

                var commands = new List<ICommand>()
                {
                   new ParseBallsCommand(),
                   new ParseUsbcCentersCommand(),
                   new RetouchBallsCommand(),
                };

                if (CommandLine.Arguments[0] == "help")
                {
                    if (CommandLine.Arguments.Length == 1)
                    {
                        Console.WriteLine(usage);
                        Program.Exit(0);
                    }

                    command = commands.SingleOrDefault(c => c.Name == CommandLine.Arguments[1]);

                    if (command == null)
                    {
                        Console.Error.WriteLine($"Invalid command: {CommandLine.Arguments[1]}");
                        Console.Error.WriteLine(usage);
                        Program.Exit(1);
                    }

                    command.Help();
                    Program.Exit(0);
                }

                // Process the common command line options.

                // $todo(jeff.lill): None right now

                // Locate and run the command.

                command = commands.SingleOrDefault(c => c.Name == CommandLine.Arguments[0]);

                if (command == null)
                {
                    Console.Error.WriteLine($"Invalid command: {CommandLine.Arguments[0]}");
                    Program.Exit(1);
                }

                command.Run(CommandLine);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{NeonHelper.ExceptionError(e)}");
                Console.Error.WriteLine(string.Empty);
                Program.Exit(1);
            }

            Program.Exit(0);
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
        /// Returns the <see cref="CommandLine"/>.
        /// </summary>
        public static CommandLine CommandLine { get; private set; }
    }
}
