using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace TinyURL.Controllers
{
    /// <summary>
    /// Handles <see cref="Product"/> related operations.
    /// </summary>
    public class ProductsController : ApiController
    {
        /// <summary>
        /// Returns the products, optionally filtered by manufacturer.
        /// </summary>
        /// <param name="manufacturer">The optional manufacture filter.</param>
        /// <returns>The <see cref="Product"/> instances from the database.</returns>
        public IEnumerable<Product> GetProducts(string manufacturer = null)
        {
            using (var context = new TinyUrlContext())
            {
                if (string.IsNullOrEmpty(manufacturer))
                {
                    return context.Products.ToList();
                }
                else
                {
                    var query =
                        from p in context.Products
                        where p.Manufacturer == manufacturer
                        select p;

                    return query.ToList();
                }
            }
        }

        /// <summary>
        /// Returns a specific product based on its ID.
        /// </summary>
        /// <param name="id">The product ID.</param>
        /// <returns>The <see cref="Link"/>.</returns>
        public Product GetProduct(int id)
        {
            using (var context = new TinyUrlContext())
            {
                var query =
                    from p in context.Products
                    where p.Id == id
                    select p;

                var product = query.FirstOrDefault();

                if (product == null)
                {
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                }
                else
                {
                    return product;
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="Product"/> to the database.
        /// </summary>
        /// <param name="product">The new product.</param>
        /// <returns>The new product.</returns>
        public Product PutProduct(Product product)
        {
            if (product == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            using (var context = new TinyUrlContext())
            {
                context.Products.Add(product);
                context.SaveChanges();

                return product;
            }
        }

        /// <summary>
        /// Updates an existing <see cref="Product"/> in the database.
        /// </summary>
        /// <param name="product">The new product.</param>
        /// <returns>The updated product.</returns>
        public Product PostProduct(Product product)
        {
            if (product == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            Uri uri;

            if (product.Uri != null && !Uri.TryCreate(product.Uri, UriKind.Absolute, out uri))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            using (var context = new TinyUrlContext())
            {
                var query =
                    from p in context.Products
                    where p.Id == product.Id
                    select p;

                var existing = query.FirstOrDefault();

                if (existing == null)
                {
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                }

                existing.Uri          = product.Uri;
                existing.Manufacturer = product.Manufacturer;
                existing.Type         = product.Type;
                existing.Name         = product.Name;
                existing.Retailer     = product.Retailer;

                context.SaveChanges();

                return existing;
            }
        }

        /// <summary>
        /// Deletes a specific product based on its ID.
        /// </summary>
        /// <param name="id">The product ID.</param>
        public IHttpActionResult DeleteProduct(int id)
        {
            using (var context = new TinyUrlContext())
            {
                var query =
                    from p in context.Products
                    where p.Id == id
                    select p;

                var existing = query.FirstOrDefault();

                if (existing == null)
                {
                    return NotFound();
                }

                context.Products.Remove(existing);
                context.SaveChanges();

                return Ok();
            }
        }
    }
}