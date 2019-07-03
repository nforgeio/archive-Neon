//-----------------------------------------------------------------------------
// FILE:        IQRCodeScanner.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Describes the cross platform implementation of a QR-Code scanner.
    /// </summary>
    public interface IQRCodeScanner
    {
        /// <summary>
        /// Attempts to use the device camera to scan a QR-Code.
        /// </summary>
        /// <returns>The barcode text or <c>null</c> if the scan was cancelled or the App doesn't have access to the camera.</returns>
        Task<string> ScanCameraAsync();

        /// <summary>
        /// Attempts to scan a QR-Code from an image.
        /// </summary>
        /// <param name="imageStream">A stream holding the image data to be scanned.</param>
        /// <returns>The barcode text or <c>null</c> if the image didn't contain a barcode.</returns>
        Task<string> ScanImageAsync(Stream imageStream);
    }
}
