﻿//-----------------------------------------------------------------------------
// FILE:	    ClusterCerts.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Consul;
using Newtonsoft;
using Newtonsoft.Json;

using Neon.Cluster;
using Neon.Stack.Common;
using Neon.Stack.Cryptography;
using Neon.Stack.Diagnostics;

namespace NeonProxyManager
{
    /// <summary>
    /// Holds the cluster certificates.
    /// </summary>
    public class ClusterCerts : Dictionary<string, CertInfo>
    {
        /// <summary>
        /// Adds certificate information.
        /// </summary>
        /// <param name="certInfo">The certificate information.</param>
        public void Add(CertInfo certInfo)
        {
            Add(certInfo.Name, certInfo);
        }

        /// <summary>
        /// Resets the <see cref="CertInfo.WasReferenced"/> properties for all certificates.
        /// </summary>
        public void ClearReferences()
        {
            foreach (var cert in base.Values)
            {
                cert.WasReferenced = false;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if one or more of the cluster certificates have been referenced.
        /// </summary>
        public bool HasReferences
        {
            get { return this.Values.FirstOrDefault(ci => ci.WasReferenced) != null; }
        }

        /// <summary>
        /// Returns the MD5 hash of the hashes of any referenced certificates (sorted by name).
        /// </summary>
        /// <returns>The hash bytes.</returns>
        public byte[] HashReferenced()
        {
            var hasher = MD5.Create();

            if (this.Values.FirstOrDefault(ci => ci.WasReferenced) == null)
            {
                return new byte[16];
            }

            using (var ms = new MemoryStream())
            {
                foreach (var cert in this.Values.Where(ci => ci.WasReferenced).OrderBy(ci => ci.Name))
                {
                    ms.Write(hasher.ComputeHash(NeonHelper.JsonSerialize(cert.Certificate, Formatting.None)));
                }

                ms.Position = 0;

                return hasher.ComputeHash(ms);
            }
        }

        /// <summary>
        /// Converts the instance to a dictionary of <see cref="TlsCertificate"/> instances.
        /// </summary>
        /// <returns>The converted dictionary.</returns>
        public Dictionary<string, TlsCertificate> ToTlsCertificateDictionary()
        {
            var output = new Dictionary<string, TlsCertificate>();

            foreach (var item in this)
            {
                output.Add(item.Key, item.Value.Certificate);
            }

            return output;
        }
    }
}
