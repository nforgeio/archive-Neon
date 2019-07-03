//-----------------------------------------------------------------------------
// FILE:        IQRCodeGenerator.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Describes the cross platform implementation of a GQCode generator.
    /// </summary>
    public interface IQRCodeGenerator
    {
        /// <summary>
        /// Generates a QR-Code image.
        /// </summary>
        /// <param name="contents">The string to be encoded.</param>
        /// <param name="size">The size (width and height) of the QR-Code in pixels.</param>
        /// <param name="margin">The optional margin around the QR-Code in pixels.</param>
        /// <returns>A stream holding the generated QR-Code bitmap encoded as PNG.</returns>
        Stream Create(string contents, int size, int margin = 0);
    }
}
