//-----------------------------------------------------------------------------
// FILE:	    ParseUsbcCentersCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
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
    /// Parses raw USBC bowling center data downloaded from http://webservices.bowl.com/USBCFindA/Home/Center
    /// using Fiddler and writes tabular CSV data to the the standard output.
    /// </summary>
    public class ParseUsbcCentersCommand : ICommand
    {
        //---------------------------------------------------------------------
        // Private types

        /// <summary>
        /// Schema for individual USBC bowling center data.
        /// </summary>
        private class UsbcCenter
        {
            [JsonProperty(PropertyName = "address")]
            public string Address { get; set;}

            [JsonProperty(PropertyName = "arcade")]
            public bool Arcade { get; set; }

            [JsonProperty(PropertyName = "banquets")]
            public bool Banquets { get; set; }

            [JsonProperty(PropertyName = "certnumber")]
            public int CertNumber { get; set; }

            [JsonProperty(PropertyName = "childcare")]
            public bool Childcare { get; set; }

            [JsonProperty(PropertyName = "citystatezip")]
            public string CityStateZip { get; set; }

            [JsonProperty(PropertyName = "coach")]
            public bool Coach { get; set; }

            [JsonProperty(PropertyName = "email")]
            public string Email { get; set; }

            [JsonProperty(PropertyName = "glow")]
            public bool Glow { get; set; }

            [JsonProperty(PropertyName = "id")]
            public int Id { get; set; }

            [JsonProperty(PropertyName = "lanes")]
            public int Lanes { get; set; }

            [JsonProperty(PropertyName = "lounge")]
            public bool Lounge { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "parties")]
            public bool Parties { get; set; }

            [JsonProperty(PropertyName = "phone")]
            public string Phone { get; set; }

            [JsonProperty(PropertyName = "proshop")]
            public bool ProShop { get; set; }

            [JsonProperty(PropertyName = "restaurant")]
            public bool Restaurant { get; set; }

            [JsonProperty(PropertyName = "rvp")]
            public bool Rvp { get; set; }

            [JsonProperty(PropertyName = "snackbar")]
            public bool Snackbar { get; set; }

            [JsonProperty(PropertyName = "sport")]
            public bool Sport { get; set; }

            [JsonProperty(PropertyName = "web")]
            public string Web { get; set; }

            //-----------------------------------------------------------------
            // Extended fields (not extracted from raw data).

            public string CountryCode { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public double Lat { get; set; } = -1;
            public double Lon { get; set; } = -1;
        }

        /// <summary>
        /// Schema for a page of center data from the webservice.
        /// </summary>
        private class UsbcCentersPageResponse
        {
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
            public List<UsbcCenter> Results { get; set; }
        }

        //---------------------------------------------------------------------
        // Implementation

        private const string usage = @"
fun-tool parse-usbc-centers [OPTIONS] FOLDER OUTPUT

    FOLDER      - Path to the folder with the raw JSON [*.txt] files
    OUTPUT      - Path to the output CSV file

    --geocode   - Uses Bing to convert the center address to LAT/LON 

Parses the USBC Bowling Center data downloaded as raw JSON web service
responses and generates a CSV file. 
";
        /// <inheritdoc/>
        public string Name
        {
            get { return "parse-usbc-centers"; }
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

            // Load the raw bowling center responses.

            var centers    = new Dictionary<int, UsbcCenter>();    // Maps USBC center-ID --> record
            var folderPath = commandLine.Arguments[1];
            var outputPath = commandLine.Arguments[2];
            var json       = new JsonSerializer();

            Console.WriteLine();
            Console.WriteLine("Parsing USBC responses...");

            foreach (var file in Directory.EnumerateFiles(folderPath, "*.txt"))
            {
                var page = NeonHelper.JsonDeserialize<UsbcCentersPageResponse>(File.ReadAllText(file));

                foreach (var center in page.Results)
                {
                    centers[center.Id] = center;
                }
            }

            Console.WriteLine($"Bowling Center Count: {centers.Count}");

            // Split city, state, and zipcode into separate fields and clean
            // up the phone number.

            Console.WriteLine("Normalizing data...");

            foreach (var center in centers.Values)
            {
                var commaPos = center.CityStateZip.LastIndexOf(',');

                center.CountryCode = "US";
                center.City        = center.CityStateZip.Substring(0, commaPos);
                center.State       = center.CityStateZip.Substring(commaPos + 2, 2);
                center.Phone       = center.Phone.Replace("/", "-")
                                                 .Replace("(", string.Empty)
                                                 .Replace(")", "-");

                // Strip the extended zip code (if any) and prefix it with zeros
                // to end up with five digits.

                var zip     = center.CityStateZip.Substring(center.CityStateZip.LastIndexOf(' ') + 1);
                var dashPos = zip.IndexOf('-');

                if (dashPos != -1)
                {
                    zip = zip.Substring(0, dashPos);
                }

                while (zip.Length < 5)
                {
                    zip = "0" + zip;
                }

                center.Zip = zip;
            }

            if (commandLine.HasOption("--geocode"))
            {
                Console.WriteLine("Geocoding...");

                // $hack(jeff.lill):
                //
                // Hardcoding this here.  At some point it would be nice to
                // build a geocoding library.

                var BingMapsKey = "AnzsnUz2slEXrN5r-dPRm_hqCwFih5SXRTIoW9UF6-S10N4WJ_9GWLnTqf8n6JCB";
                var Delay       = TimeSpan.FromSeconds(0.5);   // Avoid service throttling
                var success     = 0;
                var errors      = 0;

                using (var client = new JsonClient())
                {
                    foreach (var center in centers.Values)
                    {
                        var uri = $"http://dev.virtualearth.net/REST/v1/Locations/{center.CountryCode}/{center.State}/{center.Zip}/{center.City}/{center.Address}?key={BingMapsKey}";

                        try
                        {
                            var response = client.GetAsync<dynamic>(uri).Result;

                            if (response.resourceSets.Count == 0 || response.resourceSets[0].resources.Count == 0)
                            {
                                errors++;
                                continue;
                            }

                            var point       = response.resourceSets[0].resources[0].point;
                            var coordinates = point.coordinates;

                            center.Lat = (double)coordinates[0];
                            center.Lon = (double)coordinates[1];

                            success++;
                        }
                        catch
                        {
                            errors++;
                        }

                        Console.CursorLeft = 0;
                        Console.Write(new string(' ', 40));
                        Console.CursorLeft = 0;
                        Console.Write($"Geocoded: {success + errors + 1} of {centers.Count}   -- errors: {errors}");

                        Thread.Sleep(Delay);
                    }
                }

                Console.WriteLine();
            }

            // Generate the CSV output sorted by CountryCode, State, City, Name

            Console.WriteLine("Writing CSV file...");

            using (var output = new StreamWriter(outputPath))
            {
                output.WriteLine("CountryCode,State,City,Name,Lanes,UsbcID,CertNumber,Arcade,Banquets,Childcare,Coach,Glow,Lounge,Parties,ProShop,Restaurant,Rvp,Snackbar,Sport,Address,Zip,Phone,Email,Web,Lat,Lon");

                var sb = new StringBuilder();

                foreach (var center in centers.Values
                    .OrderBy(c => c.CountryCode)
                    .ThenBy(c => c.State)
                    .ThenBy(c => c.City)
                    .ThenBy(c => c.Name))
                {
                    sb.Clear();

                    AppendField(sb, center.CountryCode);
                    AppendField(sb, center.State);
                    AppendField(sb, center.City);
                    AppendField(sb, center.Name);
                    AppendField(sb, center.Lanes);
                    AppendField(sb, center.Id);
                    AppendField(sb, center.CertNumber);
                    AppendField(sb, center.Arcade);
                    AppendField(sb, center.Banquets);
                    AppendField(sb, center.Childcare);
                    AppendField(sb, center.Coach);
                    AppendField(sb, center.Glow);
                    AppendField(sb, center.Lounge);
                    AppendField(sb, center.Parties);
                    AppendField(sb, center.ProShop);
                    AppendField(sb, center.Restaurant);
                    AppendField(sb, center.Rvp);
                    AppendField(sb, center.Snackbar);
                    AppendField(sb, center.Sport);
                    AppendField(sb, center.Address);
                    AppendField(sb, center.Zip);
                    AppendField(sb, center.Phone);
                    AppendField(sb, center.Email);
                    AppendField(sb, center.Web);
                    AppendField(sb, center.Lat);
                    AppendField(sb, center.Lon);

                    output.WriteLine(sb);
                }
            }

            Console.WriteLine("*** DONE ***");
        }

        private void AppendField(StringBuilder sb, bool value)
        {
            if (value)
            {
                sb.AppendWithSeparator("X", ",");
            }
        }

        private void AppendField(StringBuilder sb, string value)
        {
            value = value ?? string.Empty;

            if (value.Contains(','))
            {
                sb.AppendWithSeparator($"\"{value}\"", ",");
            }
            else
            {
                sb.AppendWithSeparator(value, ",");
            }
        }

        private void AppendField(StringBuilder sb, double value)
        {
            sb.AppendWithSeparator(value.ToString(), ",");
        }
    }
}
