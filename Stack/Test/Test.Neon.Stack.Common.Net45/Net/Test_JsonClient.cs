﻿//-----------------------------------------------------------------------------
// FILE:	    Test_JsonClient_Get.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Stack.Common;
using Neon.Stack.Net;
using Neon.Stack.Retry;

using Xunit;

namespace TestCommon
{
    public partial class Test_JsonClient
    {
        private const string baseUri = "http://127.0.0.10:80/";

        public class RequestDoc
        {
            public string Operation { get; set; }
            public string Arg0 { get; set; }
            public string Arg1 { get; set; }
        }

        public class ReplyDoc
        {
            public string Value1 { get; set; }
            public string Value2 { get; set; }
        }

        private string GetBodyText(IOwinRequest request)
        {
            return new StreamReader(request.Body).ReadToEnd();
        }

        [Fact]
        public void Defaults()
        {
            using (var jsonClient = new JsonClient())
            {
                Assert.IsType<ExponentialRetryPolicy>(jsonClient.SafeRetryPolicy);
                Assert.IsType<ExponentialRetryPolicy>(jsonClient.UnsafeRetryPolicy);
            }
        }
    }
}
