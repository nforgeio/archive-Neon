//-----------------------------------------------------------------------------
// FILE:        BorderRenderer.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoreGraphics;
using UIKit;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using Neon.Stack.XamarinExtensions;
using Neon.Stack.XamarinExtensions.iOS;

[assembly: ExportRenderer(typeof(Border), typeof(BorderRenderer))]

namespace Neon.Stack.XamarinExtensions.iOS
{
    /// <summary>
    /// Implements the iOS renderer the <see cref="Border"/> control.
    /// </summary>
    public class BorderRenderer : ViewRenderer<Border, BorderRenderer.UIBorderView>
    {
        //---------------------------------------------------------------------
        // Private-ish types

        /// <summary>
        /// Implements the underlying iOS view.
        /// </summary>
        public class UIBorderView : UIView
        {
            private Border formsView;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="formsView">The parent Xamarin.Forms view.</param>
            public UIBorderView(Border formsView)
            {
                this.formsView = formsView;
            }

            /// <summary>
            /// Redraws the view.
            /// </summary>
            /// <param name="rect">Describes the area that needs redrawing.</param>
            public override void Draw(CGRect rect)
            {
                base.Draw(rect);

                var cornerRadius    = formsView.CornerRadius;
                var viewBounds      = this.Bounds;
                var roundedCorners  = cornerRadius.TopLeft != 0.0 ||
                                      cornerRadius.TopRight != 0.0 ||
                                      cornerRadius.BottomRight != 0.0 ||
                                      cornerRadius.BottomLeft != 0.0;
                var borderThickness = formsView.BorderThickness;
                var fixedThickness  = borderThickness.Left == borderThickness.Top &&
                                      borderThickness.Left == borderThickness.Right &&
                                      borderThickness.Left == borderThickness.Bottom;
                var g               = UIGraphics.GetCurrentContext();

                g.SaveState();

                // We need to inset the bounds by 1/2 the thickness of each side so
                // the lines will be drawn completely within the view boundry.  This
                // is necessary because iOS draws thick lines centered on the coordinates.
                //
                // Without this, the flat sides of the borders will be clipped and the
                // corners will look weirdly thick.

                CGRect borderBounds;

                if (fixedThickness)
                {
                    borderBounds = new CGRect(viewBounds.Left + borderThickness.Left / 2,
                                              viewBounds.Top + borderThickness.Top / 2,
                                              viewBounds.Width - (borderThickness.Left + borderThickness.Top) / 2,
                                              viewBounds.Height - (borderThickness.Top + borderThickness.Bottom) / 2);
                }
                else
                {
                    var leftInset   = Max(borderThickness.Bottom, borderThickness.Left, borderThickness.Top) / 2;
                    var topInset    = Max(borderThickness.Left, borderThickness.Top, borderThickness.Right) / 2;
                    var rightInset  = Max(borderThickness.Top, borderThickness.Right, borderThickness.Bottom) / 2;
                    var bottomInset = Max(borderThickness.Right, borderThickness.Bottom, borderThickness.Left) / 2;

                    borderBounds = new CGRect(viewBounds.Left + leftInset,
                                              viewBounds.Top + topInset,
                                              viewBounds.Width - (leftInset + rightInset),
                                              viewBounds.Height - (topInset + bottomInset));
                }

                // Don't draw anything for empty rectangles.

                if (borderBounds.Left >= borderBounds.Right ||
                    borderBounds.Top >= borderBounds.Bottom)
                {
                    return;
                }

                try
                {
                    // This is a bit tricky because we need to handle various combinations
                    // of corner radii and border thicknesses.

                    if (roundedCorners)
                    {
                        if (fixedThickness)
                        {
                            DrawRoundedFixedThickness(g, borderBounds, borderThickness.Left, cornerRadius);
                        }
                        else
                        {
                            DrawRoundedVariableThickness(g, borderBounds, borderThickness, cornerRadius);
                        }
                    }
                    else
                    {
                        if (fixedThickness)
                        {
                            DrawSquareFixedThickness(g, borderBounds, borderThickness.Left);
                        }
                        else
                        {
                            DrawSquareVariableThickness(g, borderBounds, borderThickness);
                        }
                    }
                }
                finally
                {
                    g.RestoreState();
                }
            }

            /// <summary>
            /// Returns the maximum of a set of non-negative values.
            /// </summary>
            /// <param name="values">The values.</param>
            /// <returns>The maximum.</returns>
            private static nfloat Max(params double[] values)
            {
                var max = 0.0;

                foreach (var value in values)
                {
                    if (value > max)
                    {
                        max = value;
                    }
                }

                return (nfloat)max;
            }

            /// <summary>
            /// Draws a rectangle with square corners and a border with a consistent width 
            /// for all sides.
            /// </summary>
            /// <param name="g">The graphic context.</param>
            /// <param name="borderBounds">The border rectangle.</param>
            /// <param name="lineWidth">The border width.</param>
            private void DrawSquareFixedThickness(CGContext g, CGRect borderBounds, double lineWidth)
            {
                g.SetFillColor(ConvertToPlatform.CGColor(formsView.BackgroundColor));
                g.SetStrokeColor(ConvertToPlatform.CGColor(formsView.BorderColor));
                g.SetLineWidth((nfloat)lineWidth);

                using (var path = new CGPath())
                {
                    path.MoveToPoint(borderBounds.Left, borderBounds.Top);
                    path.AddLineToPoint(borderBounds.Right, borderBounds.Top);
                    path.AddLineToPoint(borderBounds.Right, borderBounds.Bottom);
                    path.AddLineToPoint(borderBounds.Left, borderBounds.Bottom);
                    path.AddLineToPoint(borderBounds.Top, borderBounds.Left);

                    g.AddPath(path);
                    g.DrawPath(CGPathDrawingMode.FillStroke);
                }
            }

            /// <summary>
            /// Draws a rectangle with square corners and with sides with differing
            /// border thicknesses.
            /// </summary>
            /// <param name="g">The graphic context.</param>
            /// <param name="borderBounds">The border rectangle.</param>
            /// <param name="borderThickness">The border thickness.</param>
            private void DrawSquareVariableThickness(CGContext g, CGRect borderBounds, Thickness borderThickness)
            {
                // Fill the rectangle with no borders first.  We'll draw
                // the borders further below.

                g.SetFillColor(ConvertToPlatform.CGColor(formsView.BackgroundColor));
                g.SetStrokeColor(ConvertToPlatform.CGColor(formsView.BorderColor));
                g.SetLineWidth((nfloat)0.0);
                g.SetLineCap(CGLineCap.Butt);

                using (var path = new CGPath())
                {
                    path.MoveToPoint(borderBounds.Left, borderBounds.Top);
                    path.AddLineToPoint(borderBounds.Right, borderBounds.Top);
                    path.AddLineToPoint(borderBounds.Right, borderBounds.Bottom);
                    path.AddLineToPoint(borderBounds.Left, borderBounds.Bottom);
                    path.AddLineToPoint(borderBounds.Top, borderBounds.Left);

                    g.AddPath(path);
                    g.DrawPath(CGPathDrawingMode.Fill);
                }

                // Draw the left border.

                if (borderThickness.Left > 0.0)
                {
                    g.SetLineWidth((nfloat)borderThickness.Left);

                    g.MoveTo(borderBounds.Left, borderBounds.Top);
                    g.AddLineToPoint(borderBounds.Left, borderBounds.Bottom);
                    g.StrokePath();
                }

                // Draw the top border.

                if (borderThickness.Top > 0.0)
                {
                    g.SetLineWidth((nfloat)borderThickness.Top);

                    g.MoveTo(borderBounds.Left, borderBounds.Top);
                    g.AddLineToPoint(borderBounds.Right, borderBounds.Top);
                    g.StrokePath();
                }

                // Draw the right border.

                if (borderThickness.Right > 0.0)
                {
                    g.SetLineWidth((nfloat)borderThickness.Right);

                    g.MoveTo(borderBounds.Right, borderBounds.Top);
                    g.AddLineToPoint(borderBounds.Right, borderBounds.Bottom);
                    g.StrokePath();
                }

                // Draw the bottom border.

                if (borderThickness.Bottom > 0.0)
                {
                    g.SetLineWidth((nfloat)borderThickness.Bottom);

                    g.MoveTo(borderBounds.Right, borderBounds.Bottom);
                    g.AddLineToPoint(borderBounds.Left, borderBounds.Bottom);
                    g.StrokePath();
                }
            }

            /// <summary>
            /// Draws a rectangle with rounded corners, a border with a consistent thickness 
            /// for all sides.
            /// </summary>
            /// <param name="g">The graphic context.</param>
            /// <param name="borderBounds">The border rectangle.</param>
            /// <param name="lineWidth">The border width.</param>
            /// <param name="cornerRadius">The corner radii.</param>
            private void DrawRoundedFixedThickness(CGContext g, CGRect borderBounds, double lineWidth, CornerRadius cornerRadius)
            {
                var radiusTopLeft     = (nfloat)cornerRadius.TopLeft;
                var radiusTopRight    = (nfloat)cornerRadius.TopRight;
                var radiusBottomRight = (nfloat)cornerRadius.BottomRight;
                var radiusBottomLeft  = (nfloat)cornerRadius.BottomLeft;

                g.SetFillColor(ConvertToPlatform.CGColor(formsView.BackgroundColor));
                g.SetStrokeColor(ConvertToPlatform.CGColor(formsView.BorderColor));
                g.SetLineWidth((nfloat)lineWidth);
                g.SetLineJoin(CGLineJoin.Miter);

                using (var path = new CGPath())
                {
                    // Top border.

                    if (radiusTopRight == 0.0)
                    {
                        path.MoveToPoint(borderBounds.Left, borderBounds.Top);
                    }
                    else
                    {
                        path.MoveToPoint(borderBounds.Left + radiusTopLeft, borderBounds.Top);
                        path.AddArcToPoint(borderBounds.Right, borderBounds.Top, borderBounds.Right, borderBounds.Top + radiusTopRight, radiusTopRight);
                    }

                    // Right border.

                    if (radiusBottomRight == 0.0)
                    {
                        path.AddLineToPoint(borderBounds.Right, borderBounds.Bottom);
                    }
                    else
                    {
                        path.AddLineToPoint(borderBounds.Right, borderBounds.Bottom - radiusBottomRight);
                        path.AddArcToPoint(borderBounds.Right, borderBounds.Bottom, borderBounds.Right - radiusBottomRight, borderBounds.Bottom, radiusBottomRight);
                    }

                    // Bottom border.

                    if (radiusBottomLeft == 0.0)
                    {
                        path.AddLineToPoint(borderBounds.Left, borderBounds.Bottom);
                    }
                    else
                    {
                        path.AddLineToPoint(borderBounds.Left + radiusBottomLeft, borderBounds.Bottom);
                        path.AddArcToPoint(borderBounds.Left, borderBounds.Bottom, borderBounds.Left, borderBounds.Bottom - radiusBottomLeft, radiusBottomLeft);
                    }

                    // Left border.

                    if (radiusTopLeft == 0.0)
                    {
                        path.AddLineToPoint(borderBounds.Left, borderBounds.Top);
                    }
                    else
                    {
                        path.AddLineToPoint(borderBounds.Left, borderBounds.Top + radiusTopLeft);
                        path.AddArcToPoint(borderBounds.Left, borderBounds.Top, borderBounds.Left + radiusTopLeft, borderBounds.Top, radiusTopLeft);
                    }

                    // Draw the border.

                    g.AddPath(path);
                    g.DrawPath(CGPathDrawingMode.FillStroke);
                }
            }

            /// <summary>
            /// Draws a rectangle with rounded corners and with sides with differing
            /// border thicknesses.
            /// </summary>
            /// <param name="g">The graphic context.</param>
            /// <param name="borderBounds">The border rectangle.</param>
            /// <param name="borderThickness">The border thickness.</param>
            /// <param name="cornerRadius">The corner radii.</param>
            private void DrawRoundedVariableThickness(CGContext g, CGRect borderBounds, Thickness borderThickness, CornerRadius cornerRadius)
            {
                var radiusTopLeft     = (nfloat)cornerRadius.TopLeft;
                var radiusTopRight    = (nfloat)cornerRadius.TopRight;
                var radiusBottomRight = (nfloat)cornerRadius.BottomRight;
                var radiusBottomLeft  = (nfloat)cornerRadius.BottomLeft;

                g.SetFillColor(ConvertToPlatform.CGColor(formsView.BackgroundColor));
                g.SetStrokeColor(ConvertToPlatform.CGColor(formsView.BorderColor));
                g.SetLineJoin(CGLineJoin.Miter);
                g.SetLineCap(CGLineCap.Square);

                // Fill the view (not the border) bounds with no borders first.

                g.SetLineWidth((nfloat)0.0);
                
                using (var path = new CGPath())
                {
                    var viewBounds = this.Bounds;

                    // Top border.

                    if (radiusTopRight == 0.0)
                    {
                        path.MoveToPoint(viewBounds.Left, viewBounds.Top);
                    }
                    else
                    {
                        path.MoveToPoint(viewBounds.Left + radiusTopLeft, viewBounds.Top);
                        path.AddArcToPoint(viewBounds.Right, viewBounds.Top, viewBounds.Right, viewBounds.Top + radiusTopRight, radiusTopRight);
                    }

                    // Right border.

                    if (radiusBottomRight == 0.0)
                    {
                        path.AddLineToPoint(viewBounds.Right, viewBounds.Bottom);
                    }
                    else
                    {
                        path.AddLineToPoint(viewBounds.Right, viewBounds.Bottom - radiusBottomRight);
                        path.AddArcToPoint(viewBounds.Right, viewBounds.Bottom, viewBounds.Right - radiusBottomRight, viewBounds.Bottom, radiusBottomRight);
                    }

                    // Bottom border.

                    if (radiusBottomLeft == 0.0)
                    {
                        path.AddLineToPoint(viewBounds.Left, viewBounds.Bottom);
                    }
                    else
                    {
                        path.AddLineToPoint(viewBounds.Left + radiusBottomLeft, viewBounds.Bottom);
                        path.AddArcToPoint(viewBounds.Left, viewBounds.Bottom, viewBounds.Left, viewBounds.Bottom - radiusBottomLeft, radiusBottomLeft);
                    }

                    // Left border.

                    if (radiusTopLeft == 0.0)
                    {
                        path.AddLineToPoint(viewBounds.Left, viewBounds.Top);
                    }
                    else
                    {
                        path.AddLineToPoint(viewBounds.Left, viewBounds.Top + radiusTopLeft);
                        path.AddArcToPoint(viewBounds.Left, viewBounds.Top, viewBounds.Left + radiusTopLeft, viewBounds.Top, radiusTopLeft);
                    }

                    g.AddPath(path);
                    g.DrawPath(CGPathDrawingMode.Fill);
                }

                //-------------------------------------------------------------
                // We're going to draw the flat sides first.

                // Draw the left border.

                if (borderThickness.Left > 0.0)
                {
                    g.SetLineWidth((nfloat)borderThickness.Left);

                    g.MoveTo(borderBounds.Left, borderBounds.Top + radiusTopLeft);
                    g.AddLineToPoint(borderBounds.Left, borderBounds.Bottom - radiusBottomLeft);
                    g.StrokePath();
                }

                // Draw the top border.

                if (borderThickness.Top > 0.0)
                {
                    g.SetLineWidth((nfloat)borderThickness.Top);

                    g.MoveTo(borderBounds.Left + radiusTopLeft, borderBounds.Top);
                    g.AddLineToPoint(borderBounds.Right - radiusTopRight, borderBounds.Top);
                    g.StrokePath();
                }

                // Draw the right border.

                if (borderThickness.Right > 0.0)
                {
                    g.SetLineWidth((nfloat)borderThickness.Right);

                    g.MoveTo(borderBounds.Right, borderBounds.Top + radiusTopRight);
                    g.AddLineToPoint(borderBounds.Right, borderBounds.Bottom - radiusBottomRight);
                    g.StrokePath();
                }

                // Draw the bottom border.

                if (borderThickness.Bottom > 0.0)
                {
                    g.SetLineWidth((nfloat)borderThickness.Bottom);

                    g.MoveTo(borderBounds.Right - radiusBottomRight, borderBounds.Bottom);
                    g.AddLineToPoint(borderBounds.Left + radiusBottomLeft, borderBounds.Bottom);
                    g.StrokePath();
                }

                //-------------------------------------------------------------
                // Now draw the corners.  Note that we'll draw the corner if either
                // of the sides are drawn and we'll use the width of the thickest
                // side to draw the curve.

                // Top-left corner.

                if (radiusTopLeft > 0.0 && (borderThickness.Top > 0.0 || borderThickness.Left > 0.0))
                {
                    g.SetLineWidth((nfloat)Math.Max(borderThickness.Left, borderThickness.Top));

                    g.MoveTo(borderBounds.Left, borderBounds.Top + radiusTopLeft);
                    g.AddArcToPoint(borderBounds.Left, borderBounds.Top, borderBounds.Left + radiusTopLeft, borderBounds.Top, radiusTopLeft);
                    g.StrokePath();
                }

                // Top-right corner.

                if (radiusTopRight > 0.0 && (borderThickness.Top > 0.0 || borderThickness.Right > 0.0))
                {
                    g.SetLineWidth((nfloat)Math.Max(borderThickness.Top, borderThickness.Right));

                    g.MoveTo(borderBounds.Right - radiusTopRight, borderBounds.Top);
                    g.AddArcToPoint(borderBounds.Right, borderBounds.Top, borderBounds.Right, borderBounds.Top + radiusTopRight, radiusTopRight);
                    g.StrokePath();
                }

                // Bottom-right corner.

                if (radiusBottomRight > 0.0 && (borderThickness.Bottom > 0.0 || borderThickness.Right > 0.0))
                {
                    g.SetLineWidth((nfloat)Math.Max(borderThickness.Bottom, borderThickness.Right));

                    g.MoveTo(borderBounds.Right, borderBounds.Bottom - radiusBottomRight);
                    g.AddArcToPoint(borderBounds.Right, borderBounds.Bottom, borderBounds.Right - radiusBottomRight, borderBounds.Bottom, radiusBottomRight);
                    g.StrokePath();
                }

                // Bottom-left corner.

                if (radiusBottomLeft > 0.0 && (borderThickness.Bottom > 0.0 || borderThickness.Left > 0.0))
                {
                    g.SetLineWidth((nfloat)Math.Max(borderThickness.Bottom, borderThickness.Left));

                    g.MoveTo(borderBounds.Left + radiusBottomLeft, borderBounds.Bottom);
                    g.AddArcToPoint(borderBounds.Left, borderBounds.Bottom, borderBounds.Left, borderBounds.Bottom - radiusBottomLeft, radiusBottomLeft);
                    g.StrokePath();
                }
            }
        }

        //---------------------------------------------------------------------
        // Implementation

        /// <summary>
        /// Constructor.
        /// </summary>
        public BorderRenderer()
        {
        }

        /// <summary>
        /// Updates native control properties.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed or <c>null</c> to update all properties.</param>
        private void UpdateProperty(string propertyName = null)
        {
            switch (propertyName)
            {
                case null:
                case "Height":
                case "Width":
                case "BorderColor":
                case "BackgroundColor":
                case "BorderThickness":
                case "CornerRadius":

                    Control.SetNeedsDisplay();
                    break;
            }
        }

        /// <summary>
        /// Called by the base class when the associated portable class library
        /// view being rendered has changed.
        /// </summary>
        /// <param name="args">Describes the changes.</param>
        protected override void OnElementChanged(ElementChangedEventArgs<Border> args)
        {
            if (Control == null)
            {
                SetNativeControl(
                    new UIBorderView(Element)
                    {
                        BackgroundColor = UIColor.Clear
                    });
            }

            base.OnElementChanged(args);

            if (args.NewElement != null)
            {
                UpdateProperty();
            }
        }

        /// <summary>
        /// Handles view property changes.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event arguments.</param>
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            base.OnElementPropertyChanged(sender, args);
            UpdateProperty(args.PropertyName);
        }
    }
}
