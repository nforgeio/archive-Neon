//-----------------------------------------------------------------------------
// FILE:	    SignageContentPackage.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;

namespace Neon.Fun.Signage
{
    /// <summary>
    /// Handles the packaging and unpackaging of signage content formatted as a 
    /// ZIP archive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Digital signage content consists of a collection of files that are expected
    /// by the signage content control that handles the rendering of the specific
    /// content type.  Typically included will be one or more XAML files that specify
    /// how the content will be rendered, possibly along with media files with static
    /// images or video as well as data files.
    /// </para>
    /// <note>
    /// The file names and folder structure are specific to the content control.
    /// </note>
    /// <para>
    /// This class doesn't currently do much.  It simply zips and unzips files and
    /// folders via 
    /// </para>
    /// </remarks>
    public class SignageContentPackage
    {
    }
}
