//-----------------------------------------------------------------------------
// FILE:	    NetworkCidr.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

#if !XAMARIN

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// $todo(jeff.lill): Support IPv6.

namespace Neon.Stack.Net
{
    /// <summary>
    /// Describes a IP network subnet using Classless Inter-Domain Routing (CIDR) notation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is generally used for describing an IP subnet.  See the following Wikipedia
    /// article for more information.
    /// </para>
    /// <para>
    /// https://en.wikipedia.org/wiki/Classless_Inter-Domain_Routing#CIDR_notation
    /// </para>
    /// <note>
    /// This class currently supports only IPv4 addresses.
    /// </note>
    /// </remarks>
    public class NetworkCidr
    {
        //---------------------------------------------------------------------
        // Operators

        /// <summary>
        /// Compares two <see cref="NetworkCidr"/> instances for equality.
        /// </summary>
        /// <param name="v1">Value 1.</param>
        /// <param name="v2">Value 2</param>
        /// <returns><c>true</c> if the values are equal.</returns>
        public static bool operator ==(NetworkCidr v1, NetworkCidr v2)
        {
            return v1.Equals(v2);
        }

        /// <summary>
        /// Compares two <see cref="NetworkCidr"/> instances for inequality.
        /// </summary>
        /// <param name="v1">Value 1.</param>
        /// <param name="v2">Value 2</param>
        /// <returns><c>true</c> if the values are not equal.</returns>
        public static bool operator !=(NetworkCidr v1, NetworkCidr v2)
        {
            return !v1.Equals(v2);
        }

        /// <summary>
        /// Implicitly casts a <see cref="NetworkCidr"/> into a string.
        /// </summary>
        /// <param name="v">The value (or <c>null)</c>.</param>
        public static implicit operator string(NetworkCidr v)
        {
            if (v == null)
            {
                return null;
            }

            return v.ToString();
        }

        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Parses a subnet from CIDR notation in the form of <i>ip-address</i>/<i>prefix</i>,
        /// where <i>prefix</i> is the network prefix length in bits.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The parsed <see cref="NetworkCidr"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the input is not correctly formatted.</exception>
        public static NetworkCidr Parse(string input)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(input));

            int         slashPos = input.IndexOf('/');
            IPAddress   address;
            int         prefixLength;

            if (slashPos <= 0)
            {
                throw new ArgumentException($"Invalid CIDR [{input}].");
            }

            if (!IPAddress.TryParse(input.Substring(0, slashPos), out address))
            {
                throw new ArgumentException($"Invalid CIDR [{input}].");
            }

            if (!int.TryParse(input.Substring(slashPos + 1), out prefixLength) || prefixLength < 0 || prefixLength > 32)
            {
                throw new ArgumentException($"Invalid CIDR [{input}].");
            }

            var cidr = new NetworkCidr();

            cidr.Initialize(address, prefixLength);

            return cidr;
        }

        /// <summary>
        /// Attempts to parse a subnet from CIDR notation in the form of <i>ip-address</i>/<i>prefix</i>,
        /// where <i>prefix</i> is the network prefix length in bits.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="cidr">The parsed <see cref="NetworkCidr"/>.</param>
        /// <returns><c>true</c> if the operation was successful.</returns>
        public static bool TryParse(string input, out NetworkCidr cidr)
        {
            cidr = null;

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            int         slashPos = input.IndexOf('/');
            IPAddress   address;
            int         prefixLength;

            if (slashPos <= 0)
            {
                return false;
            }

            if (!IPAddress.TryParse(input.Substring(0, slashPos), out address))
            {
                return false;
            }

            if (!int.TryParse(input.Substring(slashPos + 1), out prefixLength) || prefixLength < 0 || prefixLength > 32)
            {
                return false;
            }

            cidr = new NetworkCidr();

            cidr.Initialize(address, prefixLength);

            return true;
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Private constructor.
        /// </summary>
        private NetworkCidr()
        {
        }

        /// <summary>
        /// Creates a subnet from an IP address and prefix length.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <param name="prefixLength">The network prefix mask length in bits.</param>
        public NetworkCidr(IPAddress address, int prefixLength)
        {
            Covenant.Requires<ArgumentNullException>(address != null);
            Covenant.Requires<ArgumentException>(address.AddressFamily == AddressFamily.InterNetwork);
            Covenant.Requires<ArgumentException>(0 <= prefixLength && prefixLength <= 32);

            Initialize(address, prefixLength);
        }

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <param name="prefixLength">The network prefix mask length in bits.</param>
        private void Initialize(IPAddress address, int prefixLength)
        {
            Address      = address;
            PrefixLength = prefixLength;

            var maskBytes = new byte[4];
            var bitArray  = new BitArray(maskBytes);

            for (int i = 0; i < prefixLength; i++)
            {
                bitArray.Set(31 - i, true);
            }

            bitArray.Not();
#if TODO
            // $todo(jeff.lill): Might be able to restore this when .NET Standard 2.0 is released.

            bitArray.CopyTo(maskBytes, 0);
#else
            for (int i = 0; i < bitArray.Length; i++)
            {
                if (bitArray[i])
                {
                    var index = i / 8;
                    var bit   = 1 << (7 - (i % 8));

                    maskBytes[index] |= (byte)bit;
                }
            }
#endif
            Mask = new IPAddress(maskBytes);
        }

        /// <summary>
        /// Returns the CIDR address.
        /// </summary>
        public IPAddress Address { get; private set; }

        /// <summary>
        /// Returns the subnet mask.
        /// </summary>
        public IPAddress Mask { get; private set; }

        /// <summary>
        /// Returns the prefix length in bits.
        /// </summary>
        public int PrefixLength { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Address}/{PrefixLength}";
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Address.GetHashCode() ^ PrefixLength.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is NetworkCidr))
            {
                return false;
            }

            var other = (NetworkCidr)obj;

            return other.Address.Equals(this.Address) && other.PrefixLength == this.PrefixLength;
        }
    }
}

#endif // XAMARIN
