﻿//-----------------------------------------------------------------------------
// FILE:	    IOExtensions.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>
    /// Implements I/O related class extensions.
    /// </summary>
    public static class IOExtensions
    {
        //---------------------------------------------------------------------
        // Stream extensions

        /// <summary>
        /// Writes a byte array to a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="bytes">The byte array.</param>
        public static void Write(this Stream stream, byte[] bytes)
        {
            Covenant.Requires<ArgumentNullException>(stream != null);
            Covenant.Requires<ArgumentNullException>(bytes != null);

            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Asynchronously writes a byte array to a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="bytes">The byte array.</param>
        public static async Task WriteAsync(this Stream stream, byte[] bytes)
        {
            Covenant.Requires<ArgumentNullException>(stream != null);
            Covenant.Requires<ArgumentNullException>(bytes != null);

            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Reads the byte array from the current position, advancing
        /// the position past the value read.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="cb">The number of bytes to read.</param>
        /// <returns>
        /// The byte array.  Note that the array returned may have a length
        /// less than the size requested if the end of the file has been
        /// reached.
        /// </returns>
        public static byte[] ReadBytes(this Stream stream, int cb)
        {
            byte[]  buf;
            byte[]  temp;
            int     cbRead;

            buf    = new byte[cb];
            cbRead = stream.Read(buf, 0, cb);

            if (cbRead == cb)
            {
                return buf;
            }

            temp = new byte[cbRead];
            Array.Copy(buf, temp, cbRead);
            return temp;
        }

        /// <summary>
        /// Reads all bytes from the current position to the end of the stream.
        /// </summary>
        /// <returns>The byte array.</returns>
        public static byte[] ReadToEnd(this Stream stream)
        {
            Covenant.Requires<ArgumentNullException>(stream != null);

            var buffer = new byte[64 * 1024];

            using (var ms = new MemoryStream(64 * 1024))
            {
                while (true)
                {
                    var cb = stream.Read(buffer, 0, buffer.Length);

                    if (cb == 0)
                    {
                        return ms.ToArray();
                    }

                    ms.Write(buffer, 0, cb);
                }
            }
        }

        /// <summary>
        /// Asynchronously reads all bytes from the current position to the end of the stream.
        /// </summary>
        /// <returns>The byte array.</returns>
        public static async Task<byte[]> ReadToEndAsync(this Stream stream)
        {
            Covenant.Requires<ArgumentNullException>(stream != null);

            var buffer = new byte[16 * 1024];

            using (var ms = new MemoryStream(16 * 1024))
            {
                while (true)
                {
                    var cb = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (cb == 0)
                    {
                        return ms.ToArray();
                    }

                    ms.Write(buffer, 0, cb);
                }
            }
        }

        //---------------------------------------------------------------------
        // TextReader extensions

        /// <summary>
        /// Returns an enumerator that returns the lines of text from a <see cref="TextReader"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The <see cref="IEnumerable{String}"/>.</returns>
        public static IEnumerable<string> Lines(this TextReader reader)
        {
            Covenant.Requires<ArgumentNullException>(reader != null);

            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                yield return line;
            }
        }
    }
}
