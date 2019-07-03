//-----------------------------------------------------------------------------
// FILE:	    HostOptions.cs
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
using Neon.Stack.IO;

namespace Neon.Cluster
{
    /// <summary>
    /// Describes cluster host machine configuration options.
    /// </summary>
    public class HostOptions
    {
        private const AuthMethods   defaultSshAuth        = AuthMethods.Tls;
        private const int           defaultPasswordLength = 15;
        private const bool          defaultPasswordAuth   = true;

        /// <summary>
        /// Specifies the authentication method to be used to secure SSH sessions
        /// to the cluster host nodes.  This defaults to  <see cref="AuthMethods.Tls"/>  
        /// for better security.
        /// </summary>
        [JsonProperty(PropertyName = "ssh_auth", Required = Required.Default)]
        [DefaultValue(defaultSshAuth)]
        public AuthMethods SshAuth { get; set; } = defaultSshAuth;

        /// <summary>
        /// Cluster hosts are configured with a random root account password.
        /// This defaults to <b>15</b> characters.  The minumum non-zero length
        /// is <b>8</b>.  Specify <b>0</b> to leave the root password unchanged.
        /// </summary>
        [JsonProperty(PropertyName = "password_length", Required = Required.Default)]
        [DefaultValue(defaultPasswordLength)]
        public int PasswordLength { get; set; } = defaultPasswordLength;

        /// <summary>
        /// Enables username/password authentication in addition to TLS authentication
        /// when <see cref="AuthMethods.Tls"/> is used.  This defaults to <c>true</c>.
        /// </summary>
        [JsonProperty(PropertyName = "password_auth", Required = Required.Default)]
        [DefaultValue(defaultPasswordAuth)]
        public bool PasswordAuth { get; set; } = defaultPasswordAuth;

        /// <summary>
        /// Validates the options definition and also ensures that all <c>null</c> properties are
        /// initialized to their default values.
        /// </summary>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <exception cref="ClusterDefinitionException">Thrown if the definition is not valid.</exception>
        [Pure]
        public void Validate(ClusterDefinition clusterDefinition)
        {
            if (PasswordLength > 0 && PasswordLength < 8)
            {
                throw new ClusterDefinitionException($"[{nameof(HostOptions)}.{nameof(PasswordLength)}={PasswordLength}] is not zero and is less than the minimum [8].");
            }
        }

        /// <summary>
        /// Returns a deep clone of the current instance.
        /// </summary>
        /// <returns>The clone.</returns>
        public HostOptions Clone()
        {
            return new HostOptions()
            {
                SshAuth        = this.SshAuth,
                PasswordLength = this.PasswordLength
            };
        }
    }
}
