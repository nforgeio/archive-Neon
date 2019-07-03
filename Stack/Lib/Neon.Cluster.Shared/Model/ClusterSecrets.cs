//-----------------------------------------------------------------------------
// FILE:	    ClusterSecrets.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Neon.Stack.Common;
using Neon.Stack.Cryptography;

namespace Neon.Cluster
{
    /// <summary>
    /// <para>
    /// Holds the <b>sensitive</b> information required to remotely manage an operating
    /// NeonCluster using the <b>neon.exe</b> tool.
    /// </para>
    /// <note>
    /// <b>WARNING:</b> The information serialized by this class must be carefully protected
    /// because it can be used to assume full control over a cluster.
    /// </note>
    /// </summary>
    public class ClusterSecrets
    {
        /// <summary>
        /// Returns the cluster name.
        /// </summary>
        [JsonIgnoreAttribute]
        public string Name
        {
            get { return Definition?.Name; }
        }

        /// <summary>
        /// The cluster definition.
        /// </summary>
        [JsonProperty(PropertyName = "definition", Required = Required.Always)]
        public ClusterDefinition Definition { get; set; }

        /// <summary>
        /// The host root user name.
        /// </summary>
        [JsonProperty(PropertyName = "root_account", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string RootAccount { get; set; }

        /// <summary>
        /// The host root password.
        /// </summary>
        [JsonProperty(PropertyName = "root_password", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string RootPassword { get; set; }

        /// <summary>
        /// The public and private parts of the SSH client key when the cluster is
        /// configured to authenticate clients via public keys or <c>null</c> when
        /// username/password authentication is enabled.
        /// </summary>
        [JsonProperty(PropertyName = "ssh_client_key", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public SshClientKey SshClientKey { get; set; }

        /// <summary>
        /// The HashiCorp Vault credentials.
        /// </summary>
        [JsonProperty(PropertyName = "vault_credentials", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public VaultCredentials VaultCredentials { get; set;}

        /// <summary>
        /// The HashiCorp Vault self-signed certificate.
        /// </summary>
        [JsonProperty(PropertyName = "vault_cert", Required = Required.Always)]
        public TlsCertificate VaultCertificate { get; set; }

        /// <summary>
        /// Returns the <see cref="SshCredentials"/> for the cluster that can be used
        /// by <see cref="NodeProxy{TMetadata}"/> and the <b>SSH.NET</b> Nuget package.
        /// </summary>
        /// <returns></returns>
        public SshCredentials GetSshCredentials()
        {
            if (SshClientKey != null)
            {
                return SshCredentials.FromPrivateKey(RootAccount, SshClientKey.PrivatePEM);
            }
            else
            {
                return SshCredentials.FromUserPassword(RootAccount, RootPassword);
            }
        }
    }
}
