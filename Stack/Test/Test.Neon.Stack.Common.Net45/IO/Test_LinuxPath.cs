﻿//-----------------------------------------------------------------------------
// FILE:	    Test_LinuxPath.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Stack.Common;
using Neon.Stack.IO;

using Xunit;

namespace TestCommon
{
    public class Test_LinuxPath
    {
        [Fact]
        public void ChangeExtension()
        {
            Assert.Equal("test.one", LinuxPath.ChangeExtension("test", "one"));
            Assert.Equal("test.one", LinuxPath.ChangeExtension("test.zero", "one"));
            Assert.Equal("/foo/test.one", LinuxPath.ChangeExtension("\\foo\\test", "one"));
            Assert.Equal("/foo/test.one", LinuxPath.ChangeExtension("\\foo\\test.zero", "one"));
        }

        [Fact]
        public void Combine()
        {
            Assert.Equal("/one/two/three.txt", LinuxPath.Combine("/one", "two", "three.txt"));
            Assert.Equal("/one/two/three.txt", LinuxPath.Combine("\\one", "two", "three.txt"));
        }

        [Fact]
        public void GetDirectoryName()
        {
            Assert.Equal("/one/two", LinuxPath.GetDirectoryName("\\one\\two\\three.txt"));
            Assert.Equal("/one/two", LinuxPath.GetDirectoryName("/one/two/three.txt"));
        }

        [Fact]
        public void GetExtension()
        {
            Assert.Equal(".txt", LinuxPath.GetExtension("\\one\\two\\three.txt"));
            Assert.Equal(".txt", LinuxPath.GetExtension("/one/two/three.txt"));
        }

        [Fact]
        public void GetFileName()
        {
            Assert.Equal("three.txt", LinuxPath.GetFileName("\\one\\two\\three.txt"));
            Assert.Equal("three.txt", LinuxPath.GetFileName("/one/two/three.txt"));
        }

        [Fact]
        public void GetFileNameWithoutExtension()
        {
            Assert.Equal("three", LinuxPath.GetFileNameWithoutExtension("\\one\\two\\three.txt"));
            Assert.Equal("three", LinuxPath.GetFileNameWithoutExtension("/one/two/three.txt"));
        }

        [Fact]
        public void HasExtension()
        {
            Assert.True(LinuxPath.HasExtension("\\one\\two\\three.txt"));
            Assert.True(LinuxPath.HasExtension("/one/two/three.txt"));

            Assert.False(LinuxPath.HasExtension("\\one\\two\\three"));
            Assert.False(LinuxPath.HasExtension("/one/two/three"));
        }

        [Fact]
        public void IsPathRooted()
        {
            Assert.True(LinuxPath.IsPathRooted("\\one\\two\\three.txt"));
            Assert.True(LinuxPath.IsPathRooted("/one/two/three.txt"));

            Assert.False(LinuxPath.IsPathRooted("one\\two\\three"));
            Assert.False(LinuxPath.IsPathRooted("one/two/three"));
        }
    }
}
