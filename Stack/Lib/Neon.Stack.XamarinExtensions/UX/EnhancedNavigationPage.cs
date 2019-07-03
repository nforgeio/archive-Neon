//-----------------------------------------------------------------------------
// FILE:        EnhancedNavigationPage.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Extends <see cref="NavigationPage"/> to provide additional functionality
    /// </summary>
    /// <remarks>
    /// <para>
    /// The class provides the static <see cref="Current"/> property which will be automatically
    /// set to the application's navigation page/controller if the application has one.
    /// </para>
    /// <para>
    /// A <b>Platform_TransparentTitleIcon.png</b> icon file must be included in the application's 
    /// platform host project in the standard folders (<b>Resources/drawable*</b> for Android, 
    /// <b>Resources</b> for iOS, and <b>Assets</b> for Windows Phone).
    /// </para>
    /// </remarks>
    public class EnhancedNavigationPage : NavigationPage
    {
        //---------------------------------------------------------------------
        // Static members

        private static FileImageSource titleIconSource = ImagePathExtension.Get("Platform_TransparentIcon.png");

        /// <summary>
        /// Returns the current navigation page/controller or <c>null</c>.
        /// </summary>
        public static EnhancedNavigationPage Current { get; private set; }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Default onstructor.
        /// </summary>
        public EnhancedNavigationPage()
        {
            Initialize();
        }

        /// <summary>
        /// Constructs a navigation page initialized to host a specified root page.
        /// </summary>
        /// <param name="rootPage">The root page.</param>
        public EnhancedNavigationPage(Page rootPage)
            : base(rootPage)
        {
            Initialize(rootPage);
        }

        /// <summary>
        /// Initializes the control.
        /// </summary>
        private void Initialize(Page rootPage = null)
        {
            EnhancedNavigationPage.Current = this;

            // The Xamarin.Forms Android and iOS implementations include a title
            // with the back button as well as an application icon for Andropid.
            // Both look weird.  We're going to intercept page push/pop and disable 
            // both of these.

            if (rootPage != null)
            {
                if (DeviceHelper.Platform == TargetPlatform.Android)
                {
                    SetTitleIcon(rootPage, titleIconSource);
                }

                SetBackButtonTitle(rootPage, string.Empty);
            }

            Pushed +=
                (s, a) =>
                {
                    if (DeviceHelper.Platform == TargetPlatform.Android)
                    {
                        SetTitleIcon(a.Page, titleIconSource);
                    }

                    SetBackButtonTitle(a.Page, string.Empty);
                };

            Popped +=
                (s, a) =>
                {
                    if (DeviceHelper.Platform == TargetPlatform.Android)
                    {
                        SetTitleIcon(a.Page, titleIconSource);
                    }

                    SetBackButtonTitle(a.Page, string.Empty);
                };
        }
    }
}
