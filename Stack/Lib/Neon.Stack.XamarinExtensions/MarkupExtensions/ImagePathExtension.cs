//-----------------------------------------------------------------------------
// FILE:        ImagePathExtension.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Implements a XAML markup extension that returns a platform specific
    /// path to an application image file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For Android and iOS, this extension does not modify the image path so that
    /// images will be obtained from their default locations (<b>$/Resources/drawable*</b> for
    /// Android and <b>$/Resource</b> for iOS).  For Windows Phone, <b>Assets/</b> will be
    /// prepended to the path.  This corrects the unforunate default behavior that requires
    /// images be located at the project root.
    /// </para>
    /// </remarks>
    [ContentProperty("Value")]
    public class ImagePathExtension : IMarkupExtension
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Used to obtain the platform specific image path from code.
        /// </summary>
        /// <param name="path">The target image's platform independent file name or path.</param>
        /// <returns>The platform specific resource path.</returns>
        public static FileImageSource Get(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            switch (Device.OS)
            {
                case TargetPlatform.Android:
                case TargetPlatform.iOS:
                default:

                    return (FileImageSource)ImageSource.FromFile(path);

                case TargetPlatform.WinPhone:

                    return (FileImageSource)ImageSource.FromFile("Assets/" + path);
            }
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ImagePathExtension()
        {
        }

        /// <summary>
        /// The target image's platform independent file name or path.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Converts the <see cref="Value"/> into a platform specific <see cref="ImageSource"/>.
        /// </summary>
        /// <param name="serviceProvider">The markup service provider.</param>
        /// <returns>The platform specific <see cref="ImageSource"/>.</returns>
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return Get(Value);
        }
    }
}
