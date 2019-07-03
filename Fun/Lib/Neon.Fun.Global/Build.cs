//-----------------------------------------------------------------------------
// FILE:	    Build.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Fun
{
    /// <summary>
    /// NeonStack build constants.
    /// </summary>
    internal static partial class Build
    {
        /// <summary>
        /// The build version for all NeonStack assemblies.
        /// </summary>
        public const string Version = "0.1.0.0";

        /// <summary>
        /// The company name to use for all LillTek assemblies.
        /// </summary>
        public const string Company = "Neon Research";

        /// <summary>
        /// The copyright statement to be included in all assemblies.
        /// </summary>
        public const string Copyright = "Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved..  All rights reserved.";

        /// <summary>
        /// The product statement to be included in all assemblies.
        /// </summary>
        public const string Product = "NeonFun";

        /// <summary>
        /// The build configuration.
        /// </summary>
        public const string Configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif
    }
}
