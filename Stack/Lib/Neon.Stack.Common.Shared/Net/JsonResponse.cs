//-----------------------------------------------------------------------------
// FILE:	    JsonResponse.cs
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
using System.Text;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Stack.Common;
using Neon.Stack.Net;
using Neon.Stack.Retry;

namespace Neon.Stack.Net
{
    /// <summary>
    /// Encapsulates the response returned from a <see cref="JsonClient"/> 
    /// server call.
    /// </summary>
    public class JsonResponse
    {
        /// <summary>
        /// Constructs a <see cref="JsonResponse"/> from a lower level <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="httpRespose">The lower-level response.</param>
        /// <param name="responseText">The response text.</param>
        public JsonResponse(HttpResponseMessage httpRespose, string responseText)
        {
            Covenant.Requires<ArgumentNullException>(httpRespose != null);

            // $note(jeff.lill):
            //
            // I've seen situations where JSON REST APIs return [Content-Type: text/plain], 
            // so we'll accept that too.

            var jsonContent = httpRespose.Content.Headers.ContentType != null &&
                              (
                                  httpRespose.Content.Headers.ContentType.MediaType.Equals(JsonClient.JsonContentType, StringComparison.OrdinalIgnoreCase) ||
                                  httpRespose.Content.Headers.ContentType.MediaType.Equals("text/plain", StringComparison.OrdinalIgnoreCase)
                              );

            this.HttpResponse = httpRespose;

            if (httpRespose.Content.Headers.ContentType != null
                && jsonContent
                && responseText != null
                && responseText.Length > 0)
            {
                this.JsonText = responseText;
            }
        }

        /// <summary>
        /// Returns the low-level HTTP response.
        /// </summary>
        public HttpResponseMessage HttpResponse { get; private set; }

        /// <summary>
        /// Returns the response as JSON text or <c>null</c> if the server didn't
        /// respond with JSON.
        /// </summary>
        public string JsonText { get; private set; }

        /// <summary>
        /// Returns the dynamic JSON response document, array, value or <c>null</c> if the server didn't return
        /// JSON content.
        /// </summary>
        /// <returns>The dynamic document or <c>null</c>.</returns>
        public dynamic AsDynamic()
        {
            if (JsonText == null)
            {
                return null;
            }

            return JToken.Parse(JsonText);
        }

        /// <summary>
        /// Converts the response document to a specified type or <c>null</c> if the server didn't 
        /// return JSON content.
        /// </summary>
        /// <typeparam name="TResult">The specified type.</typeparam>
        /// <returns>The converted document or its default value.</returns>
        public TResult As<TResult>()
        {
            if (JsonText == null)
            {
                return default(TResult);
            }

            return NeonHelper.JsonDeserialize<TResult>(JsonText);
        }

        /// <summary>
        /// Returns the HTTP response status code.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get { return HttpResponse.StatusCode; }
        }

        /// <summary>
        /// Returns <c>true</c> if the response status code indicates success.
        /// </summary>
        public bool IsSuccess
        {
            get { return HttpResponse.IsSuccessStatusCode; }
        }

        /// <summary>
        /// Ensures that the status code indicates success by throwing an 
        /// exception if it does not.
        /// </summary>
        /// <exception cref="HttpException">Thrown if the response doesn't indicate success.</exception>
        public void EnsureSuccess()
        {
            if (!IsSuccess)
            {
                throw new HttpException(HttpResponse.StatusCode, HttpResponse.ReasonPhrase);
            }
        }
    }
}
