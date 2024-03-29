﻿//-----------------------------------------------------------------------------
// FILE:	    Program.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Build.Framework;

using Neon.Stack;
using Neon.Stack.Common;

namespace EntityGen
{
    /// <summary>
    /// Wraps the <see cref="CodeGenerator"/> build task into a command line tool.
    /// </summary>
    public class Program : IBuildEngine
    {
        //---------------------------------------------------------------------
        // Static members

        private static bool quiet = false;

        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            var commandLine = new CommandLine(args);
            var usage =
$@"
Entity Code Generator v{Build.Version}
{Build.Copyright}

usage: entity-gen SOURCES OUTPUT 
                  [--include=NAMES]
                  [--register=CLASS] 
                  [--quiet]

    SOURCE  - source assembly paths separated by semi-colons
    OUTPUT  - path to the source code output file (*.cs)
    NAMES   - optional list of fully qualified entity interface
              names or wildcarded namespaces like: MyNamespace.*
              separated by semi-colons.
    CLASS   - optional fully qualified name for the generated
              entity registration class.

    --quiet - Disables informational messages.
";

            if (commandLine.HasHelpOption || commandLine.Arguments.Count() == 0)
            {
                Console.WriteLine(usage);
                Program.Exit(0);
            }

            if (commandLine.Arguments.Count() != 2)
            {
                Console.Error.WriteLine(usage);
                Program.Exit(1);
            }

            if (commandLine.HasOption("--quiet"))
            {
                quiet = true;
            }

            try
            {
                new Program(commandLine.Arguments[0], commandLine.Arguments[1], 
                            commandLine.GetOption("--include"), commandLine.GetOption("--register"));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(NeonHelper.ExceptionError(e));
                Program.Exit(1);
            }

            Program.Exit(0);
        }

        /// <summary>
        /// Terminates the program.
        /// </summary>
        /// <param name="exitCode">The exit code.</param>
        private static void Exit(int exitCode)
        {
            Environment.Exit(exitCode);
        }

        //---------------------------------------------------------------------
        // Instance members

        private bool errorDetected;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Program(string sources, string output, string include, string registerClass)
        {
            string[]    sourcesArray;
            string[]    includeArray = null;

            sourcesArray = sources.Split(';');

            for (int i = 0; i < sourcesArray.Length; i++)
            {
                sourcesArray[i] = sourcesArray[i].Trim();
            }

            if (!string.IsNullOrEmpty(include))
            {
                includeArray = include.Split(';');

                for (int i = 0; i < includeArray.Length; i++)
                {
                    includeArray[i] = includeArray[i].Trim();
                }
            }

            var buildTask = new CodeGenerator()
            {
                BuildEngine = this,
                Sources     = sourcesArray,
                Include     = includeArray,
                Register    = registerClass,
                Output      = output
            };

            if (buildTask.Execute() && !errorDetected)
            {
                Program.Exit(0);
            }
            else
            {
                Program.Exit(1);
            }
        }

        //---------------------------------------------------------------------
        // IBuildEngine implementation

#pragma warning disable 1591

        public int ColumnNumberOfTaskNode
        {
            get { return 0; }
        }

        public bool ContinueOnError
        {
            get { return true; }
        }

        public int LineNumberOfTaskNode
        {
            get { return 0; }
        }

        public string ProjectFileOfTaskNode
        {
            get { return string.Empty; }
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs args)
        {
        }

        public void LogErrorEvent(BuildErrorEventArgs args)
        {
            errorDetected = true;
            Console.Error.WriteLine($"error [entity-gen]: {args.Message}");
        }

        public void LogMessageEvent(BuildMessageEventArgs args)
        {
            if (!quiet)
            {
                Console.WriteLine($"information [entity-gen]: {args.Message}");
            }
        }

        public void LogWarningEvent(BuildWarningEventArgs args)
        {
            Console.Error.WriteLine($"warning [entity-gen]: {args.Message}");
        }
    }
}
