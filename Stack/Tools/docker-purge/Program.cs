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

using Neon.Stack;
using Neon.Stack.Common;

namespace DockerPurge
{
    /// <summary>
    /// Entry point for the <b>docker-purge</b> utility.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Entry point for the <b>docker-purge</b> utility.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args)
        {
            var usage = $@"
Docker Purge Utility: docker-purge [v{Build.Version}]
{Build.Copyright}

USAGE:

    docker-purge help
    docker-purge all        - Purges all local images

";

            var commandLine = new CommandLine(args);

            if (commandLine.Arguments.Length == 0)
            {
                Console.WriteLine(usage);
                Program.Exit(1);
            }

            try
            {
                switch (commandLine.Arguments[0].ToLowerInvariant())
                {
                    case "help":

                        Console.WriteLine(usage);
                        Program.Exit(0);
                        break;

                    case "all":

                        Console.WriteLine();

                        // List all images.  Docker will return one image ID per line.  We're then going to
                        // attempt to remove all images.  Note that we'll see conflict errors when we try
                        // to delete an image that's referenced by another.  We'll ignore these errors and
                        // continue deleting what we can and then try deleting the remaining images, until
                        // they've all been deleted.

                        var count      = 0;
                        var iterations = 1;

                        while (true)
                        {
                            var allDeleted = true;
                            var result     = HandleError(NeonHelper.ExecuteCaptureStreams("docker", "images -aq"));

                            foreach (var imageId in new StringReader(result.StandardOutput).Lines())
                            {
                                var id = imageId.Trim();

                                if (string.IsNullOrEmpty(id))
                                {
                                    continue;
                                }

                                Console.WriteLine($"docker rmi -f {id}");

                                result = NeonHelper.ExecuteCaptureStreams("docker", $"rmi -f {id}");

                                if (result.ExitCode != 0)
                                {
                                    if (result.StandardError.Contains("No such image"))
                                    {
                                        // Ignore these.

                                        continue;
                                    }
                                    else if (result.StandardError.Contains("conflict"))
                                    {
                                        allDeleted = false;
                                        continue;
                                    }
                                    else
                                    {
                                        HandleError(result);
                                    }
                                }

                                count++;
                            }

                            if (allDeleted)
                            {
                                break;
                            }
                            else
                            {
                                iterations++;
                            }
                        }

                        Console.WriteLine($"[{count}] Docker images removed with [{iterations}] iterations.");
                        break;

                    default:

                        Console.WriteLine(usage);
                        Program.Exit(1);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(NeonHelper.ExceptionError(e));
                Program.Exit(1);
            }
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
        /// Checks a <see cref="ExecuteResult"/> for an error and if there is one,
        /// writes the information to the console and exits the program.
        /// </summary>
        /// <param name="result">The execution results.</param>
        /// <returns>The result when there are no errors.</returns>
        private static ExecuteResult HandleError(ExecuteResult result)
        {
            if (result.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(result.StandardOutput))
                {
                    Console.WriteLine($"*** ERROR: {result.StandardOutput}");
                }

                if (!string.IsNullOrWhiteSpace(result.StandardError))
                {
                    Console.WriteLine($"*** ERROR: {result.StandardError}");
                }

                Program.Exit(result.ExitCode);
            }

            return result;
        }
    }
}
