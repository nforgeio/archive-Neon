using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace TinyURL.Controllers
{
    /// <summary>
    /// Handles <see cref="Link"/> related operations.
    /// </summary>
    public class LinksController : ApiController
    {
        /// <summary>
        /// Returns the links, optionally filtered by source.
        /// </summary>
        /// <param name="source">The optional source filter.</param>
        /// <returns>The <see cref="Link"/> instances from the database.</returns>
        public IEnumerable<Link> GetLinks(string source = null)
        {
            using (var context = new TinyUrlContext())
            {
                if (string.IsNullOrEmpty(source))
                {
                    return context.Links.ToList();
                }
                else
                {
                    var query =
                        from l in context.Links
                        where l.Source == source
                        select l;

                    return query.ToList();
                }
            }
        }

        /// <summary>
        /// Returns a specific link based on its ID.
        /// </summary>
        /// <param name="id">The link ID.</param>
        /// <returns>The <see cref="Link"/>.</returns>
        public Link GetLink(string id)
        {
            using (var context = new TinyUrlContext())
            {
                var query =
                    from l in context.Links
                    where l.Id == id
                    select l;

                var link = query.FirstOrDefault();

                if (link == null)
                {
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                }
                else
                {
                    return link;
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="Link"/> to the database.
        /// </summary>
        /// <param name="link">The new link.</param>
        /// <returns>The new link.</returns>
        public Link PutLink(Link link)
        {
            if (link == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(link.Id))
            {
                // We're going to set the link ID to a GUID, converted to Base64 with any forward
                // slashes converted to dashes and plus signs converted to $ (to make the ID URL-safe) 
                // and removing any padding equal signs as unnecessary.

                link.Id = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    .Replace('/', '-')
                    .Remove('+', '$')
                    .Replace("=", string.Empty);
            }

            using (var context = new TinyUrlContext())
            {
                context.Links.Add(link);
                context.SaveChanges();

                return link;
            }
        }

        /// <summary>
        /// Updates an existing <see cref="Link"/> in the database.
        /// </summary>
        /// <param name="link">The new link.</param>
        /// <returns>The updated link.</returns>
        public Link PostLink(Link link)
        {
            if (link == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(link.Id))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            using (var context = new TinyUrlContext())
            {
                var query =
                    from l in context.Links
                    where l.Id == link.Id
                    select l;

                var existing = query.FirstOrDefault();

                if (existing == null)
                {
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                }

                existing.Source = link.Source;
                existing.ProductId = link.ProductId;

                context.SaveChanges();

                return existing;
            }
        }

        /// <summary>
        /// Deletes a specific link based on its ID.
        /// </summary>
        /// <param name="id">The link ID.</param>
        public IHttpActionResult DeleteLink(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            using (var context = new TinyUrlContext())
            {
                var query =
                    from l in context.Links
                    where l.Id == id
                    select l;

                var existing = query.FirstOrDefault();

                if (existing == null)
                {
                    return NotFound();
                }

                context.Links.Remove(existing);
                context.SaveChanges();

                return Ok();
            }
        }
    }
}
