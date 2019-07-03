using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace TinyURL
{
    /// <summary>
    /// Intercepts root path requests with TinyURLs and redirects
    /// the caller to the associated product URI.
    /// </summary>
    public class RedirectHandler : DelegatingHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            const string NotFoundMessage = "<html><body><h1>Not Found</h1></body></html>";

            HttpResponseMessage response;

            switch (request.RequestUri.Segments.Length)
            {
                case 0:
                case 1:

                    response = new HttpResponseMessage(HttpStatusCode.NotFound);
                    response.Content = new StringContent(NotFoundMessage, Encoding.UTF8, "text/html");

                    return await Task.FromResult(response);

                case 2:

                    break;

                default:

                    return await base.SendAsync(request, cancellationToken);
            }

            if (request.RequestUri.Segments[1].Equals("swagger", StringComparison.OrdinalIgnoreCase))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // It looks like we have a candidate TinyURL so attempt the redirect.

            using (var context = new TinyUrlContext())
            {
                // Lookup the link

                var linkId = request.RequestUri.Segments[1];

                var query =
                    from l in context.Links
                    where l.Id == linkId
                    select l;

                // $todo(jeff.lill): This query should be async

                var link = query.FirstOrDefault();

                if (link == null || link.Product == null)
                {
                    response         = new HttpResponseMessage(HttpStatusCode.NotFound);
                    response.Content = new StringContent(NotFoundMessage, Encoding.UTF8, "text/html");

                    return await Task.FromResult(response);
                }

                // Log the hit.

                var product = link.Product;

                var hit = new Hit()
                {
                    Source       = link.Source,
                    IP           = request.GetRemoteIpAddress(),
                    Manufacturer = product.Manufacturer,
                    ProductType  = product.Type,
                    ProductName  = product.Name,
                    Retailer     = product.Retailer
                };

                context.Hits.Add(hit);
                await context.SaveChangesAsync();

                // Redirect the caller.

                response = new HttpResponseMessage(HttpStatusCode.Redirect);

                response.Headers.Location = new Uri(link.Product.Uri);

                return await Task.FromResult(response);
            }
        }
    }
}
