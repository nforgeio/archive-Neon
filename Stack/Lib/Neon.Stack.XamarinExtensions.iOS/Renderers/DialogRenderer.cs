//-----------------------------------------------------------------------------
// FILE:        DialogRenderer.cs
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

[assembly: ExportRenderer(typeof(Dialog), typeof(DialogRenderer))]

namespace Neon.Stack.XamarinExtensions.iOS
{
    /// <summary>
    /// Extends <see cref="PageRenderer"/> to capture the page's <see cref="UIViewController"/>.
    /// </summary>
    /// <remarks>
    /// This class captures the page's associated <see cref="UIViewController"/> and assigns it to
    /// the <see cref="Dialog.iOSViewController"/> property.  This all is a hack to make this
    /// controller available to the ZXing QR/Barcode scanner so it will function when a modal
    /// dialog is displayed.
    /// </remarks>
    public class DialogRenderer : PageRenderer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DialogRenderer()
        {
        }

        /// <summary>
        /// Called when the associated portal element changes.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected override void OnElementChanged(VisualElementChangedEventArgs args)
        {
            base.OnElementChanged(args);

            ((Dialog)Element).iOSViewController = this.ViewController;
        }
    }
}
