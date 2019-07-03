namespace TinyURL
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describes a TinyURL.
    /// </summary>
    [DataContract]
    public partial class Link
    {
        /// <summary>
        /// The URL ID.
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string Id { get; set; }

        /// <summary>
        /// The URL source type: <b>nfc</b>, <b>qrcode</b>...
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string Source { get; set; }

        /// <summary>
        /// The ID of item referenced by this link.
        /// </summary>
        [DataMember]
        public int? ProductId { get; set; }

        /// <summary>
        /// The referenced product.
        /// </summary>
        public virtual Product Product { get; set; }
    }
}
