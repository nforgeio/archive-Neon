﻿//-----------------------------------------------------------------------------
// FILE:	    ConsulExtensions.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Neon.Stack.Common;

namespace Consul
{
    /// <summary>
    /// HashiCorp Consul extensions.
    /// </summary>
    public static class ConsulExtensions
    {
        //---------------------------------------------------------------------
        // IKVEndpoint extensions

        /// <summary>
        /// Determines whether a Consul key exists.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns><c>true</c> if the key exists.</returns>
        public static async Task<bool> Exists(this IKVEndpoint kv, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(key));

            return (await kv.Get(key, cancellationToken)).Response != null;
        }

        /// <summary>
        /// Writes a byte array value to a Consul key.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns><c>true</c> on success.</returns>
        public static async Task<bool> PutBytes(this IKVEndpoint kv, string key, byte[] value, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(key));

            var p = new KVPair(key);

            p.Value = value ?? new byte[0];

            return (await kv.Put(p, cancellationToken)).Response;
        }

        /// <summary>
        /// Writes a string value to a Consul key.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns><c>true</c> on success.</returns>
        /// <remarks>
        /// This method writes an empty string for <c>null</c> values and writes
        /// the <see cref="object.ToString()"/> results otherwise.
        /// </remarks>
        public static async Task<bool> PutString(this IKVEndpoint kv, string key, object value, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(key));

            var p = new KVPair(key);

            if (value == null)
            {
                value = string.Empty;
            }

            p.Value = Encoding.UTF8.GetBytes(value.ToString());

            return (await kv.Put(p, cancellationToken)).Response;
        }

        /// <summary>
        /// Writes a boolean value to a Consul key.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns><c>true</c> on success.</returns>
        public static async Task<bool> PutBool(this IKVEndpoint kv, string key, bool value, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await PutString(kv, key, value ? "true" : "false", cancellationToken);
        }

        /// <summary>
        /// Writes an integer value to a Consul key.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns><c>true</c> on success.</returns>
        public static async Task<bool> PutInt(this IKVEndpoint kv, string key, int value, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await PutString(kv, key, value.ToString(), cancellationToken);
        }

        /// <summary>
        /// Writes a long value to a Consul key.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns><c>true</c> on success.</returns>
        public static async Task<bool> PutLong(this IKVEndpoint kv, string key, long value, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await PutString(kv, key, value.ToString(), cancellationToken);
        }

        /// <summary>
        /// Writes a double value to a Consul key.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns><c>true</c> on success.</returns>
        public static async Task<bool> PutDouble(this IKVEndpoint kv, string key, double value, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await PutString(kv, key, value.ToString("R", NumberFormatInfo.InvariantInfo), cancellationToken);
        }

        /// <summary>
        /// Writes an object value as JSON to a Consul key.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="formatting">Optional JSON formatting (defaults to <b>None</b>).</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns><c>true</c> on success.</returns>
        public static async Task<bool> PutObject(this IKVEndpoint kv, string key, object value, Formatting formatting = Formatting.None, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await PutString(kv, key, NeonHelper.JsonSerialize(value, formatting), cancellationToken);
        }

        /// <summary>
        /// Reads a key as a byte array.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The byte array value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if <paramref name="key"/> could not be found.</exception>
        public static async Task<byte[]> GetBytes(this IKVEndpoint kv, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(key));

            var response = (await kv.Get(key, cancellationToken)).Response;

            if (response == null)
            {
                throw new KeyNotFoundException(key);
            }

            return response.Value;
        }

        /// <summary>
        /// Reads a key as a string.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The string value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if <paramref name="key"/> could not be found.</exception>
        public static async Task<string> GetString(this IKVEndpoint kv, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(key));

            var response = (await kv.Get(key, cancellationToken)).Response;

            if (response == null)
            {
                throw new KeyNotFoundException(key);
            }

            return Encoding.UTF8.GetString(response.Value);
        }

        /// <summary>
        /// Reads and parses a key as a <c>bool</c>.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if <paramref name="key"/> could not be found.</exception>
        /// <exception cref="FormatException">Thrown if the value is not valid.</exception>
        public static async Task<bool> GetBool(this IKVEndpoint kv, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            var input = await GetString(kv, key, cancellationToken);

            switch (input.ToLowerInvariant())
            {
                case "0":
                case "no":
                case "false":

                    return false;

                case "1":
                case "yes":
                case "true":

                    return true;

                default:

                    throw new FormatException($"[{input}] is not a valid boolean.");
            }
        }

        /// <summary>
        /// Reads and parses a key as an <c>int</c>.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if <paramref name="key"/> could not be found.</exception>
        /// <exception cref="FormatException">Thrown if the value is not valid.</exception>
        public static async Task<int> GetInt(this IKVEndpoint kv, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            var input = await GetString(kv, key, cancellationToken);
            int value;

            if (int.TryParse(input, out value))
            {
                return value;
            }
            else
            {
                throw new FormatException($"[{input}] is not a valid integer.");
            }
        }

        /// <summary>
        /// Reads and parses a key as a <c>long</c>.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if <paramref name="key"/> could not be found.</exception>
        /// <exception cref="FormatException">Thrown if the value is not valid.</exception>
        public static async Task<long> GetLong(this IKVEndpoint kv, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            var     input = await GetString(kv, key, cancellationToken);
            long    value;

            if (long.TryParse(input, out value))
            {
                return value;
            }
            else
            {
                throw new FormatException($"[{input}] is not a valid long.");
            }
        }

        /// <summary>
        /// Reads and parses a key as a <c>double</c>.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if <paramref name="key"/> could not be found.</exception>
        /// <exception cref="FormatException">Thrown if the value is not valid.</exception>
        public static async Task<double> GetDouble(this IKVEndpoint kv, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            var     input = await GetString(kv, key, cancellationToken);
            double  value;

            if (double.TryParse(input, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out value))
            {
                return value;
            }
            else
            {
                throw new FormatException($"[{input}] is not a valid double.");
            }
        }

        /// <summary>
        /// Reads and deserializes a key with a JSON value as a specified type.
        /// </summary>
        /// <typeparam name="T">The type to be desearialized.</typeparam>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The parsed <typeparamref name="T"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if <paramref name="key"/> could not be found.</exception>
        /// <exception cref="FormatException">Thrown if the value is not valid.</exception>
        public static async Task<T> GetObject<T>(this IKVEndpoint kv, string key, CancellationToken cancellationToken = default(CancellationToken))
            where T : new()
        {
            var input = await GetString(kv, key, cancellationToken);

            try
            {
                return NeonHelper.JsonDeserialize<T>(input);
            }
            catch (Exception e)
            {
                throw new FormatException(e.Message, e);
            }
        }

        /// <summary>
        /// Watches a key or key prefix for changes, invoking an asynchronous callback whenever
        /// a change is detected or a timeout has been exceeded.
        /// </summary>
        /// <param name="kv">The key/value endpoint.</param>
        /// <param name="key">The key or key prefix ending with a  forward slash (<b>/</b>).</param>
        /// <param name="action">The asynchronous action with a boolean parameter that will be passed as <c>true</c> if a change was detected.</param>
        /// <param name="timeout">The optional timeout (defaults to <see cref="Timeout.InfiniteTimeSpan"/>).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The <see cref="Task"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method provides an easy way to monitor a Consul key or key prefix for changes.
        /// <paramref name="key"/> specifies the key or prefix.  Prefixes are distinguished by
        /// a terminating forward slash (<b>/</b>).
        /// </para>
        /// <para>
        /// <paramref name="action"/> must be passed as an async handler that will be called when
        /// a potential change is detected.
        /// </para>
        /// <note>
        /// Consul may invoke the action even though nothing has changed.  This occurs when the
        /// request times out (a maximum of 10 minutes) or when an idempotent operation has been
        /// performed (e.g. a transaction?).  Applications will need to take any necessary care 
        /// to verify that that the notification should actually trigger an action.
        /// </note>
        /// <para>
        /// <paramref name="timeout"/> specifies the maximum time to wait for Consul to respond.
        /// This defaults to <see cref="Timeout.InfiniteTimeSpan"/> which means the method will
        /// wait forever.  It can be useful to specify a different <paramref name="timeout"/>.
        /// With this, the method will call the <paramref name="action"/> whenever a change
        /// is detected or when the timeout has been exceeded.  The action parameter will be
        /// <c>false</c> for the latter case.
        /// </para>
        /// <para>
        /// Here's an example:
        /// </para>
        /// <code lang="c#">
        /// ConsulClient    consul;
        /// 
        /// await consul.KV.Watch("foo", 
        ///     async changed =>
        ///     {
        ///         if (changed)
        ///         {
        ///             // Do something when the key changed.
        ///         }
        ///         else
        ///         {
        ///             // Do something for timeouts.
        ///         }
        ///     },
        ///     TimeSpan.FromSeconds(30));
        /// </code> 
        /// </remarks>
        public static async Task Watch(this IKVEndpoint kv, string key, Func<Task> action, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(key));
            Covenant.Requires<ArgumentNullException>(action != null);

            if (timeout <= TimeSpan.Zero)
            {
                timeout = Timeout.InfiniteTimeSpan;
            }

            await Task.Run(
                async () =>
                {
                    var response  = await kv.Get(key, cancellationToken);
                    var lastIndex = response.LastIndex;
                    var options   = new QueryOptions() { WaitTime = timeout };

                    await action();

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        options.WaitIndex = lastIndex;
                        response          = await kv.Get(key, options, cancellationToken);

                        await action();

                        lastIndex = response.LastIndex;
                    }
                });
        }
    }
}
