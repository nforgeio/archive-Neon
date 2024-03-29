﻿//-----------------------------------------------------------------------------
// FILE:	    CertInfo.cs
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
    /// Certificate information.
    /// </summary>
    public class CertInfo
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The certificate name.</param>
        /// <param name="certificate">The certificate.</param>
        public CertInfo(string name, TlsCertificate certificate)
        {
            this.Name        = name;
            this.Certificate = certificate;
            this.Hash        = Convert.ToBase64String(MD5.Create().ComputeHash(NeonHelper.JsonSerialize(certificate, Formatting.None)));
        }

        /// <summary>
        /// Returns the certificate name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns the associated certificate.
        /// </summary>
        public TlsCertificate Certificate { get; private set; }

        /// <summary>
        /// Returns the base 64 encoded MD5 hash of the certificate.
        /// </summary>
        public string Hash { get; private set; }

        /// <summary>
        /// Used to indicate that this certificate was referenced by a proxy configuration.
        /// </summary>
        public bool WasReferenced { get; set; }
    }
}
