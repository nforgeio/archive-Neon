//-----------------------------------------------------------------------------
// FILE:	    NeonHelper.OS.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
#if NETCORE
using System.Runtime.InteropServices;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neon.Stack.Common
{
    public static partial class NeonHelper
    {
        private static bool osChecked;
        private static bool isWindows;
        private static bool isLinux;
        private static bool isOSX;

        /// <summary>
        /// Detects the current operating system.
        /// </summary>
        private static void DetectOS()
        {
            if (osChecked)
            {
                return;     // Already did a detect
            }

            try
            {
#if XAMARIN
                // $todo(jeff.lill):
                //
                // Need to figure out a way to determine this for portable 
                // libraries running on Xamarin.  One possible way is
                // to have the common Xamarin libraries fill this in when
                // they initialize or use some kind of dependency injection.

                isWindows = false;
                isLinux   = false;
                isOSX     = false;
#elif NETCORE
                isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                isLinux   = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                isOSX     = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#else
                // Detect the base operating system.

                switch ((int)Environment.OSVersion.Platform)
                {
                    case 4:
                    case 128:

                        isWindows = false;
                        isLinux   = true;
                        isOSX     = false;
                        break;

                    case (int)PlatformID.MacOSX:

                        isWindows = false;
                        isLinux   = false;
                        isOSX     = true;
                        break;

                    default:

                        isWindows = true;
                        isLinux   = false;
                        isOSX     = false;
                        break;
                }
#endif
            }

            finally
            {
                // Set the global to true so we won't test again.

                osChecked = true;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the application is running on a Windows variant
        /// operating system.
        /// </summary>
        public static bool IsWindows
        {
            get
            {
                if (osChecked)
                {
                    return isWindows;
                }

                DetectOS();
                return isWindows;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the application is running on a Linux variant
        /// operating system.
        /// </summary>
        public static bool IsLinux
        {
            get
            {
                if (osChecked)
                {
                    return isLinux;
                }

                DetectOS();
                return isLinux;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the application is running on Max OSX.
        /// </summary>
        public static bool IsOSX
        {
            get
            {
                if (osChecked)
                {
                    return isOSX;
                }

                DetectOS();
                return isOSX;
            }
        }
    }
}
