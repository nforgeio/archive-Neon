using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace TinyURL
{
    public static class Extensions
    {
        /// <summary>
        /// Returns the IP address of the requesting client if available.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The IP address string or <c>null</c>.</returns>
        public static string GetRemoteIpAddress(this HttpRequestMessage request)
        {
            if (request == null)
            {
                return null;
            }

            object context;

            if (request.Properties.TryGetValue("MS_HttpContext", out context))
            {
                var httpContext = context as HttpContextBase;

                if (httpContext != null)
                {
                    var userAddress = httpContext.Request.UserHostAddress;

                    if (userAddress == null)
                    {
                        return null;
                    }

                    // Convert to IPv4 if possible.

                    IPAddress address;

                    if (!IPAddress.TryParse(userAddress, out address))
                    {
                        return null;
                    }

                    if (address.IsIPv4MappedToIPv6)
                    {
                        return address.MapToIPv4().ToString();
                    }
                    else
                    {
                        return userAddress;
                    }
                }
            }

            return null;
        }
    }
}
