//-----------------------------------------------------------------------------
// FILE:	    ParseBallsCommand.cs
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
    public class ParseBallsCommand : ICommand
    {
        //---------------------------------------------------------------------
        // Private types

        private class BallInfo
        {
            public const string HeaderRow = "Manufacturer\tProduct Line\tModel\tRelease Date\tSKU\tRetired\tCoverstock\tFinish\tColor\tFragrance\tWeights\tImage";

            public string Manufacturer { get; set; } = string.Empty;
            public string ProductLine { get; set; }  = string.Empty;
            public string Model { get; set; }        = string.Empty;
            public string ReleaseDate { get; set; }  = string.Empty;
            public string Sku { get; set; }          = string.Empty;
            public string Retired { get; set; }      = string.Empty;
            public string Coverstock { get; set; }   = string.Empty;
            public string Finish { get; set; }       = string.Empty;
            public string Color { get; set; }        = string.Empty;
            public string Fragrance { get; set; }    = string.Empty;
            public string Weights { get; set; }      = string.Empty;
            public string Image { get; set; }        = string.Empty;
            public string ImageUri { get; set; }

            public void WriteCsv(TextWriter writer)
            {
                writer.Write($"{Manufacturer}\t");
                writer.Write($"{ProductLine}\t");
                writer.Write($"{Model}\t");
                writer.Write($"{ReleaseDate}\t");
                writer.Write($"{Sku}\t");
                writer.Write($"{Retired}\t");
                writer.Write($"{Coverstock}\t");
                writer.Write($"{Finish}\t");
                writer.Write($"{Color}\t");
                writer.Write($"{Fragrance}\t");
                writer.Write($"{Weights}\t");
                writer.WriteLine($"{Image}");
            }
        }

        //---------------------------------------------------------------------
        // Implementation

        private const string usage = @"
------------------------------------------------
fun-tool parse-balls storm-classic INPUT OUTPUT

    INPUT       - Path to the input file.
    OUTPUT      - Path to the output folder.

Parses the Fiddler captured Storm Classic ball page responses
and writes a TSV file and ball images to a folder.

------------------------------------------------
fun-tool parse-balls track-classic INPUT OUTPUT

    INPUT       - Path to the input file.
    OUTPUT      - Path to the output folder.

Parses the Fiddler captured Storm Retired ball page responses
and writes a TSV file and ball images to a folder.
";
        /// <inheritdoc/>
        public string Name
        {
            get { return "parse-balls"; }
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
            if (commandLine.Arguments.Count() != 4)
            {
                Console.WriteLine(usage);
                Program.Exit(1);
            }

            var command    = commandLine.Arguments[1];
            var inputPath  = commandLine.Arguments[2];
            var outputPath = commandLine.Arguments[3];

            switch (command.ToLower())
            {
                case "storm-classic":

                    ParseStormClassic(inputPath, outputPath);
                    break;

                case "track-classic":

                    ParseTrackClassic(inputPath, outputPath);
                    break;

                default:

                    Console.WriteLine(usage);
                    Program.Exit(1);
                    break;
            }

            Console.WriteLine("*** DONE ***");
        }

        /// <summary>
        /// Parses Fiddler dumps of Storm classic ball information.
        /// </summary>
        /// <param name="inputPath">Path the the Fiddler dump text file.</param>
        /// <param name="outputFolder">Path to the output folder.</param>
        private void ParseStormClassic(string inputPath, string outputFolder)
        {
            // $hack(jeff.lill): 
            //
            // Hardcoded to the http://stormbowling.com/products/balls/classic page format
            // as captured by Fiddler.

            // Split the input file text up into the HTTP responses for each ball.

            var responses   = new List<string>();
            var captureText = File.ReadAllText(inputPath);
            int startPos    = 0;
            int endPos      = 0;

            while (true)
            {
                startPos = captureText.IndexOf("\r\nHTTP/1.1 200 OK", endPos);

                if (startPos == -1)
                {
                    break;
                }

                endPos = captureText.IndexOf("\r\nGET http://stormbowling.com", startPos);

                if (endPos == -1)
                {
                    responses.Add(captureText.Substring(startPos));
                    break;
                }
                else
                {
                    responses.Add(captureText.Substring(startPos, endPos - startPos));
                }
            }

            // Parse the responses to extract the ball information.

            var balls = new List<BallInfo>();

            foreach (var response in responses)
            {
                var ball = new BallInfo()
                {
                    Manufacturer = "Storm",
                    Retired      = "X"
                };

                balls.Add(ball);

                // Extract the model

                startPos = response.IndexOf("<div id=\"maincol\">");
                startPos = response.IndexOf("<h2>", startPos);
                endPos   = response.IndexOf("</h2>", startPos);

                ball.Model = CleanField(response.Substring(startPos, endPos - startPos));

                // Extract the image URL.

                const string imagePrefix = "<img src=\"/img/balls/";

                startPos  = response.IndexOf(imagePrefix, endPos);
                startPos += imagePrefix.Length;
                endPos    = response.IndexOf("\"", startPos);

                var imageUriFragment = response.Substring(startPos, endPos - startPos);

                ball.ImageUri = $"https://stormbowling.com/img/balls/{imageUriFragment}";

                // The ball image file name is the model name with any embedded forward or back 
                // slashes replaced by dashes and appending the downloaded image's file extension.
                // 
                // NOTE: We need to convert some special characters so ZIP will work.

                ball.Image = ball.Model.Replace("/", "-").Replace("\\", "-");
                ball.Image = "storm." + ball.Image + Path.GetExtension(ball.ImageUri);
                ball.Image = ball.Image.Replace("’", "'");
                ball.Image = ball.Image.Replace("ñ", "n");

                // Extract Coverstock

                const string coverstockPrefix = "Coverstock</span>:</dt><dd>";

                startPos = response.IndexOf(coverstockPrefix);

                if (startPos != -1)
                {
                    startPos += coverstockPrefix.Length;
                    endPos    = response.IndexOf("</dd>", startPos);

                    ball.Coverstock = CleanField(response.Substring(startPos, endPos - startPos));
                }

                // Extract Finish

                const string finishPrefix = "Factory Finish:</dt><dd>";

                startPos = response.IndexOf(finishPrefix);

                if (startPos != -1)
                {
                    startPos += finishPrefix.Length;
                    endPos    = response.IndexOf("</dd>", startPos);

                    ball.Finish = CleanField(response.Substring(startPos, endPos - startPos));
                }

                // Extract Color

                const string colorPrefix = "Ball Color:</dt><dd>";

                startPos  = response.IndexOf(colorPrefix);

                if (startPos != -1)
                {
                    startPos += colorPrefix.Length;
                    endPos = response.IndexOf("</dd>", startPos);

                    ball.Color = CleanField(response.Substring(startPos, endPos - startPos));
                }

                // Extract Fragrance

                const string fragrancePrefix = "Fragrance:</dt><dd>";

                startPos = response.IndexOf(fragrancePrefix);

                if (startPos != -1)
                {
                    startPos += fragrancePrefix.Length;
                    endPos    = response.IndexOf("</dd>", startPos);

                    ball.Fragrance = CleanField(response.Substring(startPos, endPos - startPos));
                }

                // Extract Weights

                const string weightsPrefix = "Weights:</dt><dd>";

                startPos = response.IndexOf(weightsPrefix);

                if (startPos != -1)
                {
                    startPos += weightsPrefix.Length;
                    endPos    = response.IndexOf("</dd>", startPos);

                    ball.Weights = CleanField(response.Substring(startPos, endPos - startPos));

                    switch (ball.Weights)
                    {
                        case "10-16 lbs.":

                            ball.Weights = "10,11,12,13,14,15,16";
                            break;

                        case "12-16 lbs.":

                            ball.Weights = "12,13,14,15,16";
                            break;

                        case "14-16 lbs.":

                            ball.Weights = "14,15,16";
                            break;

                        case "15-16 lbs.":

                            ball.Weights = "15,16";
                            break;

                        case "6-16 lbs.":

                            ball.Weights = "6,7,8,9,10,11,12,13,14,15,16";
                            break;

                        case "6. 8. 10-16 lbs.":
                        case "6, 8, 10-16 lbs.":

                            ball.Weights = "6,8,10,11,12,13,14,15,16";
                            break;
                    }
                }

                // Extract SKU

                const string skuPrefix = "SKU:</dt><dd>";

                startPos = response.IndexOf(skuPrefix);

                if (startPos != -1)
                {
                    startPos += skuPrefix.Length;
                    endPos    = response.IndexOf("</dd>", startPos);

                    ball.Sku = CleanField(response.Substring(startPos, endPos - startPos));
                }

                // Extract Release Data

                const string datePrefix = "Released:</dt> <dd class=\"released\"><em>";

                startPos = response.IndexOf(datePrefix);

                if (startPos != -1)
                {
                    startPos += datePrefix.Length;
                    endPos    = response.IndexOf("</em>", startPos);

                    ball.ReleaseDate = CleanField(response.Substring(startPos, endPos - startPos));
                }
            }

            // Write the TSV file.

            using (var output = new StreamWriter(new FileStream(Path.Combine(outputFolder, "Capture.tsv"), FileMode.Create), Encoding.UTF8))
            {
                output.WriteLine(BallInfo.HeaderRow);

                foreach (var ball in balls)
                {
                    ball.WriteCsv(output);
                }
            }

            // Download the ball images.

            Directory.CreateDirectory(Path.Combine(outputFolder, "Images"));

            using (var client = new HttpClient())
            {
                foreach (var ball in balls)
                {
                    Console.WriteLine($"Downloading: {ball.Image}");

                    var imageBytes = client.GetByteArrayAsync(ball.ImageUri).Result;

                    File.WriteAllBytes(Path.Combine(outputFolder, "Images", ball.Image), imageBytes);
                }
            }
        }

        /// <summary>
        /// Parses Fiddler dumps of Track classic ball information.
        /// </summary>
        /// <param name="inputPath">Path the the Fiddler dump text file.</param>
        /// <param name="outputFolder">Path to the output folder.</param>
        private void ParseTrackClassic(string inputPath, string outputFolder)
        {
            // $hack(jeff.lill): 
            //
            // Hardcoded to the GET http://www.trackbowling.com/products/retired-balls page format
            // as captured by Fiddler.

            // Split the input file text up into the HTTP responses for each ball.

            var responses   = new List<string>();
            var captureText = File.ReadAllText(inputPath);
            int startPos    = 0;
            int endPos      = 0;

            while (true)
            {
                startPos = captureText.IndexOf("\r\nHTTP/1.1 200 OK", endPos);

                if (startPos == -1)
                {
                    break;
                }

                endPos = captureText.IndexOf("\r\nGET http://www.trackbowling.com/products/retired-ball", startPos);

                if (endPos == -1)
                {
                    responses.Add(captureText.Substring(startPos));
                    break;
                }
                else
                {
                    responses.Add(captureText.Substring(startPos, endPos - startPos));
                }
            }

            // Parse the responses to extract the ball information.

            var balls = new List<BallInfo>();

            foreach (var response in responses)
            {
                var ball = new BallInfo()
                {
                    Manufacturer = "Track",
                    Retired      = "X"
                };

                balls.Add(ball);

                // Extract the model

                startPos = response.IndexOf("<!-- InstanceBeginEditable name=\"main_content\" -->");
                startPos = response.IndexOf("<h1>", startPos);
                endPos   = response.IndexOf("</h1>", startPos);

                ball.Model = CleanField(response.Substring(startPos, endPos - startPos));

                // Extract the image URL

                const string imageArea   = "<div class=\"product-detail-main\">";
                const string imagePrefix = "<img src=\"";

                startPos = response.IndexOf(imageArea);
                startPos = response.IndexOf(imagePrefix, endPos);

                if (startPos != -1)
                {
                    endPos = response.IndexOf(">", startPos);

                    var imageElement = response.Substring(startPos, endPos - startPos);

                    if (imageElement.Contains("id=\"ballCover\""))
                    {
                        startPos += imagePrefix.Length;
                        endPos    = response.IndexOf("\"", startPos);

                        ball.ImageUri = response.Substring(startPos, endPos - startPos);
                    }
                }

                // The ball image file name is the model name with any embedded forward or back 
                // slashes replaced by dashes and appending the downloaded image's file extension.
                // 
                // NOTE: We need to convert some special characters so ZIP will work.

                if (!string.IsNullOrEmpty(ball.ImageUri))
                {
                    ball.Image = ball.Model.Replace("/", "-").Replace("\\", "-");
                    ball.Image = "track." + ball.Image + Path.GetExtension(ball.ImageUri);
                    ball.Image = ball.Image.Replace("’", "'");
                    ball.Image = ball.Image.Replace("ñ", "n");
                    ball.Image = ball.Image.Replace("\"", string.Empty);
                }

                // Extract Coverstock

                const string coverstockPrefix = "Outer shell of the ball";
                const string ddElement        = "<dd>";

                startPos = response.IndexOf(coverstockPrefix);

                if (startPos != -1)
                {
                    startPos  = response.IndexOf(ddElement, startPos);
                    startPos += ddElement.Length;
                    endPos    = response.IndexOf("</dd>", startPos);

                    ball.Coverstock = CleanField(response.Substring(startPos, endPos - startPos));
                }

                // Extract Finish

                const string finishPrefix = "Surface texture on the ball";

                startPos = response.IndexOf(finishPrefix);

                if (startPos != -1)
                {
                    startPos  = response.IndexOf(ddElement, startPos);
                    startPos += ddElement.Length;
                    endPos    = response.IndexOf("</dd>", startPos);

                    ball.Finish = CleanField(response.Substring(startPos, endPos - startPos));
                }

                // Extract Color

                const string colorPrefix = "Color of the ball";

                startPos  = response.IndexOf(colorPrefix);

                if (startPos != -1)
                {
                    startPos  = response.IndexOf(ddElement, startPos);
                    startPos += ddElement.Length;
                    endPos    = response.IndexOf("</dd>", startPos);

                    ball.Color = CleanField(response.Substring(startPos, endPos - startPos));
                }

                // Extract Weights

                const string weightsPrefix = "Available weights";

                startPos = response.IndexOf(weightsPrefix);

                if (startPos != -1)
                {
                    startPos = response.IndexOf(ddElement, startPos);
                    startPos += ddElement.Length;
                    endPos    = response.IndexOf("</dd>", startPos);

                    ball.Weights = CleanField(response.Substring(startPos, endPos - startPos));
                    ball.Weights = ball.Weights.Replace("Lbs.", string.Empty);
                    ball.Weights = ball.Weights.Replace("LBS", string.Empty);
                    ball.Weights = ball.Weights.Trim();

                    switch (ball.Weights)
                    {
                        case "10-16":

                            ball.Weights = "10,11,12,13,14,15,16";
                            break;

                        case "12-16":

                            ball.Weights = "12,13,14,15,16";
                            break;

                        case "14-16":

                            ball.Weights = "14,15,16";
                            break;

                        case "15-16":

                            ball.Weights = "15,16";
                            break;

                        case "6-16":

                            ball.Weights = "6,7,8,9,10,11,12,13,14,15,16";
                            break;

                        case "6, 8, 10-16":

                            ball.Weights = "6,8,10,11,12,13,14,15,16";
                            break;
                    }
                }
            }

            // Write the TSV file.

            using (var output = new StreamWriter(new FileStream(Path.Combine(outputFolder, "Capture.tsv"), FileMode.Create), Encoding.UTF8))
            {
                output.WriteLine(BallInfo.HeaderRow);

                foreach (var ball in balls)
                {
                    ball.WriteCsv(output);
                }
            }

            // Download the ball images.
            
            Directory.CreateDirectory(Path.Combine(outputFolder, "Images"));

            using (var client = new HttpClient())
            {
                foreach (var ball in balls)
                {
                    if (!string.IsNullOrEmpty(ball.ImageUri))
                    {
                        Console.WriteLine($"Downloading: {ball.Image}");

                        var imageBytes = client.GetByteArrayAsync(ball.ImageUri).Result;

                        File.WriteAllBytes(Path.Combine(outputFolder, "Images", ball.Image), imageBytes);
                    }
                }
            }
        }

        private string CleanField(string value)
        {
            value = value.Replace("™", string.Empty);           // Remove TM  characters
            value = value.Replace("®", string.Empty);           // Remove (R) characters
            value = value.Replace(" &trade;", string.Empty);    // Remove TM and (R) HTML codes
            value = value.Replace("&trade;", string.Empty);
            value = value.Replace(" &reg;", string.Empty);
            value = value.Replace("&reg;", string.Empty);
            value = value.Replace("&quot;", "\"");             // Replace HTML quotes with real ones
            value = value.Replace("&ldquo;", "\"");
            value = value.Replace("&rdquo;", "\"");

            if (value.EndsWith(" *"))
            {
                value = value.Substring(0, value.Length - 2);   // Remove trailing " *" strings
            }

            value = value.Replace("  ", " ");                   // Replace double space with single space
            value = value.Replace(" / ", "/");                  // Tighten up color combinations

            // Strip out any embedded HTML elements (e.g. <span>...</span>)

            if (value.Contains("<"))
            {
                var sb      = new StringBuilder();
                var include = true;

                foreach (var ch in value)
                {
                    switch (ch)
                    {
                        case '<':

                            include = false;
                            break;

                        case '>':

                            include = true;
                            break;

                        default:

                            if (include)
                            {
                                sb.Append(ch);
                            }
                            break;
                    }
                }

                value = sb.ToString();
            }

            return value.Trim();
        }
    }
}
