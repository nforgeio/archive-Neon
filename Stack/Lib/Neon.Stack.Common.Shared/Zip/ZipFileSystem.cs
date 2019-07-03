//-----------------------------------------------------------------------------
// FILE:	    ZipFileSystem.cs
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
    /// <summary>
    /// Implements a virtual file system that portable <b>SharpZipLib</b> can use
    /// to access the device file system.  Call <see cref="Initialize()"/> at least
    /// once before attempting to use <b>SharpZipLib</b> functionaly that requires this.
    /// </summary>
    public class ZipFileSystem : IVirtualFileSystem
    {
        //---------------------------------------------------------------------
        // Static members

        private static bool isInitialized = false;

        /// <summary>
        /// Initializes the SharpZipLib virtual file system.
        /// </summary>
        public static void Initialize()
        {
            if (!isInitialized)
            {
                VFS.SetCurrent(new ZipFileSystem());
                isInitialized = true;
            }
        }

        //---------------------------------------------------------------------
        // IVirtualFileSystem implementation
        //
        // We can simply drop through to the standard .NET APIs because Xamarin
        // Android and iOS afre based on Mono which includes this support.

        #pragma warning disable 1591

        public string CurrentDirectory
        {
            get { return Directory.GetCurrentDirectory(); }
        }

        public char DirectorySeparatorChar
        {
            get { return Path.DirectorySeparatorChar; }
        }

        public void CopyFile(string fromFileName, string toFileName, bool overwrite)
        {
            File.Copy(fromFileName, toFileName, overwrite);
        }

        public void CreateDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
        }

        public VfsStream CreateFile(string filename)
        {
            // SharpZipLib does ensure that parent folders exist so we'll do this ourselves.

            Directory.CreateDirectory(Path.GetDirectoryName(filename));

            return new VfsProxyStream(new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite), filename);
        }

        public void DeleteFile(string fileName)
        {
            File.Delete(fileName);
        }

        public IEnumerable<string> GetDirectories(string directory)
        {
            return Directory.EnumerateDirectories(directory);
        }

        public IDirectoryInfo GetDirectoryInfo(string directoryName)
        {
            return new VirtualDirectoryInfo(directoryName);
        }

        public IFileInfo GetFileInfo(string filename)
        {
            return new VirtualFileInfo(filename);
        }

        public IEnumerable<string> GetFiles(string directory)
        {
            return Directory.EnumerateFiles(directory);
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        public string GetTempFileName()
        {
            return Path.GetTempFileName();
        }

        public void MoveFile(string fromFileName, string toFileName)
        {
            File.Move(fromFileName, toFileName);
        }

        public VfsStream OpenReadFile(string filename)
        {
            return new VfsProxyStream(new FileStream(filename, FileMode.Open, FileAccess.Read), filename);
        }

        public VfsStream OpenWriteFile(string filename)
        {
            return new VfsProxyStream(new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite), filename);
        }

        public void SetAttributes(string name, ZipFileAttribues attributes)
        {
            IOFileAttributes fileAttributes = (IOFileAttributes)0;

            if ((attributes & ZipFileAttribues.Archive) != 0)
            {
                fileAttributes = fileAttributes & IOFileAttributes.Archive;
            }

            if ((attributes & ZipFileAttribues.Directory) != 0)
            {
                fileAttributes = fileAttributes & IOFileAttributes.Directory;
            }

            if ((attributes & ZipFileAttribues.Hidden) != 0)
            {
                fileAttributes = fileAttributes & IOFileAttributes.Hidden;
            }

            if ((attributes & ZipFileAttribues.Normal) != 0)
            {
                fileAttributes = fileAttributes & IOFileAttributes.Normal;
            }

            if ((attributes & ZipFileAttribues.ReadOnly) != 0)
            {
                fileAttributes = fileAttributes & IOFileAttributes.ReadOnly;
            }

            File.SetAttributes(name, fileAttributes);
        }

        public void SetLastWriteTime(string name, DateTime dateTime)
        {
            File.SetLastWriteTime(name, dateTime);
        }
    }
}
