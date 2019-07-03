//-----------------------------------------------------------------------------
// FILE:        BorderRenderer.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables.Shapes;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using AndroidView = Android.Views.View;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using Neon.Stack.XamarinExtensions;
using Neon.Stack.XamarinExtensions.Droid;

// $todo(jeff.lill):
//
// We have some rendering issues with borders with square corners and 
// and variable width borders.  The corners don't come out square.

[assembly: ExportRenderer(typeof(Border), typeof(BorderRenderer))]

namespace Neon.Stack.XamarinExtensions.Droid
{
    /// <summary>
    /// Implements the Android renderer for the <see cref="Border"/> control.
    /// </summary>
    public class BorderRenderer : ViewRenderer<Border, BorderRenderer.DroidBorderView>
    {
        //---------------------------------------------------------------------
        // Private-ish types

        // Note: 
        //
        // The basic approach here is to draw the border in one or more parts,
        // where each part includes a Paint instance specifying the stroke and
        // fill properties and a Path instance specifying the part's shape.
        //
        // The border parts will be regenerated whenever the view size changes 
        // or when relevant properties of the associated portable view change
        // and then OnDraw() is called.
        //
        // We're specifically not regenerating the parts within OnDraw() because
        // it's best practice not to, due to performance concerns.

        /// <summary>
        /// Holds the <see cref="Path"/> and <see cref="Paint"/> objects to be used
        /// to draw a portion of the border.
        /// </summary>
        private struct BorderPart
        {
            public Path     Path;
            public Paint    Paint;
        }

        /// <summary>
        /// Implements the underlying Android view.
        /// </summary>
        public class DroidBorderView : AndroidView
        {
            private Border              element;
            private bool                isDirty;
            private List<BorderPart>    borderParts;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="context">Information about the global application environment.</param>
            /// <param name="element">The associated portable element.</param>
            public DroidBorderView(Context context, Border element)
                : base(context)
            {
                this.element     = element;
                this.isDirty     = false;
                this.borderParts = null;
            }

            /// <summary>
            /// Called when the view's size changes.
            /// </summary>
            /// <param name="w">The new width.</param>
            /// <param name="h">The new height.</param>
            /// <param name="oldw">The original width.</param>
            /// <param name="oldh">The original height.</param>
            protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
            {
                base.OnSizeChanged(w, h, oldw, oldh);

                isDirty = true;
                Invalidate();
            }

            /// <summary>
            /// Called when the associated portable view detects a relevant
            /// propetly change.
            /// </summary>
            internal void OnElementPropertyChanged()
            {
                isDirty = true;
                Invalidate();
            }

            /// <summary>
            /// Called to draw the view.
            /// </summary>
            /// <param name="canvas">The canvas.</param>
            protected override void OnDraw(Canvas canvas)
            {
                if (isDirty)
                {
                    GenerateParts();
                    isDirty = false;
                }

                if (borderParts == null)
                {
                    return;
                }

                foreach (var part in borderParts)
                {
                    canvas.DrawPath(part.Path, part.Paint);
                }
            }

            /// <summary>
            /// Generates the visual border parts based on the current size of the
            /// view and the associated portal view's properties.
            /// </summary>
            private void GenerateParts()
            {
                Invalidate();

                if (Height == 0 || Width == 0)
                {
                    // Don't draw anything in these cases.

                    borderParts = null;
                    return;
                }

                // $hack(jeff.lill):
                //
                // Android seems to draw corners at half the radius for some reason.
                // I'm pretty sure my rendering code below is correct.  I'm going to 
                // double the corner metrics to compensate.

                var cornerRadius     = new CornerRadius(element.CornerRadius.TopLeft * 2.0,
                                                        element.CornerRadius.TopRight * 2.0,
                                                        element.CornerRadius.BottomRight * 2.0,
                                                        element.CornerRadius.BottomLeft * 2.0);

                var viewBounds       = new RectF(0, 0, Width, Height);
                var roundedCorners   = cornerRadius.TopLeft != 0.0 ||
                                       cornerRadius.TopRight != 0.0 ||
                                       cornerRadius.BottomRight != 0.0 ||
                                       cornerRadius.BottomLeft != 0.0;
                var borderThickness  = element.BorderThickness;
                var fixedThickness   = borderThickness.Left == borderThickness.Top &&
                                       borderThickness.Left == borderThickness.Right &&
                                       borderThickness.Left == borderThickness.Bottom;

                // We need to inset the bounds by 1/2 the thickness of each side so
                // the lines will be drawn completely within the view boundry.  This
                // is necessary because iOS draws thick lines centered on the coordinates.
                //
                // Without this, the flat sides of the borders will be clipped and the
                // corners will look weirdly thick.

                RectF borderBounds;

                if (fixedThickness)
                {
                    borderBounds = new RectF(viewBounds.Left + (float)(borderThickness.Left / 2),
                                             viewBounds.Top + (float)(borderThickness.Top / 2),
                                             viewBounds.Width() - (float)(borderThickness.Left + borderThickness.Top) / 2,
                                             viewBounds.Height() - (float)(borderThickness.Top + borderThickness.Bottom) / 2);
                }
                else
                {
                    var leftInset   = Max(borderThickness.Bottom, borderThickness.Left, borderThickness.Top) / 2;
                    var topInset    = Max(borderThickness.Left, borderThickness.Top, borderThickness.Right) / 2;
                    var rightInset  = Max(borderThickness.Top, borderThickness.Right, borderThickness.Bottom) / 2;
                    var bottomInset = Max(borderThickness.Right, borderThickness.Bottom, borderThickness.Left) / 2;

                    borderBounds = new RectF(viewBounds.Left + leftInset,
                                             viewBounds.Top + topInset,
                                             viewBounds.Width() - (leftInset + rightInset),
                                             viewBounds.Height() - (topInset + bottomInset));
                }

                // Don't draw anything for empty rectangles.

                if (borderBounds.Left >= borderBounds.Right ||
                    borderBounds.Top >= borderBounds.Bottom)
                {
                    return;
                }

                // This is a bit tricky because we need to handle various combinations
                // of corner radii and border thicknesses.

                if (roundedCorners)
                {
                    if (fixedThickness)
                    {
                        GenerateRoundedFixedThickness(borderBounds, borderThickness.Left, cornerRadius);
                    }
                    else
                    {
                        GenerateRoundedVariableThickness(borderBounds, borderThickness, cornerRadius);
                    }
                }
                else
                {
                    if (fixedThickness)
                    {
                        GenerateSquareFixedThickness(borderBounds, borderThickness.Left);
                    }
                    else
                    {
                        GenerateSquareVariableThickness(borderBounds, borderThickness);
                    }
                }
            }

            /// <summary>
            /// Returns the maximum of a set of non-negative values.
            /// </summary>
            /// <param name="values">The values.</param>
            /// <returns>The maximum.</returns>
            private static float Max(params double[] values)
            {
                var max = 0.0;

                foreach (var value in values)
                {
                    if (value > max)
                    {
                        max = value;
                    }
                }

                return (float)max;
            }

            /// <summary>
            /// Generates border parts for a rectangle with square corners and a border with a consistent width 
            /// for all sides.
            /// </summary>
            /// <param name="borderBounds">The border rectangle.</param>
            /// <param name="lineWidth">The border width.</param>
            private void GenerateSquareFixedThickness(RectF borderBounds, double lineWidth)
            {
                borderParts = new List<BorderPart>();

                // Generate the border path.

                var path = new Path();

                path.MoveTo(borderBounds.Left, borderBounds.Top);
                path.LineTo(borderBounds.Right, borderBounds.Top);
                path.LineTo(borderBounds.Right, borderBounds.Bottom);
                path.LineTo(borderBounds.Left, borderBounds.Bottom);
                path.LineTo(borderBounds.Top, borderBounds.Left);

                // Generate the fill part first, unless the background color is transparent.

                if (element.BackgroundColor.A != 0.0)
                {
                    var fillPaint = new Paint();

                    fillPaint.SetStyle(Paint.Style.Fill);

                    fillPaint.AntiAlias = true;
                    fillPaint.Color     = ConvertToPlatform.Color(element.BackgroundColor);

                    borderParts.Add(new BorderPart() { Paint = fillPaint, Path = path });
                }

                // Generate the stroke part, unless the border color is transparent or the thickness is 0.

                if (element.BorderColor.A != 0.0 && lineWidth > 0.0)
                {
                    var strokePaint = new Paint();

                    strokePaint.SetStyle(Paint.Style.Stroke);

                    strokePaint.AntiAlias   = true;
                    strokePaint.Color       = ConvertToPlatform.Color(element.BorderColor);
                    strokePaint.StrokeWidth = (float)lineWidth;

                    borderParts.Add(new BorderPart() { Paint = strokePaint, Path = path });
                }
            }

            /// <summary>
            /// Generates border parts for a rectangle with square corners and with sides with differing
            /// border thicknesses.
            /// </summary>
            /// <param name="borderBounds">The border rectangle.</param>
            /// <param name="borderThickness">The border thickness.</param>
            private void GenerateSquareVariableThickness(RectF borderBounds, Thickness borderThickness)
            {
                borderParts = new List<BorderPart>();

                // Generate the border path.

                var path = new Path();

                path.MoveTo(borderBounds.Left, borderBounds.Top);
                path.LineTo(borderBounds.Right, borderBounds.Top);
                path.LineTo(borderBounds.Right, borderBounds.Bottom);
                path.LineTo(borderBounds.Left, borderBounds.Bottom);
                path.LineTo(borderBounds.Top, borderBounds.Left);

                // Generate the fill part first, unless the background color is transparent.

                if (element.BackgroundColor.A != 0.0)
                {
                    var fillPaint = new Paint();

                    fillPaint.SetStyle(Paint.Style.Fill);

                    fillPaint.AntiAlias = true;
                    fillPaint.Color     = ConvertToPlatform.Color(element.BackgroundColor);

                    borderParts.Add(new BorderPart() { Paint = fillPaint, Path = path });
                }

                // Generate the non-zero width borders, unless the border color is transparent.

                if (element.BorderColor.A != 0.0)
                {
                    var borderColor = ConvertToPlatform.Color(element.BorderColor);

                    // Left border.

                    if (element.BorderThickness.Left > 0.0)
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)element.BorderThickness.Left;
                        strokePaint.StrokeCap   = Paint.Cap.Butt;

                        var strokePath = new Path();

                        strokePath.MoveTo(borderBounds.Left, borderBounds.Top);
                        strokePath.LineTo(borderBounds.Left, borderBounds.Bottom);

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }

                    // Top border.

                    if (element.BorderThickness.Top > 0.0)
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)element.BorderThickness.Top;
                        strokePaint.StrokeCap   = Paint.Cap.Butt;

                        var strokePath = new Path();

                        strokePath.MoveTo(borderBounds.Left, borderBounds.Top);
                        strokePath.LineTo(borderBounds.Right, borderBounds.Top);

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }

                    // Right border.

                    if (element.BorderThickness.Right > 0.0)
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)element.BorderThickness.Right;
                        strokePaint.StrokeCap   = Paint.Cap.Butt;

                        var strokePath = new Path();

                        strokePath.MoveTo(borderBounds.Right, borderBounds.Top);
                        strokePath.LineTo(borderBounds.Right, borderBounds.Bottom);

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }

                    // Bottom border.

                    if (element.BorderThickness.Bottom > 0.0)
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)element.BorderThickness.Bottom;
                        strokePaint.StrokeCap   = Paint.Cap.Butt;

                        var strokePath = new Path();

                        strokePath.MoveTo(borderBounds.Left, borderBounds.Bottom);
                        strokePath.LineTo(borderBounds.Bottom, borderBounds.Bottom);

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }
                }
            }

            /// <summary>
            /// Generates border parts for a rectangle with rounded corners, a border with a 
            /// consistent thickness for all sides.
            /// </summary>
            /// <param name="borderBounds">The border rectangle.</param>
            /// <param name="lineWidth">The border width.</param>
            /// <param name="cornerRadius">The corner radii.</param>
            private void GenerateRoundedFixedThickness(RectF borderBounds, double lineWidth, CornerRadius cornerRadius)
            {
                var radiusTopLeft     = (float)cornerRadius.TopLeft;
                var radiusTopRight    = (float)cornerRadius.TopRight;
                var radiusBottomRight = (float)cornerRadius.BottomRight;
                var radiusBottomLeft  = (float)cornerRadius.BottomLeft;

                borderParts = new List<BorderPart>();

                //-------------------------------------------------------------
                // Generate the border path.

                var path = new Path();

                // Top border.

                path.MoveTo(radiusTopLeft == 0.0 ? borderBounds.Left : borderBounds.Left + radiusTopLeft, borderBounds.Top);

                if (radiusTopRight == 0.0)
                {
                    path.LineTo(borderBounds.Right, borderBounds.Top);
                }
                else
                {
                    path.LineTo(borderBounds.Right - radiusTopRight, borderBounds.Top);

                    var cornerRect = new RectF(borderBounds.Right - 2 * radiusTopRight, borderBounds.Top, borderBounds.Right, borderBounds.Top + 2 * radiusTopRight);

                    path.ArcTo(cornerRect, 270F, 90F);
                }

                // Right border.

                if (radiusBottomRight == 0.0)
                {
                    path.LineTo(borderBounds.Right, borderBounds.Bottom);
                }
                else
                {
                    path.LineTo(borderBounds.Right, borderBounds.Bottom - radiusBottomRight);

                    var cornerRect = new RectF(borderBounds.Right - 2 * radiusTopRight, borderBounds.Bottom - 2 * radiusBottomRight, borderBounds.Right, borderBounds.Bottom);

                    path.ArcTo(cornerRect, 0F, 90F);
                }

                // Bottom border.

                if (radiusBottomLeft == 0.0)
                {
                    path.LineTo(borderBounds.Left, borderBounds.Bottom);
                }
                else
                {
                    path.LineTo(borderBounds.Left + radiusBottomLeft, borderBounds.Bottom);

                    var cornerRect = new RectF(borderBounds.Left, borderBounds.Bottom - 2 * radiusBottomLeft, borderBounds.Left + 2 * radiusBottomLeft, borderBounds.Bottom);

                    path.ArcTo(cornerRect, 90F, 90F);
                }

                // Left border.

                if (radiusTopLeft == 0.0)
                {
                    path.LineTo(borderBounds.Left, borderBounds.Top);
                }
                else
                {
                    path.LineTo(borderBounds.Left, borderBounds.Top + radiusTopLeft);

                    var cornerRect = new RectF(borderBounds.Left, borderBounds.Top, borderBounds.Left + 2 * radiusTopLeft, borderBounds.Top + 2 * radiusTopLeft);

                    path.ArcTo(cornerRect, 180F, 90F);
                }

                //-------------------------------------------------------------
                // Generate the fill part first, unless the background color is transparent.

                if (element.BackgroundColor.A != 0.0)
                {
                    var fillPaint = new Paint();

                    fillPaint.SetStyle(Paint.Style.Fill);

                    fillPaint.AntiAlias = true;
                    fillPaint.Color     = ConvertToPlatform.Color(element.BackgroundColor);

                    borderParts.Add(new BorderPart() { Paint = fillPaint, Path = path });
                }

                //-------------------------------------------------------------
                // Generate the stroke part, unless the border color is transparent or the thickness is 0.

                if (element.BorderColor.A != 0.0 && lineWidth > 0.0)
                {
                    var strokePaint = new Paint();

                    strokePaint.SetStyle(Paint.Style.Stroke);

                    strokePaint.AntiAlias   = true;
                    strokePaint.Color       = ConvertToPlatform.Color(element.BorderColor);
                    strokePaint.StrokeWidth = (float)lineWidth;
                    strokePaint.StrokeJoin  = Paint.Join.Miter;

                    borderParts.Add(new BorderPart() { Paint = strokePaint, Path = path });
                }
            }

            /// <summary>
            /// Generates border parts for a rectangle with rounded corners and with sides with differing
            /// border thicknesses.
            /// </summary>
            /// <param name="borderBounds">The border rectangle.</param>
            /// <param name="borderThickness">The border thickness.</param>
            /// <param name="cornerRadius">The corner radii.</param>
            private void GenerateRoundedVariableThickness(RectF borderBounds, Thickness borderThickness, CornerRadius cornerRadius)
            {
                var radiusTopLeft     = (float)cornerRadius.TopLeft;
                var radiusTopRight    = (float)cornerRadius.TopRight;
                var radiusBottomRight = (float)cornerRadius.BottomRight;
                var radiusBottomLeft  = (float)cornerRadius.BottomLeft;

                borderParts = new List<BorderPart>();

                //-------------------------------------------------------------
                // Generate the border path.

                var path = new Path();

                // Top border.

                path.MoveTo(radiusTopLeft == 0.0 ? borderBounds.Left : borderBounds.Left + radiusTopLeft, borderBounds.Top);

                if (radiusTopRight == 0.0)
                {
                    path.LineTo(borderBounds.Right, borderBounds.Top);
                }
                else
                {
                    path.LineTo(borderBounds.Right - radiusTopRight, borderBounds.Top);

                    var cornerRect = new RectF(borderBounds.Right - 2 * radiusTopRight, borderBounds.Top, borderBounds.Right, borderBounds.Top + 2 * radiusTopRight);

                    path.ArcTo(cornerRect, 270F, 90F);
                }

                // Right border.

                if (radiusBottomRight == 0.0)
                {
                    path.LineTo(borderBounds.Right, borderBounds.Bottom);
                }
                else
                {
                    path.LineTo(borderBounds.Right, borderBounds.Bottom - radiusBottomRight);

                    var cornerRect = new RectF(borderBounds.Right - 2 * radiusTopRight, borderBounds.Bottom - 2 * radiusBottomRight, borderBounds.Right, borderBounds.Bottom);

                    path.ArcTo(cornerRect, 0F, 90F);
                }

                // Bottom border.

                if (radiusBottomLeft == 0.0)
                {
                    path.LineTo(borderBounds.Left, borderBounds.Bottom);
                }
                else
                {
                    path.LineTo(borderBounds.Left + radiusBottomLeft, borderBounds.Bottom);

                    var cornerRect = new RectF(borderBounds.Left, borderBounds.Bottom - 2 * radiusBottomLeft, borderBounds.Left + 2 * radiusBottomLeft, borderBounds.Bottom);

                    path.ArcTo(cornerRect, 90F, 90F);
                }

                // Left border.

                if (radiusTopLeft == 0.0)
                {
                    path.LineTo(borderBounds.Left, borderBounds.Top);
                }
                else
                {
                    path.LineTo(borderBounds.Left, borderBounds.Top + radiusTopLeft);

                    var cornerRect = new RectF(borderBounds.Left, borderBounds.Top, borderBounds.Left + 2 * radiusTopLeft, borderBounds.Top + 2 * radiusTopLeft);

                    path.ArcTo(cornerRect, 180F, 90F);
                }

                //-------------------------------------------------------------
                // Generate the fill part first, unless the background color is transparent.

                if (element.BackgroundColor.A != 0.0)
                {
                    var fillPaint = new Paint();

                    fillPaint.SetStyle(Paint.Style.Fill);

                    fillPaint.AntiAlias = true;
                    fillPaint.Color     = ConvertToPlatform.Color(element.BackgroundColor);

                    borderParts.Add(new BorderPart() { Paint = fillPaint, Path = path });
                }

                if (element.BorderColor.A != 0.0)
                {
                    var borderColor = ConvertToPlatform.Color(element.BorderColor);

                    //---------------------------------------------------------
                    // Generate the stroke parts for the flat sides.

                    // Left border

                    if (element.BorderThickness.Left > 0.0)
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)element.BorderThickness.Left;
                        strokePaint.StrokeJoin  = Paint.Join.Miter;

                        var strokePath = new Path();

                        if (radiusTopLeft == 0.0)
                        {
                            strokePath.MoveTo(borderBounds.Left, borderBounds.Top);
                        }
                        else
                        {
                            strokePath.MoveTo(borderBounds.Left, borderBounds.Top + radiusTopLeft);
                        }

                        if (radiusBottomLeft == 0.0)
                        {
                            strokePath.LineTo(borderBounds.Left, borderBounds.Bottom);
                        }
                        else
                        {
                            strokePath.LineTo(borderBounds.Left, borderBounds.Bottom - radiusBottomLeft);
                        }

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }

                    // Top border

                    if (element.BorderThickness.Top > 0.0)
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)element.BorderThickness.Top;
                        strokePaint.StrokeJoin  = Paint.Join.Miter;

                        var strokePath = new Path();

                        if (radiusTopLeft == 0.0)
                        {
                            strokePath.MoveTo(borderBounds.Left, borderBounds.Top);
                        }
                        else
                        {
                            strokePath.MoveTo(borderBounds.Left + radiusTopLeft, borderBounds.Top);
                        }

                        if (radiusTopRight == 0.0)
                        {
                            strokePath.LineTo(borderBounds.Right, borderBounds.Top);
                        }
                        else
                        {
                            strokePath.LineTo(borderBounds.Right - radiusTopRight, borderBounds.Top);
                        }

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }

                    // Right border

                    if (element.BorderThickness.Right > 0.0)
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)element.BorderThickness.Right;
                        strokePaint.StrokeJoin  = Paint.Join.Miter;

                        var strokePath = new Path();

                        if (radiusTopRight == 0.0)
                        {
                            strokePath.MoveTo(borderBounds.Right, borderBounds.Top);
                        }
                        else
                        {
                            strokePath.MoveTo(borderBounds.Right, borderBounds.Top + radiusTopLeft);
                        }

                        if (radiusBottomRight == 0.0)
                        {
                            strokePath.LineTo(borderBounds.Right, borderBounds.Bottom);
                        }
                        else
                        {
                            strokePath.LineTo(borderBounds.Right, borderBounds.Bottom - radiusBottomRight);
                        }

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }

                    // Bottom border

                    if (element.BorderThickness.Bottom > 0.0)
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)element.BorderThickness.Bottom;
                        strokePaint.StrokeJoin  = Paint.Join.Miter;

                        var strokePath = new Path();

                        if (radiusBottomRight == 0.0)
                        {
                            strokePath.MoveTo(borderBounds.Right, borderBounds.Bottom);
                        }
                        else
                        {
                            strokePath.MoveTo(borderBounds.Right - radiusBottomRight, borderBounds.Bottom);
                        }

                        if (radiusBottomLeft == 0.0)
                        {
                            strokePath.LineTo(borderBounds.Left, borderBounds.Bottom);
                        }
                        else
                        {
                            strokePath.LineTo(borderBounds.Left + radiusBottomLeft, borderBounds.Bottom);
                        }

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }

                    //---------------------------------------------------------
                    // Generate the stroke parts for the corners.  Note that we'll draw the 
                    // corner if either of the sides are drawn and we'll use the width of 
                    // the thickest side to draw the curve.

                    // Top-left corner.

                    if (radiusTopLeft > 0.0 && (borderThickness.Top > 0.0 || borderThickness.Left > 0.0))
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)Math.Max(element.BorderThickness.Top, element.BorderThickness.Left);
                        strokePaint.StrokeJoin  = Paint.Join.Miter;

                        var strokePath = new Path();
                        var cornerRect = new RectF(borderBounds.Left, borderBounds.Top, borderBounds.Left + 2 * radiusTopLeft, borderBounds.Top + 2 * radiusTopLeft);

                        strokePath.MoveTo(borderBounds.Left, borderBounds.Top + radiusTopLeft);
                        strokePath.ArcTo(cornerRect, 180, 90F);

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }

                    // Top-right corner.

                    if (radiusTopRight > 0.0 && (borderThickness.Top > 0.0 || borderThickness.Right > 0.0))
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)Math.Max(element.BorderThickness.Top, element.BorderThickness.Right);
                        strokePaint.StrokeJoin  = Paint.Join.Miter;

                        var strokePath = new Path();
                        var cornerRect = new RectF(borderBounds.Right - 2 * radiusTopRight, borderBounds.Top, borderBounds.Right, borderBounds.Top + 2 * radiusTopRight);

                        strokePath.MoveTo(borderBounds.Right - radiusTopRight, borderBounds.Top);
                        strokePath.ArcTo(cornerRect, 270F, 90F);

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }

                    // Bottom-right corner.

                    if (radiusTopRight > 0.0 && (borderThickness.Bottom > 0.0 || borderThickness.Right > 0.0))
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)Math.Max(element.BorderThickness.Bottom, element.BorderThickness.Right);
                        strokePaint.StrokeJoin  = Paint.Join.Miter;

                        var strokePath = new Path();
                        var cornerRect = new RectF(borderBounds.Right - 2 * radiusBottomRight, borderBounds.Bottom - 2 * radiusBottomRight, borderBounds.Right, borderBounds.Bottom);

                        strokePath.MoveTo(borderBounds.Right, borderBounds.Bottom - radiusBottomRight);
                        strokePath.ArcTo(cornerRect, 0F, 90F);

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }

                    // Bottom-left corner.

                    if (radiusTopRight > 0.0 && (borderThickness.Bottom > 0.0 || borderThickness.Left > 0.0))
                    {
                        var strokePaint = new Paint();

                        strokePaint.SetStyle(Paint.Style.Stroke);

                        strokePaint.AntiAlias   = true;
                        strokePaint.Color       = borderColor;
                        strokePaint.StrokeWidth = (float)Math.Max(element.BorderThickness.Bottom, element.BorderThickness.Left);
                        strokePaint.StrokeJoin  = Paint.Join.Miter;

                        var strokePath = new Path();
                        var cornerRect = new RectF(borderBounds.Left, borderBounds.Bottom - 2 * radiusBottomLeft, borderBounds.Left + 2 * radiusBottomLeft, borderBounds.Bottom);

                        strokePath.MoveTo(borderBounds.Left + radiusBottomLeft, borderBounds.Bottom);
                        strokePath.ArcTo(cornerRect, 90F, 90F);

                        borderParts.Add(new BorderPart() { Paint = strokePaint, Path = strokePath });
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Implementation.

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
                case "BorderColor":
                case "BackgroundColor":
                case "BorderThickness":
                case "CornerRadius":

                    Control.OnElementPropertyChanged();
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
                SetNativeControl(new DroidBorderView(Context, Element));
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