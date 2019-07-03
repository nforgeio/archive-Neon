﻿//-----------------------------------------------------------------------------
// FILE:	    NeonHelper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Neon.Stack.Diagnostics;

namespace Neon.Stack.Common
{
    /// <summary>
    /// Provides global common utilities and state.
    /// </summary>
    public static partial class NeonHelper
    {
        /// <summary>
        /// Ordinal value of an ASCII carriage return.
        /// </summary>
        public const int CR = 0x0D;

        /// <summary>
        /// Ordinal value of an ASCII linefeed.
        /// </summary>
        public const int LF = 0x0A;

        /// <summary>
        /// Ordinal value of an ASCII horizontal TAB.
        /// </summary>
        public const int HT = 0x09;

        /// <summary>
        /// Ordinal value of an ASCII escape character.
        /// </summary>
        public const int ESC = 0x1B;

        /// <summary>
        /// Ordinal value of an ASCII TAB character.
        /// </summary>
        public const int TAB = 0x09;

        /// <summary>
        /// A string consisting of a CRLF sequence.
        /// </summary>
        public const string CRLF = "\r\n";

        /// <summary>
        /// The constant 1,024 (2^10).
        /// </summary>
        public const int Kilo = 1024;

        /// <summary>
        /// The constant 1,048,576 (2^20).
        /// </summary>
        public const int Mega = Kilo * Kilo;

        /// <summary>
        /// The constant 1,073,741,824 (2^30).
        /// </summary>
        public const int Giga = Mega * Kilo;

        /// <summary>
        /// Returns the characters used as wildcards for the current file system.
        /// </summary>
        public static char[] FileWildcards { get; private set; } = new char[] { '*', '?' };

        /// <summary>
        /// Indicates whether the current application was built as 32 or 64-bit or <c>null</c>
        /// if this hasn't been determined yet.
        /// </summary>
        private static bool? is64Bit;

        /// <summary>
        /// Indicates whether the current application is running on a developer workstation
        /// or <c>null</c> if this hasn't been determined yet.  This is determined by the
        /// presence of the <b>DEV_WORKSTATION</b> environment variable.
        /// </summary>
        private static bool? isDevWorkstation;

        /// <summary>
        /// Returns <c>true</c> if the application was built as 64-bit.
        /// </summary>
        public static bool Is64Bit
        {
            get
            {
                if (is64Bit.HasValue)
                {
                    return is64Bit.Value;
                }
#if NETCORE
                is64Bit = System.Runtime.InteropServices.Marshal.SizeOf<IntPtr>() == 8;
#else
                is64Bit = System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntPtr)) == 8;
#endif
                return is64Bit.Value;
            }
        }

        /// <summary>
        /// Indicates whether the current application is running on a developer workstation.
        /// This is determined by the presence of the <b>DEV_WORKSTATION</b> environment variable.
        /// </summary>
        public static bool IsDevWorkstation
        {
            get
            {
                if (isDevWorkstation.HasValue)
                {
                    return isDevWorkstation.Value;
                }

                isDevWorkstation = Environment.GetEnvironmentVariable("DEV_WORKSTATION") != null;

                return isDevWorkstation.Value;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the library was built as 32-bit.
        /// </summary>
        public static bool Is32BitBuild
        {
            get { return !Is64Bit; }
        }

        /// <summary>
        /// Parses a floating point count string that may include one of the following unit
        /// suffixes: <b>B</b>, <b>K</b>, <b>KB</b>, <b>M</b>, <b>MB</b>, <b>G</b>, 
        /// or <b>GB</b>.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="value">Returns as the output value.</param>
        /// <returns><b>true</b> on success</returns>
        public static bool TryParseCount(string input, out double value)
        {
            value = 0.0;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var units = 1;
            var trim  = 0;

            if (input.EndsWith("KB", StringComparison.OrdinalIgnoreCase))
            {
                units = Kilo;
                trim  = 2;
            }
            else if (input.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
            {
                units = Mega;
                trim  = 2;
            }
            else if (input.EndsWith("GB", StringComparison.OrdinalIgnoreCase))
            {
                units = Giga;
                trim  = 2;
            }
            else if (input.EndsWith("B", StringComparison.OrdinalIgnoreCase))
            {
                units = 1;
                trim  = 1;
            }
            else if (input.EndsWith("K", StringComparison.OrdinalIgnoreCase))
            {
                units = Kilo;
                trim  = 1;
            }
            else if (input.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                units = Mega;
                trim  = 1;
            }
            else if (input.EndsWith("G", StringComparison.OrdinalIgnoreCase))
            {
                units = Giga;
                trim  = 1;
            }

            if (trim != 0)
            {
                input = input.Substring(0, input.Length - trim);
            }

            double raw;

            if (!double.TryParse(input.Trim(), out raw))
            {
                return false;
            }

            value = raw * units;

            return value >= 0.0;
        }
    }
}
