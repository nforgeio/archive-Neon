//-----------------------------------------------------------------------------
// FILE:	    RetouchBallsCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Stack.Common;
using Neon.Stack.Net;

namespace FunTool
{
    /// <summary>
    /// Parses bowling ball information from information captured from manufacturer
    /// websites via Fiddler.
    /// </summary>
    public class RetouchBallsCommand : ICommand
    {
        private const string usage = @"
fun-tool retouch-balls SOURCE-FOLDER TARGET-FOLDER

    SOURCE-FOLDER   - Raw source image folder
    TARGET-FOLDER   - Retouched image folder

Launches PAINT.NET for images one-by-one from the source folder
so they can be manually edited and saved as PNG formatted images
and then copied to the target folder.

Raw ball images are need to be manually:

    * Cropped for size
    * Add transparency
    * Saved as PNG

This command does the following for each image in the source folder:

    1. If a file with the same name and the the PNG
       extension exists in the target folder, then
       the current file will be skipped.

    2. The file is copied to a temporary folder.

    3. The file is opened in PAINT.NET.

    4. The user needs to edit the image and saved it
       with the same name as a PNG.

    5. The edited PNG file is copied to the target folder.

    6. The temporary folder is cleared and the next source
       file is processed (goto step #1).
";
        /// <inheritdoc/>
        public string Name
        {
            get { return "retouch-balls"; }
        }

        /// <inheritdoc/>
        public bool NeedsCredentials
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
            if (commandLine.Arguments.Count() != 3)
            {
                Console.WriteLine(usage);
                Program.Exit(1);
            }

            var sourceFolder = commandLine.Arguments[1];
            var targetFolder = commandLine.Arguments[2];
            var tempFolder   = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var paintPath    = @"C:\Program Files\paint.net\PaintDotNet.exe";

            if (!File.Exists(paintPath))
            {
                Console.WriteLine($"PAINT.NET is not installed at [{paintPath}].");
                Program.Exit(1);
                return;
            }

            Directory.CreateDirectory(targetFolder);
            Directory.CreateDirectory(tempFolder);

            try
            {
                foreach (var sourceImagePath in Directory.EnumerateFiles(sourceFolder))
                {
                    var tempImagePath    = Path.Combine(tempFolder, Path.GetFileName(sourceImagePath));
                    var tempPngImagePath = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(sourceImagePath) + ".png");
                    var targetImagePath  = Path.Combine(targetFolder, Path.GetFileName(tempPngImagePath));

                tryAgain:

                    if (File.Exists(targetImagePath))
                    {
                        continue;
                    }

                    foreach (var file in Directory.EnumerateFiles(tempFolder))
                    {
                        File.Delete(file);
                    }

                    File.Copy(sourceImagePath, tempImagePath);

                    var paintProcess = Process.Start(paintPath, $"\"{tempImagePath}\"");

                    paintProcess.WaitForExit();

                    if (File.Exists(tempPngImagePath))
                    {
                        File.Copy(tempPngImagePath, targetImagePath);
                    }
                    else
                    {
                        goto tryAgain;
                    }
                }
            }
            finally
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }
}