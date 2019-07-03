using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;

using Newtonsoft.Json;

namespace TinyURL
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // We're only doing JSON.

            GlobalConfiguration.Configuration.Formatters.Clear();
            GlobalConfiguration.Configuration.Formatters.Add(new JsonMediaTypeFormatter());

            // Pretty-print JSON output to make things nicer for debugging.

            var json = GlobalConfiguration.Configuration.Formatters.JsonFormatter;

            json.SerializerSettings.Formatting = Formatting.Indented;

            // Register the TinyURL redirect handler.

            GlobalConfiguration.Configuration.MessageHandlers.Add(new RedirectHandler());

            // Web API configuration and services

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "Api",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "Root",
                routeTemplate: "{action}",
                defaults: new { controller = "Root" }
            );
        }
    }
}
