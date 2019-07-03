//-----------------------------------------------------------------------------
// FILE:	    VaultClient.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Neon.Stack.Common;
using Neon.Stack.Net;
using Neon.Stack.Retry;

namespace Neon.Cluster
{
    /// <summary>
    /// A light-weight (and incomplete) HashiCorp Vault client.
    /// </summary>
    public class VaultClient : IDisposable
    {
        private const string vaultApiVersion = "v1";

        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Opens a Vault connection using a Vault token.
        /// </summary>
        /// <param name="uri">The Vault server URI.</param>
        /// <param name="token">The token.</param>
        /// <returns>The <see cref="VaultClient"/>.</returns>
        public static VaultClient OpenWithToken(Uri uri, string token)
        {
            Covenant.Requires<ArgumentNullException>(uri != null);
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(token));

            var vaultClient = new VaultClient(uri);

            vaultClient.jsonClient.HttpClient.DefaultRequestHeaders.Add("X-Vault-Token", token);

            return vaultClient;
        }

        /// <summary>
        /// Opens a Vault connection using Vault AppRole credentials.
        /// </summary>
        /// <param name="uri">The Vault server URI.</param>
        /// <param name="roleId">The role ID.</param>
        /// <param name="secretId">The secret ID.</param>
        /// <returns>The <see cref="VaultClient"/>.</returns>
        public static VaultClient OpenWithAppRole(Uri uri, string roleId, string secretId)
        {
            Covenant.Requires<ArgumentNullException>(uri != null);
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(roleId));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(secretId));

            var vaultClient = new VaultClient(uri);

            dynamic loginPayload = new ExpandoObject();

            loginPayload.role_id = roleId;
            loginPayload.secret_id = secretId;

            var loginResponse = vaultClient.jsonClient.PostAsync($"/{vaultApiVersion}/auth/approle/login", loginPayload).Result.AsDynamic();

            vaultClient.jsonClient.HttpClient.DefaultRequestHeaders.Add("X-Vault-Token", (string)loginResponse.auth.client_token);

            return vaultClient;
        }

        //---------------------------------------------------------------------
        // Instance members

        private object          syncLock = new object();
        private JsonClient      jsonClient;

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="uri">The Vault server URI.</param>
        private VaultClient(Uri uri)
        {
#if NETCORE
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = ValidateRemoteCertificate
            };
#else
            var handler = new WebRequestHandler()
            {
                ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateRemoteCertificate)
            };
#endif

            jsonClient                        = new JsonClient(handler, disposeHandler: true);
            jsonClient.SafeRetryPolicy        = new ExponentialRetryPolicy(TransientDetector.NetworkAndHttp);
            jsonClient.HttpClient.BaseAddress = uri;
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~VaultClient()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all resources associated with the instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all associated resources.
        /// </summary>
        /// <param name="disposing">Pass <c>true</c> if we're disposing, <c>false</c> if we're finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            lock (syncLock)
            {
                if (jsonClient != null)
                {
                    jsonClient.Dispose();
                    jsonClient = null;
                }
            }
        }

        /// <summary>
        /// Returns the underlying <see cref="Stack.Net.JsonClient"/>.
        /// </summary>
        public JsonClient JsonClient
        {
            get { return jsonClient; }
        }

        /// <summary>
        /// Indicates the self-signed server certificates are to be trusted.
        /// This defaults to <c>true</c>.
        /// </summary>
        public bool AllowSelfSignedCertificates { get; set; } = true;
        
        /// <summary>
        /// Verifies the remote Secure Sockets Layer (SSL) certificate used for authentication.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="certificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate.</param>
        /// <returns><c>true</c> if the certificate is to be accepted.</returns>
        private  bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (AllowSelfSignedCertificates)
            {
                return (sslPolicyErrors & ~(SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch)) == 0;
            }
            else
            {
                return sslPolicyErrors == SslPolicyErrors.None;
            }
        }

        /// <summary>
        /// Removes any leading forward slash (/) from a path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The normalized path.</returns>
        private static string Normalize(string path)
        {
            if (path.StartsWith("/"))
            {
                return path.Substring(1);
            }
            else
            {
                return path;
            }
        }

        /// <summary>
        /// Reads the Vault object located at the specified path as a dynamic.
        /// </summary>
        /// <param name="path">The object path.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <returns>The result as a <c>dynamic</c> object.</returns>
        public async Task<dynamic> ReadDynamicAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));

            return (await jsonClient.GetAsync($"/{vaultApiVersion}/{Normalize(path)}", null, cancellationToken))
                .AsDynamic()
                .data;
        }

        /// <summary>
        /// Reads and deserializes the Vault object located at the specified path as JSON.
        /// </summary>
        /// <typeparam name="T">The type being read.</typeparam>
        /// <param name="path">The object path.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <returns>The result as a <c>dynamic</c> object.</returns>
        public async Task<T> ReadJsonAsync<T>(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));

            var jsonText = (await jsonClient.GetAsync($"/{vaultApiVersion}/{Normalize(path)}", null, cancellationToken))
                .AsDynamic()
                .data
                .ToString();

            return NeonHelper.JsonDeserialize<T>(jsonText);
        }

        /// <summary>
        /// Writes a value to a Vault path.
        /// </summary>
        /// <param name="path">The object path.</param>
        /// <param name="value">The value to be written or <c>null</c>.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <returns>The result as a <c>dynamic</c> object.</returns>
        public async Task<dynamic> WriteJsonAsync(string path, object value, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));
            Covenant.Requires<ArgumentNullException>(value != null);

            return (await jsonClient.PostAsync($"/{vaultApiVersion}/{Normalize(path)}", value, null, cancellationToken))
                .AsDynamic();
        }

        /// <summary>
        /// Deletes the value at a Vault path.
        /// </summary>
        /// <param name="path">The object path.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));

            var response = await JsonClient.DeleteUnsafeAsync($"/{vaultApiVersion}/{Normalize(path)}", null, cancellationToken);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.NotFound:

                    return;

                default:

                    response.EnsureSuccess();
                    break;
            }
        }

        /// <summary>
        /// Lists the keys beneath a Vault path.
        /// </summary>
        /// <param name="path">The vault path, with or without a trailing forward slash (<b>/</b>).</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <returns>A string list.</returns>
        public async Task<IEnumerable<string>> ListAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));

            if (!path.EndsWith("/"))
            {
                path += "/";
            }

            var result = await jsonClient.GetUnsafeAsync($"/{vaultApiVersion}/{Normalize(path)}", new ArgDictionary() { { "list", "true" } }, cancellationToken);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return new string[0];
            }
            else
            {
                result.EnsureSuccess();
            }

            dynamic v = result.AsDynamic();

            return (string[])((JArray)v.data.keys).ToObject(typeof(string[]));
        }

        /// <summary>
        /// Returns credentials for a Vault AppRole.
        /// </summary>
        /// <param name="roleName">The role name.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="ClusterCredentials"/>.</returns>
        public async Task<ClusterCredentials> GetAppRoleCredentialsAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            Covenant.Requires<ArgumentException>(ClusterDefinition.IsValidName(roleName));

            string roleId;
            string secretId;

            // Verify that the role exists.

            try
            {
                await ReadDynamicAsync($"auth/approle/role/{roleName}", cancellationToken);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to access Vault [AppRole={roleName}]: {NeonHelper.ExceptionError(e)}");
            }

            // Fetch the role ID.

            try
            {
                var response = await ReadDynamicAsync($"auth/approle/role/{roleName}/role-id", cancellationToken);

                roleId = response.role_id;
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to fetch the role ID for Vault [AppRole={roleName}]: {NeonHelper.ExceptionError(e)}");
            }

            // Fetch a secret ID.

            try
            {
                var response = (await WriteJsonAsync($"auth/approle/role/{roleName}/secret-id", cancellationToken)).data;

                secretId = response.secret_id;
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to fetch the role ID for Vault [AppRole={roleName}]: {NeonHelper.ExceptionError(e)}");
            }

            // Return the credentials.

            return ClusterCredentials.FromVaultRole(roleId, secretId);
        }
    }
}
