﻿//-----------------------------------------------------------------------------
// FILE:	    JsonClient.cs
// CONTRIBUTOR: ArgDictionary Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Stack.Common;
using Neon.Stack.Retry;

namespace Neon.Stack.Net
{
    /// <summary>
    /// A dictionary of objects keyed by case insenstive strings used as a shorthand 
    /// way for passing optional arguments to other class' methods (like <see cref="JsonClient"/>.
    /// </summary>
    public class ArgDictionary : Dictionary<string, object>
    {
    }
}
