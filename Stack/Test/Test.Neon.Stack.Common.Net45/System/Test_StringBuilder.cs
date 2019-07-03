﻿//-----------------------------------------------------------------------------
// FILE:	    Test_StringBuilder.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Stack.Common;
using Neon.Stack.Retry;

using Xunit;

namespace TestCommon
{
    public class Test_StringBuilder
    {
        [Fact]
        public void AppendLineLinux()
        {
            var sb = new StringBuilder();

            sb.AppendLineLinux("this is a test");

            Assert.True(sb.ToString().Contains("\n"));
            Assert.False(sb.ToString().Contains("\r"));
        }

        [Fact]
        public void AppendWithSeparator()
        {
            var sb = new StringBuilder();

            sb.AppendWithSeparator("one");
            sb.AppendWithSeparator("two");
            sb.AppendWithSeparator(null);
            sb.AppendWithSeparator(string.Empty);
            sb.AppendWithSeparator("three");

            Assert.Equal("one two three", sb.ToString());

            sb.Clear();

            sb.AppendWithSeparator("one", ", ");
            sb.AppendWithSeparator("two", ", ");
            sb.AppendWithSeparator(null, ", ");
            sb.AppendWithSeparator(string.Empty, ", ");
            sb.AppendWithSeparator("three", ", ");

            Assert.Equal("one, two, three", sb.ToString());

        }
    }
}
