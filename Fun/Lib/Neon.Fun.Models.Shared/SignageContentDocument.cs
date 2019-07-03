//-----------------------------------------------------------------------------
// FILE:	    SignageContentDocument.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Diagnostics.Contracts;
using System.IO;

using Neon.Stack.Common;
using Neon.Stack.Data;
using Neon.Stack.IO;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;

namespace Neon.Fun.Signage
{
    /// <summary>
    /// Manages digital signage content, adding capabilities to the generated
    /// model document.
    /// </summary>
    public partial class SignageContentDocument
    {
        private FastZip GetZipper()
        {
            return new FastZip()
            {
                CreateEmptyDirectories = true,
                UseZip64               = UseZip64.On
            };
        }

        /// <summary>
        /// Recursively zips files and folder into the document's <b>package</b> attachment.
        /// </summary>
        /// <param name="sourceFolder">The source folder path.</param>
        public void Zip(string sourceFolder)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(sourceFolder));

            using (var stream = new BlockStream())
            {
                GetZipper().CreateZip(stream, sourceFolder, true, null, null);

                stream.Position = 0;
                SetPackage(stream, FunContentTypes.Zip);
            }
        }

        /// <summary>
        /// Recursively unzips the files and folders from the document's <b>package</b> attachment.
        /// </summary>
        /// <param name="targetFolder">The destination folder path.</param>
        public void Unzip(string targetFolder)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(targetFolder));

            using (var stream = new FileStream(Package, FileMode.Open, FileAccess.Read))
            {
                GetZipper().ExtractZip(stream, targetFolder, FastZip.Overwrite.Always, null, null, null, true, false);
            }
        }
    }
}
