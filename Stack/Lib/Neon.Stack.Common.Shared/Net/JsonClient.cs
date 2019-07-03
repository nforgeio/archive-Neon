﻿//-----------------------------------------------------------------------------
// FILE:	    JsonClient.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Stack.Common;
using Neon.Stack.Diagnostics;
using Neon.Stack.Retry;

namespace Neon.Stack.Net
{
    /// <summary>
    /// Implements a light-weight JSON oriented HTTP client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="GetAsync(string, ArgDictionary, CancellationToken, LogActivity)"/>, 
    /// <see cref="PutAsync(string, dynamic, ArgDictionary, CancellationToken, LogActivity)"/>, 
    /// <see cref="PostAsync(string, dynamic, ArgDictionary, CancellationToken, LogActivity)"/>, and 
    /// <see cref="DeleteAsync(string, ArgDictionary, CancellationToken, LogActivity)"/>
    /// to perform HTTP operations that ensure that a non-error HTTP status code is returned by the servers.
    /// </para>
    /// <para>
    /// Use <see cref="GetUnsafeAsync(string, ArgDictionary, CancellationToken, LogActivity)"/>, 
    /// <see cref="PutUnsafeAsync(string, dynamic, ArgDictionary, CancellationToken, LogActivity)"/>, 
    /// <see cref="PostUnsafeAsync(string, dynamic, ArgDictionary, CancellationToken, LogActivity)"/>, and 
    /// <see cref="DeleteUnsafeAsync(string, ArgDictionary, CancellationToken, LogActivity)"/>
    /// to perform an HTTP without ensuring a non-error HTTP status code.
    /// </para>
    /// <para>
    /// This class can also handle retrying operations when transient errors are detected.  Set 
    /// <see cref="SafeRetryPolicy"/> to a <see cref="IRetryPolicy"/> implementation such as
    /// <see cref="LinearRetryPolicy"/> or <see cref="ExponentialRetryPolicy"/> to enable this.
    /// </para>
    /// <note>
    /// This class uses a reasonable <see cref="ExponentialRetryPolicy"/> by default.  You can override the default
    /// retry policy for specific requests using the methods that take an <see cref="IRetryPolicy"/> as their first
    /// parameter.
    /// </note>
    /// </remarks>
    public class JsonClient : IDisposable
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// The JSON HTTP content type.
        /// </summary>
        public const string JsonContentType = "application/json";

        //---------------------------------------------------------------------
        // Instance members

        private object          syncLock          = new object();
        private IRetryPolicy    safeRetryPolicy   = new ExponentialRetryPolicy(TransientDetector.NetworkAndHttp);
        private IRetryPolicy    unsafeRetryPolicy = new ExponentialRetryPolicy(TransientDetector.Network);
        private HttpClient      client;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="handler">The optional message handler.</param>
        /// <param name="disposeHandler">Indicates whether the handler will be disposed automatically (defaults to <c>false</c>).</param>
        public JsonClient(HttpMessageHandler handler = null, bool disposeHandler = false)
        {
            if (handler == null)
            {
                client = new HttpClient();
            }
            else
            {
                client = new HttpClient(handler, disposeHandler);
            }

            client.DefaultRequestHeaders.Add("Accept", JsonContentType);
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~JsonClient()
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
                if (client != null)
                {
                    client.Dispose();
                    client = null;
                }
            }
        }

        /// <summary>
        /// Returns the underlying <see cref="System.Net.Http.HttpClient"/>.
        /// </summary>
        public HttpClient HttpClient
        {
            get { return client; }
        }

        /// <summary>
        /// <para>
        /// The <see cref="IRetryPolicy"/> to be used to detect and retry transient network and HTTP
        /// errors for the <b>safe</b> methods.  This defaults to <see cref="ExponentialRetryPolicy"/> with 
        /// the transient detector function set to <see cref="TransientDetector.NetworkAndHttp(Exception)"/>.
        /// </para>
        /// <note>
        /// You may set this to <c>null</c> to disable safe transient error retry.
        /// </note>
        /// </summary>
        public IRetryPolicy SafeRetryPolicy
        {
            get { return safeRetryPolicy; }
            set { safeRetryPolicy = value ?? NoRetryPolicy.Instance; }
        }

        /// <summary>
        /// <para>
        /// The <see cref="IRetryPolicy"/> to be used to detect and retry transient network errors for the
        /// <b>unsafe</b> methods.  This defaults to <see cref="ExponentialRetryPolicy"/> with the transient 
        /// detector function set to <see cref="TransientDetector.NetworkAndHttp(Exception)"/>.
        /// </para>
        /// <note>
        /// You may set this to <c>null</c> to disable unsafe transient error retry.
        /// </note>
        /// </summary>
        public IRetryPolicy UnsafeRetryPolicy
        {
            get { return unsafeRetryPolicy; }
            set { unsafeRetryPolicy = value ?? NoRetryPolicy.Instance; }
        }

        /// <summary>
        /// Formats the URI by appending query arguments as required.
        /// </summary>
        /// <param name="uri">The base URI.</param>
        /// <param name="args">The query arguments.</param>
        /// <returns>The formatted URI.</returns>
        private string FormatUri(string uri, ArgDictionary args)
        {
            if (args == null || args.Count == 0)
            {
                return uri;
            }

            var sb    = new StringBuilder(uri);
            var first = true;

            foreach (var arg in args)
            {
                if (first)
                {
                    sb.Append('?');
                    first = false;
                }
                else
                {
                    sb.Append('&');
                }

                string value;

                if (arg.Value == null)
                {
                    value = "null";
                }
                else if (arg.Value is bool)
                {
                    value = (bool)arg.Value ? "true" : "false";
                }
                else
                {
                    value = arg.Value.ToString();
                }

                sb.Append($"{arg.Key}={Uri.EscapeDataString(value)}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the object passed into JSON content suitable for transmitting in
        /// an HTTP request.
        /// </summary>
        /// <param name="document">The document object or JSON text.</param>
        /// <returns>Tne <see cref="HttpContent"/>.</returns>
        private HttpContent CreateJsonContent(object document)
        {
            var json = document as string;

            return new StringContent(json ?? NeonHelper.JsonSerialize(document), Encoding.UTF8, JsonContentType);
        }

        /// <summary>
        /// Performs an HTTP <b>GET</b> ensuring that a success code was returned.
        /// </summary>
        /// <param name="uri">The URI</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<JsonResponse> GetAsync(string uri, ArgDictionary args = null, 
                                                 CancellationToken cancellationToken = default(CancellationToken),
                                                 LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            return await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.GetAsync(FormatUri(uri, args), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });
        }

        /// <summary>
        /// Performs an HTTP <b>GET</b> returning a specific type and ensuring that a success code was returned.
        /// </summary>
        /// <typeparam name="TResult">The desired result type.</typeparam>
        /// <param name="uri">The URI</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<TResult> GetAsync<TResult>(string uri, ArgDictionary args = null, 
                                                     CancellationToken cancellationToken = default(CancellationToken), 
                                                     LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            var result = await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.GetAsync(FormatUri(uri, args), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });

            return result.As<TResult>();
        }

        /// <summary>
        /// Performs an HTTP <b>GET</b> using a specific <see cref="IRetryPolicy"/>" and ensuring
        /// that a success code was returned.
        /// </summary>
        /// <param name="retryPolicy">The retry policy or <c>null</c> to disable retries.</param>
        /// <param name="uri">The URI</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<JsonResponse> GetAsync(IRetryPolicy retryPolicy, string uri, ArgDictionary args = null, 
                                                 CancellationToken cancellationToken = default(CancellationToken), 
                                                 LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            retryPolicy = retryPolicy ?? NoRetryPolicy.Instance;

            return await retryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.GetAsync(FormatUri(uri, args), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });
        }

        /// <summary>
        /// Performs an HTTP <b>GET</b> without ensuring that a success code was returned.
        /// </summary>
        /// <param name="uri">The URI</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        public async Task<JsonResponse> GetUnsafeAsync(string uri, ArgDictionary args = null,
                                                       CancellationToken cancellationToken = default(CancellationToken), 
                                                       LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            return await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var response = await client.GetAsync(FormatUri(uri, args), cancellationToken, activity);

                    return new JsonResponse(response, await response.Content.ReadAsStringAsync());
                });
        }

        /// <summary>
        /// Performs an HTTP <b>GET</b> using a specific <see cref="IRetryPolicy"/> and 
        /// without ensuring that a success code was returned.
        /// </summary>
        /// <param name="retryPolicy">The retry policy or <c>null</c> to disable retries.</param>
        /// <param name="uri">The URI</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        public async Task<JsonResponse> GetUnsafeAsync(IRetryPolicy retryPolicy, string uri, ArgDictionary args = null, 
                                                       CancellationToken cancellationToken = default(CancellationToken), 
                                                       LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            retryPolicy = retryPolicy ?? NoRetryPolicy.Instance;

            return await retryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var response = await client.GetAsync(FormatUri(uri, args), cancellationToken, activity);

                    return new JsonResponse(response, await response.Content.ReadAsStringAsync());
                });
        }

        /// <summary>
        /// Performs an HTTP <b>PUT</b> ensuring that a success code was returned.
        /// </summary>
        /// <param name="uri">The URI</param>
        /// <param name="document">The object to be uploaded.</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<JsonResponse> PutAsync(string uri, object document, ArgDictionary args = null, 
                                                 CancellationToken cancellationToken = default(CancellationToken), 
                                                 LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            return await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.PutAsync(FormatUri(uri, args), CreateJsonContent(document), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });
        }

        /// <summary>
        /// Performs an HTTP <b>PUT</b> returning a specific type and ensuring that a success code was returned.
        /// </summary>
        /// <typeparam name="TResult">The desired result type.</typeparam>
        /// <param name="uri">The URI</param>
        /// <param name="document">The object to be uploaded.</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<TResult> PutAsync<TResult>(string uri, object document, ArgDictionary args = null, 
                                                     CancellationToken cancellationToken = default(CancellationToken), 
                                                     LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            var result = await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.PutAsync(FormatUri(uri, args), CreateJsonContent(document), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });

            return result.As<TResult>();
        }

        /// <summary>
        /// Performs an HTTP <b>PUT</b> using a specific <see cref="IRetryPolicy"/>" and ensuring that a 
        /// success code was returned.
        /// </summary>
        /// <param name="retryPolicy">The retry policy or <c>null</c> to disable retries.</param>
        /// <param name="uri">The URI</param>
        /// <param name="document">The object to be uploaded.</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<JsonResponse> PutAsync(IRetryPolicy retryPolicy, string uri, object document, 
                                                 ArgDictionary args = null, CancellationToken cancellationToken = default(CancellationToken), 
                                                 LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            retryPolicy = retryPolicy ?? NoRetryPolicy.Instance;

            return await retryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.PutAsync(FormatUri(uri, args), CreateJsonContent(document), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });
        }

        /// <summary>
        /// Performs an HTTP <b>PUT</b> without ensuring that a success code was returned.
        /// </summary>
        /// <param name="uri">The URI</param>
        /// <param name="document">The object to be uploaded.</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        public async Task<JsonResponse> PutUnsafeAsync(string uri, object document, ArgDictionary args = null, 
                                                       CancellationToken cancellationToken = default(CancellationToken), 
                                                       LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            return await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var response = await client.PutAsync(FormatUri(uri, args), CreateJsonContent(document), cancellationToken, activity);

                    return new JsonResponse(response, await response.Content.ReadAsStringAsync());
                });
        }

        /// <summary>
        /// Performs an HTTP <b>PUT</b> using a specific <see cref="IRetryPolicy"/>" and without 
        /// ensuring that a success code was returned.
        /// </summary>
        /// <param name="retryPolicy">The retry policy or <c>null</c> to disable retries.</param>
        /// <param name="uri">The URI</param>
        /// <param name="document">The object to be uploaded.</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        public async Task<JsonResponse> PutUnsafeAsync(IRetryPolicy retryPolicy, string uri, object document, ArgDictionary args = null, 
                                                       CancellationToken cancellationToken = default(CancellationToken), 
                                                       LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            retryPolicy = retryPolicy ?? NoRetryPolicy.Instance;

            return await retryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var response = await client.PutAsync(FormatUri(uri, args), CreateJsonContent(document), cancellationToken, activity);

                    return new JsonResponse(response, await response.Content.ReadAsStringAsync());
                });
        }

        /// <summary>
        /// Performs an HTTP <b>POST</b> ensuring that a success code was returned.
        /// </summary>
        /// <param name="uri">The URI</param>
        /// <param name="document">The object to be uploaded.</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<JsonResponse> PostAsync(string uri, object document, ArgDictionary args = null, 
                                                  CancellationToken cancellationToken = default(CancellationToken), 
                                                  LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));
            Covenant.Requires<ArgumentNullException>(document != null);

            return await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.PostAsync(FormatUri(uri, args), CreateJsonContent(document), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });
        }

        /// <summary>
        /// Performs an HTTP <b>POST</b> returning a specific type and ensuring that a success code was returned.
        /// </summary>
        /// <typeparam name="TResult">The desired result type.</typeparam>
        /// <param name="uri">The URI</param>
        /// <param name="document">The object to be uploaded.</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<TResult> PostAsync<TResult>(string uri, object document, ArgDictionary args = null, 
                                                      CancellationToken cancellationToken = default(CancellationToken), 
                                                      LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));
            Covenant.Requires<ArgumentNullException>(document != null);

            var result = await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.PostAsync(FormatUri(uri, args), CreateJsonContent(document), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });

            return result.As<TResult>();
        }

        /// <summary>
        /// Performs an HTTP <b>POST</b> using a specific <see cref="IRetryPolicy"/> and ensuring that
        /// a success code was returned.
        /// </summary>
        /// <param name="retryPolicy">The retry policy or <c>null</c> to disable retries.</param>
        /// <param name="uri">The URI</param>
        /// <param name="document">The object to be uploaded.</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<JsonResponse> PostAsync(IRetryPolicy retryPolicy, string uri, object document, ArgDictionary args = null, 
                                                  CancellationToken cancellationToken = default(CancellationToken), 
                                                  LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));
            Covenant.Requires<ArgumentNullException>(document != null);

            retryPolicy = retryPolicy ?? NoRetryPolicy.Instance;

            return await retryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.PostAsync(FormatUri(uri, args), CreateJsonContent(document), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });
        }

        /// <summary>
        /// Performs an HTTP <b>POST</b> without ensuring that a success code was returned.
        /// </summary>
        /// <param name="uri">The URI</param>
        /// <param name="document">The object to be uploaded.</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        public async Task<JsonResponse> PostUnsafeAsync(string uri, object document, ArgDictionary args = null, 
                                                        CancellationToken cancellationToken = default(CancellationToken), 
                                                        LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));
            Covenant.Requires<ArgumentNullException>(document != null);

            return await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var response = await client.PostAsync(FormatUri(uri, args), CreateJsonContent(document), cancellationToken, activity);

                    return new JsonResponse(response, await response.Content.ReadAsStringAsync());
                });
        }

        /// <summary>
        /// Performs an HTTP <b>POST</b> using a specific <see cref="IRetryPolicy"/> and without ensuring
        /// that a success code was returned.
        /// </summary>
        /// <param name="retryPolicy">The retry policy or <c>null</c> to disable retries.</param>
        /// <param name="uri">The URI</param>
        /// <param name="document">The object to be uploaded.</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        public async Task<JsonResponse> PostUnsafeAsync(IRetryPolicy retryPolicy, string uri, object document, ArgDictionary args = null, 
                                                        CancellationToken cancellationToken = default(CancellationToken), 
                                                        LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));
            Covenant.Requires<ArgumentNullException>(document != null);

            retryPolicy = retryPolicy ?? NoRetryPolicy.Instance;

            return await retryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var response = await client.PostAsync(FormatUri(uri, args), CreateJsonContent(document), cancellationToken, activity);

                    return new JsonResponse(response, await response.Content.ReadAsStringAsync());
                });
        }

        /// <summary>
        /// Performs an HTTP <b>DELETE</b> ensuring that a success code was returned.
        /// </summary>
        /// <param name="uri">The URI</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<JsonResponse> DeleteAsync(string uri, ArgDictionary args = null, 
                                                    CancellationToken cancellationToken = default(CancellationToken),
                                                    LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            return await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.DeleteAsync(FormatUri(uri, args), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });
        }

        /// <summary>
        /// Performs an HTTP <b>DELETE</b> returning a specific type and ensuring that a success code was returned.
        /// </summary>
        /// <typeparam name="TResult">The desired result type.</typeparam>
        /// <param name="uri">The URI</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<TResult> DeleteAsync<TResult>(string uri, ArgDictionary args = null, 
                                                        CancellationToken cancellationToken = default(CancellationToken), 
                                                        LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            var result = await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.DeleteAsync(FormatUri(uri, args), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });

            return result.As<TResult>();
        }

        /// <summary>
        /// Performs an HTTP <b>DELETE</b> using a specific <see cref="IRetryPolicy"/> and ensuring 
        /// that a success code was returned.
        /// </summary>
        /// <param name="retryPolicy">The retry policy or <c>null</c> to disable retries.</param>
        /// <param name="uri">The URI</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        /// <exception cref="HttpException">Thrown when the server responds with an HTTP error status code.</exception>
        public async Task<JsonResponse> DeleteAsync(IRetryPolicy retryPolicy, string uri, ArgDictionary args = null, 
                                                    CancellationToken cancellationToken = default(CancellationToken), 
                                                    LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            retryPolicy = retryPolicy ?? NoRetryPolicy.Instance;

            return await retryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var httpResponse = await client.DeleteAsync(FormatUri(uri, args), cancellationToken, activity);
                    var jsonResponse = new JsonResponse(httpResponse, await httpResponse.Content.ReadAsStringAsync());

                    jsonResponse.EnsureSuccess();

                    return jsonResponse;
                });
        }

        /// <summary>
        /// Performs an HTTP <b>DELETE</b> without ensuring that a success code was returned.
        /// </summary>
        /// <param name="uri">The URI</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        public async Task<JsonResponse> DeleteUnsafeAsync(string uri, ArgDictionary args = null, 
                                                          CancellationToken cancellationToken = default(CancellationToken), 
                                                          LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            return await safeRetryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var response = await client.DeleteAsync(FormatUri(uri, args), cancellationToken, activity);

                    return new JsonResponse(response, await response.Content.ReadAsStringAsync());
                });
        }

        /// <summary>
        /// Performs an HTTP <b>DELETE</b> using a specific <see cref="IRetryPolicy"/> and without ensuring 
        /// that a success code was returned.
        /// </summary>
        /// <param name="retryPolicy">The retry policy or <c>null</c> to disable retries.</param>
        /// <param name="uri">The URI</param>
        /// <param name="args">The optional query arguments.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/>.</param>
        /// <param name="activity">The optional <see cref="LogActivity"/> whose ID is to be included in the request.</param>
        /// <returns>The <see cref="JsonResponse"/>.</returns>
        /// <exception cref="SocketException">Thrown for network connectivity issues.</exception>
        public async Task<JsonResponse> DeleteUnsafeAsync(IRetryPolicy retryPolicy, string uri, ArgDictionary args = null, 
                                                          CancellationToken cancellationToken = default(CancellationToken), 
                                                          LogActivity activity = default(LogActivity))
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            retryPolicy = retryPolicy ?? NoRetryPolicy.Instance;

            return await retryPolicy.InvokeAsync(
                async () =>
                {
                    var client = this.client;

                    if (client == null)
                    {
                        throw new ObjectDisposedException(nameof(JsonClient));
                    }

                    var response = await client.DeleteAsync(FormatUri(uri, args), cancellationToken, activity);

                    return new JsonResponse(response, await response.Content.ReadAsStringAsync());
                });
        }
    }
}
