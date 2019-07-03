﻿//-----------------------------------------------------------------------------
// FILE:	    CertificateOptions.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Neon.Stack.Common;
using Neon.Stack.Cryptography;
using Neon.Stack.IO;

namespace Neon.Cluster
{
    /// <summary>
    /// Defines the components of an optional TLS certificate.
    /// </summary>
    public class CertificateOptions
    {
        /// <summary>
        /// The optional path to the PEM encoded TLS certificate file to be used to secure access to a service.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This class follows the <b>HAProxy</b> convention of allowing the PEM encoded public certificate
        /// and private key to be encoded into a single text file by simply by concatenating the public
        /// certificate with the private key, certificate first.
        /// </para>
        /// <note>
        /// The certificate part must include any intermediate certificates issues by the certificate
        /// authority after the certificate and before the private key.
        /// </note>
        /// </remarks>
        [JsonProperty(PropertyName = "path", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string Path { get; set; } = null;

        /// <summary>
        /// Returns <c>true</c> if TLS is enabled.
        /// </summary>
        [JsonIgnore]
        public bool IsSecured
        {
            get { return !string.IsNullOrWhiteSpace(Path); }
        }

        /// <summary>
        /// Returns the URL scheme implied by these options: <b>https</b> if certificate is
        /// present, <b>http</b> otherwise.
        /// </summary>
        [JsonIgnore]
        public string Scheme
        {
            get { return IsSecured ? "https" : "http"; }
        }

        /// <summary>
        /// Validates the options definition and also ensures that all <c>null</c> properties are
        /// initialized to their default values.
        /// </summary>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <param name="parentOptionName">Identifies the parent option type (used in error messages).</param>
        /// <exception cref="ClusterDefinitionException">Thrown if the definition is not valid.</exception>
        [Pure]
        public void Validate(ClusterDefinition clusterDefinition, string parentOptionName)
        {
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            parentOptionName = parentOptionName ?? string.Empty;

            if (IsSecured)
            {
                if (!File.Exists(Path))
                {
                    throw new FileNotFoundException($"[{parentOptionName}] TLS certificate file [{Path}] does not exist.");
                }
            }
        }

        /// <summary>
        /// Returns a deep clone of the current instance.
        /// </summary>
        /// <returns>The clone.</returns>
        public CertificateOptions Clone()
        {
            return new CertificateOptions()
            {
                Path = this.Path
            };
        }

        /// <summary>
        /// Loads the certificate files (if any).
        /// </summary>
        /// <returns>
        /// Returns a <see cref="TlsCertificate"/> instance with the loaded public certificate 
        /// and private key or <c>null</c> if no certificate is defined.
        /// </returns>
        public TlsCertificate Load()
        {
            if (IsSecured)
            {
                var certificate = TlsCertificate.Load(Path);

                certificate.Parse();

                return certificate;
            }
            else
            {
                return null;
            }
        }
    }
}
