//-----------------------------------------------------------------------------
// FILE:        PlatformConvert.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoreGraphics;
using UIKit;

using Xamarin.Forms;

namespace Neon.Stack.XamarinExtensions.iOS
{
    /// <summary>
    /// Implements conversions from Xamarin.Forms types to Platform specific types.
    /// </summary>
    public static class ConvertToPlatform
    {
        /// <summary>
        /// Converts a floating point color component in the range of 0.0..1.0 to 
        /// a byte value.
        /// </summary>
        /// <param name="value">The color component.</param>
        /// <returns>The color component byte value.</returns>
        private static byte ToColorByte(double value)
        {
            if (value <= 0.0)
            {
                return 0;
            }
            else if (value >= 1.0)
            {
                return 255;
            }
            else
            {
                return (byte)(255.0 * value);
            }
        }

        /// <summary>
        /// Converts a Xamarin.Forms color into a iOS UI color.
        /// </summary>
        /// <param name="color">The Xamarin.Forms color.</param>
        /// <returns>The iOS color.</returns>
        public static UIColor UIColor(global::Xamarin.Forms.Color color)
        {
            return UIKit.UIColor.FromRGBA(ToColorByte(color.R),
                                          ToColorByte(color.G),
                                          ToColorByte(color.B),
                                          ToColorByte(color.A));
        }

        /// <summary>
        /// Converts a Xamarin.Forms color into a iOS graphics color.
        /// </summary>
        /// <param name="color">The Xamarin.Forms color.</param>
        /// <returns>The iOS color.</returns>
        public static CGColor CGColor(global::Xamarin.Forms.Color color)
        {
            return new CoreGraphics.CGColor((nfloat)color.R,
                                            (nfloat)color.G,
                                            (nfloat)color.B,
                                            (nfloat)color.A);
        }
    }
}
