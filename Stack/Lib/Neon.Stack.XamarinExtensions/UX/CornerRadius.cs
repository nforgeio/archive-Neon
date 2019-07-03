//-----------------------------------------------------------------------------
// FILE:        CornerRadius.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Describes the radii for the four corners of a box.
    /// </summary>
    [TypeConverter(typeof(CornerRadiusTypeConverter))]
    public struct CornerRadius
    {
        /// <summary>
        /// Returns the top-left corner radius.
        /// </summary>
        public double TopLeft { get; private set; }

        /// <summary>
        /// Returns the top-right corner radius.
        /// </summary>
        public double TopRight { get; private set; }

        /// <summary>
        /// Returns the bottom-right corner radius.
        /// </summary>
        public double BottomRight { get; private set; }

        /// <summary>
        /// Returns the bottom-left corner radius.
        /// </summary>
        public double BottomLeft { get; private set; }

        /// <summary>
        /// Constructs an instance with uniform radii for all corners.
        /// </summary>
        /// <param name="uniformSize">The corner radius.</param>
        public CornerRadius(double uniformSize)
        {
            if (uniformSize < 0.0)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.TopLeft     =
            this.TopRight    =
            this.BottomRight =
            this.BottomLeft  = uniformSize;
        }

        /// <summary>
        /// Constructs an instance specifying the radius for each of the
        /// four corners.
        /// </summary>
        /// <param name="topLeft">The top-left corner radius.</param>
        /// <param name="topRight">The top-right corner radius.</param>
        /// <param name="bottomRight">The bottom-right corner radius.</param>
        /// <param name="bottomLeft">The bottom-left corner radius.</param>
        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            if (topLeft < 0.0 || topRight < 0.0 || bottomRight < 0.0 || bottomLeft < 0.0)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.TopLeft     = topLeft;
            this.TopRight    = topRight;
            this.BottomRight = bottomRight;
            this.BottomLeft  = bottomLeft;
        }

        /// <summary>
        /// Renders the instance as a string.
        /// </summary>
        /// <returns>The rendered string.</returns>
        public override string ToString()
        {
            return $"TopLeft={TopLeft}, TopRight={TopRight}, BottomLeft={BottomLeft}, BottomRight={BottomRight}";
        }
    }
}
