//-----------------------------------------------------------------------------
// FILE:        Attachment.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Describes an email attachment.
    /// </summary>
    public class Attachment
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The attachment file name.</param>
        /// <param name="data">The attachment data.</param>
        /// <param name="mimeType">The attachment MIME type.</param>
        public Attachment(string name, byte[] data, string mimeType)
        {
            this.Name     = name;
            this.Data     = data;
            this.MimeType = mimeType;
        }

        /// <summary>
        /// Returns the attachment file name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns the attachment data.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// The attachment MIME type.
        /// </summary>
        public string MimeType { get; private set; }
    }
}
