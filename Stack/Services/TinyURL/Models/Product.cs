namespace TinyURL
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describes a product (e.g. a bowling ball,...)
    /// </summary>
    [DataContract]
    public partial class Product
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Product()
        {
            Links = new HashSet<Link>();
        }

        /// <summary>
        /// The item ID.
        /// </summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>
        /// The URI to the web page for the item.
        /// </summary>
        [DataMember]
        [StringLength(2048)]
        public string Uri { get; set; }

        /// <summary>
        /// Identifies the product manufacturer.
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string Manufacturer { get; set; }

        /// <summary>
        /// Identifies the product type: <b>ball</b>, <b>bag></b>...
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string Type { get; set; }

        /// <summary>
        /// Identifies the product.
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string Name { get; set; }

        /// <summary>
        /// Identifies the product retailer.
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string Retailer { get; set; }

        /// <summary>
        /// TinyURL links to this product.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Link> Links { get; set; }
    }
}
