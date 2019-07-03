//-----------------------------------------------------------------------------
// FILE:	    ISignageContentDocument.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models.Signage
{
    /// <summary>
    /// Holds the media and information required to render signage content.
    /// </summary>
    /// <remarks>
    /// The package properties will be available as the document content as
    /// an <see cref="ISignageContentProperties"/> entity and the media and other assets
    /// will be persisted as the <b>"package"</b> attachment as a ZIP archive.
    /// </remarks>
    [BinderDocument(typeof(ISignageContentProperties), Namespace = FunConst.SignageNamespace)]
    public interface ISignageContentDocument
    {
        /// <summary>
        /// File system path to the ZIP archive package file.
        /// </summary>
        [BinderAttachment(AttachmentName = "package")]
        string Package { get; }
    }
}
