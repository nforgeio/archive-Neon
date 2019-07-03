namespace TinyURL
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    using System.Runtime.Serialization;

    /// <summary>
    /// Used to log a successful tiny URL redirection.
    /// </summary>
    [DataContract]
    public partial class Hit
    {
        /// <summary>
        /// The record ID.
        /// </summary>
        [DataMember]
        public long Id { get; set; }

        /// <summary>
        /// The event time (UTC).
        /// </summary>
        [DataMember]
        public DateTime TimeUtc { get; set; }

        /// <summary>
        /// The client IP address.
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string IP { get; set; }

        /// <summary>
        /// The URL source type: <b>nfc</b>, <b>qrcode</b>...
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string Source { get; set; }

        /// <summary>
        /// Identifies the product manufacturer.
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string Manufacturer { get; set; }

        /// <summary>
        /// Identifies the product type: <b>ball</b>, <b>bag</b>...
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string ProductType { get; set; }

        /// <summary>
        /// Identifies the item.
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string ProductName { get; set; }

        /// <summary>
        /// Identifies the item retailer.
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string Retailer { get; set; }
    }
}
