//-----------------------------------------------------------------------------
// FILE:	    SystemExtensions.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// System class extensions.
    /// </summary>
    public static class SystemExtensions
    {
        //---------------------------------------------------------------------
        // String extensions

#if NETCORE

        // $todo(jeff.lill):
        //
        // I'm not entirely sure why these methods aren't being implemented
        // by [System.Linq].

        /// <summary>
        /// Returns the first character of a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The first character.</returns>
        public static char First(this string input)
        {
            Covenant.Requires<ArgumentNullException>(input != null);
            Covenant.Requires<IndexOutOfRangeException>(input.Length > 0);

            return input[0];
        }

        /// <summary>
        /// Returns the last character of a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The last character.</returns>
        public static char Last(this string input)
        {
            Covenant.Requires<ArgumentNullException>(input != null);
            Covenant.Requires<IndexOutOfRangeException>(input.Length > 0);

            return input[input.Length - 1];
        }

#endif

        //---------------------------------------------------------------------
        // Encoding extensions

#if NETCORE
        /// <summary>
        /// Converts a byte array into a string using the encoding.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/>.</param>
        /// <param name="bytes">The input bytes.</param>
        /// <returns>The converted string.</returns>
        public static string GetString(this Encoding encoding, byte[] bytes)
        {
            Covenant.Requires<ArgumentNullException>(encoding != null);
            Covenant.Requires<ArgumentNullException>(bytes != null);

            return encoding.GetString(bytes, 0, bytes.Length);
        }
#endif
    }
}
