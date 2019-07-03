﻿//-----------------------------------------------------------------------------
// FILE:	    TestClass.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Test.Neon.Models
{
    [Include]
    public class TestClass
    {
        public const string TestString1 = "Hello World!";
        public const string TestString2 = null;
        public const int TestInt = 666;
        public const bool TestBool = true;
    }
}
