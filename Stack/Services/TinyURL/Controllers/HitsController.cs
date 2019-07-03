using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace TinyURL.Controllers
{
    public class HitsController : ApiController
    {
        /// <summary>
        /// Returns the hits.
        /// </summary>
        /// <returns>The <see cref="Hit"/> instances from the database.</returns>
        public IEnumerable<Hit> GetLinks()
        {
            using (var context = new TinyUrlContext())
            {
                return context.Hits.ToArray();
            }
        }
    }
}
