//-----------------------------------------------------------------------------
// FILE:	    VirtualDirectoryInfo.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;

using Neon.Stack.Common;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.VirtualFileSystem;

using IOFileAttributes = System.IO.FileAttributes;
using ZipFileAttribues = ICSharpCode.SharpZipLib.VirtualFileSystem.FileAttributes;

namespace Neon.Stack.Zip
{
#pragma warning disable 1591

    internal class VirtualDirectoryInfo : IDirectoryInfo
    {
        public VirtualDirectoryInfo(string path)
        {
            try
            {
                var fileInfo   = new FileInfo(path);
                var attributes = fileInfo.Attributes;

                if ((attributes & IOFileAttributes.Archive) != 0)
                {
                    Attributes |= ZipFileAttribues.Archive;
                }

                if ((attributes & IOFileAttributes.Directory) != 0)
                {
                    Attributes |= ZipFileAttribues.Directory;
                }

                if ((attributes & IOFileAttributes.Hidden) != 0)
                {
                    Attributes |= ZipFileAttribues.Hidden;
                }

                if ((attributes & IOFileAttributes.Normal) != 0)
                {
                    Attributes |= ZipFileAttribues.Normal;
                }

                if ((attributes & IOFileAttributes.ReadOnly) != 0)
                {
                    Attributes |= ZipFileAttribues.ReadOnly;
                }

                CreationTime   = File.GetCreationTime(path);
                Exists         = true;
                LastAccessTime = File.GetLastAccessTime(path);
                LastWriteTime  = File.GetLastWriteTime(path);
                Name           = fileInfo.Name;
            }
            catch (IOException)
            {
                Exists = false;
            }
        }

        //---------------------------------------------------------------------
        // IDirectoryInfo implementation

        public ZipFileAttribues Attributes { get; private set; }
        public DateTime CreationTime { get; private set; }
        public bool Exists { get; private set; }
        public DateTime LastAccessTime { get; private set; }
        public DateTime LastWriteTime { get; private set; }
        public string Name { get; private set; }
    }
}
