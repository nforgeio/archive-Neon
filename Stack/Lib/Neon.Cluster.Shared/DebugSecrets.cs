﻿//-----------------------------------------------------------------------------
// FILE:	    DebugSecrets.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Consul;
using Newtonsoft.Json;

using Neon.Stack.Common;

namespace Neon.Cluster
{
    /// <summary>
    /// Used to emulate Docker service secrets when debugging an application using 
    /// <see cref="NeonClusterHelper.ConnectCluster(DebugSecrets, string)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Add simple text secrets to the collection using <see cref="Add(string, string)"/>.
    /// </para>
    /// <para>
    /// You can also create temporary cluster Vault and Consul credentials using
    /// <see cref="VaultAppRole(string, string)"/> and <see cref="ConsulToken(string, string[])"/>.
    /// Temporary credentials have a lifespan of 1 day by default, but this can be
    /// changed by setting <see cref="CredentialTTL"/>.
    /// </para>
    /// </remarks>
    public class DebugSecrets : Dictionary<string, string>
    {
        //---------------------------------------------------------------------
        // Private types

        private enum CredentialType
        {
            VaultToken,
            VaultAppRole,
            ConsulToken
        }

        private class CredentialRequest
        {
            public CredentialType   Type;
            public string           SecretName;
            public string           RoleName;
            public string           Token;
        }

        //---------------------------------------------------------------------
        // Implementation

        private List<CredentialRequest> credentialRequests = new List<CredentialRequest>();
        private ClusterSecrets          clusterSecrets;
        private VaultClient             vaultClient;

        /// <summary>
        /// The lifespan of Vault and Consul credentials created by this class.  This defaults
        /// to 1 day, but may be modified by applications.
        /// </summary>
        public TimeSpan CredentialTTL { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Adds a string secret.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="value">The secret value.</param>
        /// <returns>The current instance to support fluent-style coding.</returns>
        public new DebugSecrets Add(string secretName, string value)
        {
            base.Add(secretName, value);

            return this;
        }

        /// <summary>
        /// Adds Vault token credentials to the dictionary.  The credentials will be
        /// formatted as <see cref="ClusterCredentials"/> serialized to JSON.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="token">The Vault token.</param>
        /// <returns>The current instance to support fluent-style coding.</returns>
        public DebugSecrets VaultToken(string secretName, string token)
        {
            credentialRequests.Add(
                new CredentialRequest()
                {
                    Type       = CredentialType.VaultToken,
                    SecretName = secretName,
                    Token      = token
                });

            return this;
        }

        /// <summary>
        /// Adds Vault AppRole credentials to the dictionary.  The credentials will be
        /// formatted as <see cref="ClusterCredentials"/> serialized to JSON.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="roleName">The Vault role name.</param>
        /// <returns>The current instance to support fluent-style coding.</returns>
        public DebugSecrets VaultAppRole(string secretName, string roleName)
        {
            credentialRequests.Add(
                new CredentialRequest()
                {
                    Type       = CredentialType.VaultAppRole,
                    SecretName = secretName,
                    RoleName   = roleName
                });

            return this;
        }

        /// <summary>
        /// Creates a temporary Consul token with the specified access control policies
        /// and then adds the token as a named secret.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="policies">The Consul policy names or HCL.</param>
        /// <returns>The current instance to support fluent-style coding.</returns>
        public DebugSecrets ConsulToken(string secretName, params string[] policies)
        {
            // $todo(jeff.lill): Implement this.

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a <see cref="VaultClient"/> for the attached cluster using the root token.
        /// </summary>
        private VaultClient VaultClient
        {
            get
            {
                if (vaultClient == null)
                {
                    vaultClient = NeonClusterHelper.OpenVault(ClusterCredentials.FromVaultToken(clusterSecrets.VaultCredentials.RootToken));
                }

                return vaultClient;
            }
        }

        /// <summary>
        /// Called internally by <see cref="NeonClusterHelper.ConnectCluster(DebugSecrets, string)"/> to 
        /// create any requested Vault and Consul credentials and add them to the dictionary.
        /// </summary>
        /// <param name="cluster">The attached cluster.</param>
        /// <param name="clusterSecrets">The attached cluster secrets.</param>
        internal void Realize(ClusterProxy cluster, ClusterSecrets clusterSecrets)
        {
            this.clusterSecrets = clusterSecrets;

            ClusterCredentials credentials;

            foreach (var request in credentialRequests)
            {
                switch (request.Type)
                {
                    case CredentialType.VaultToken:

                        // Serialize the credentials as JSON and persist.

                        credentials = ClusterCredentials.FromVaultToken(request.Token);

                        Add(request.SecretName, NeonHelper.JsonSerialize(credentials, Formatting.Indented));
                        break;

                    case CredentialType.VaultAppRole:

                        // Serialize the credentials as JSON and persist.

                        credentials = VaultClient.GetAppRoleCredentialsAsync(request.RoleName).Result;

                        Add(request.SecretName, NeonHelper.JsonSerialize(credentials, Formatting.Indented));
                        break;

                    case CredentialType.ConsulToken:

                        // $todo(jeff.lill): Implement this.

                        break;
                }
            }
        }
    }
}
