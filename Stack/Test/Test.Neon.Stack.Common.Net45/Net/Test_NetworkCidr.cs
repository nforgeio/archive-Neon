//-----------------------------------------------------------------------------
// FILE:	    Test_NetworkCidr.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Stack.Common;
using Neon.Stack.Net;

using Xunit;

namespace TestCommon
{
    public class Test_NetworkCidr
    {
        [Fact]
        public void Parse()
        {
            var cidr = NetworkCidr.Parse("10.1.2.3/8");

            Assert.Equal(IPAddress.Parse("10.1.2.3"), cidr.Address);
            Assert.Equal(8, cidr.PrefixLength);
            Assert.Equal(IPAddress.Parse("255.255.255.0"), cidr.Mask);

            cidr = NetworkCidr.Parse("10.1.2.3/16");

            Assert.Equal(IPAddress.Parse("10.1.2.3"), cidr.Address);
            Assert.Equal(16, cidr.PrefixLength);
            Assert.Equal(IPAddress.Parse("255.255.0.0"), cidr.Mask);

            cidr = NetworkCidr.Parse("10.1.2.3/24");

            Assert.Equal(IPAddress.Parse("10.1.2.3"), cidr.Address);
            Assert.Equal(24, cidr.PrefixLength);
            Assert.Equal(IPAddress.Parse("255.0.0.0"), cidr.Mask);
        }

        [Fact]
        public void ParseErrors()
        {
            // $note(jeff.lill): These won't be thrown because [Covenant.Requires<>()] is disabled.

            //Assert.Throws<ArgumentNullException>(() => NetworkCidr.Parse(null));
            //Assert.Throws<ArgumentNullException>(() => NetworkCidr.Parse(string.Empty));

            Assert.Throws<ArgumentException>(() => NetworkCidr.Parse("10.0.0.1"));
            Assert.Throws<ArgumentException>(() => NetworkCidr.Parse("/6"));
            Assert.Throws<ArgumentException>(() => NetworkCidr.Parse("10.0.0.1/-1"));
            Assert.Throws<ArgumentException>(() => NetworkCidr.Parse("10.0.0.1/33"));
            Assert.Throws<ArgumentException>(() => NetworkCidr.Parse("10.A.0.1/8"));
        }

        [Fact]
        public void TryParse()
        {
            NetworkCidr cidr;

            Assert.True(NetworkCidr.TryParse("10.1.2.3/8", out cidr));

            Assert.Equal(IPAddress.Parse("10.1.2.3"), cidr.Address);
            Assert.Equal(8, cidr.PrefixLength);
            Assert.Equal(IPAddress.Parse("255.255.255.0"), cidr.Mask);

            Assert.True(NetworkCidr.TryParse("10.1.2.3/16", out cidr));

            Assert.Equal(IPAddress.Parse("10.1.2.3"), cidr.Address);
            Assert.Equal(16, cidr.PrefixLength);
            Assert.Equal(IPAddress.Parse("255.255.0.0"), cidr.Mask);

            Assert.True(NetworkCidr.TryParse("10.1.2.3/24", out cidr));

            Assert.Equal(IPAddress.Parse("10.1.2.3"), cidr.Address);
            Assert.Equal(24, cidr.PrefixLength);
            Assert.Equal(IPAddress.Parse("255.0.0.0"), cidr.Mask);
        }

        [Fact]
        public void TryParseErrors()
        {
            NetworkCidr cidr;

            Assert.False(NetworkCidr.TryParse(null, out cidr));
            Assert.False(NetworkCidr.TryParse(string.Empty, out cidr));
            Assert.False(NetworkCidr.TryParse("10.0.0.1", out cidr));
            Assert.False(NetworkCidr.TryParse("/6", out cidr));
            Assert.False(NetworkCidr.TryParse("10.0.0.1/-1", out cidr));
            Assert.False(NetworkCidr.TryParse("10.0.0.1/33", out cidr));
            Assert.False(NetworkCidr.TryParse("10.A.0.1/8", out cidr));
        }

        [Fact]
        public void Init()
        {
            var cidr = new NetworkCidr(IPAddress.Parse("10.1.2.3"), 8);

            Assert.Equal(IPAddress.Parse("10.1.2.3"), cidr.Address);
            Assert.Equal(8, cidr.PrefixLength);
            Assert.Equal(IPAddress.Parse("255.255.255.0"), cidr.Mask);

            cidr = new NetworkCidr(IPAddress.Parse("10.1.2.3"), 16);

            Assert.Equal(IPAddress.Parse("10.1.2.3"), cidr.Address);
            Assert.Equal(16, cidr.PrefixLength);
            Assert.Equal(IPAddress.Parse("255.255.0.0"), cidr.Mask);

            cidr = new NetworkCidr(IPAddress.Parse("10.1.2.3"), 24);

            Assert.Equal(IPAddress.Parse("10.1.2.3"), cidr.Address);
            Assert.Equal(24, cidr.PrefixLength);
            Assert.Equal(IPAddress.Parse("255.0.0.0"), cidr.Mask);
        }

        [Fact]
        public void InitErrors()
        {
            // $note(jeff.lill): These won't be thrown because [Covenant.Requires<>()] is disabled.

            //Assert.Throws<ArgumentNullException>(() => new NetworkCidr(null, 8));
            //Assert.Throws<ArgumentException>(() => new NetworkCidr(IPAddress.Parse("255.255.0.0"), -1));
            //Assert.Throws<ArgumentException>(() => new NetworkCidr(IPAddress.Parse("255.255.0.0"), 33));
        }

        [Fact]
        public void Compare()
        {
            Assert.True(NetworkCidr.Parse("10.0.0.1/8") == NetworkCidr.Parse("10.0.0.1/8"));
            Assert.True(NetworkCidr.Parse("10.0.0.1/8").Equals(NetworkCidr.Parse("10.0.0.1/8")));
            Assert.False(NetworkCidr.Parse("10.0.0.1/8") == NetworkCidr.Parse("10.0.2.1/8"));
            Assert.False(NetworkCidr.Parse("10.0.0.1/8") == NetworkCidr.Parse("10.0.0.1/16"));

            Assert.False(NetworkCidr.Parse("10.0.0.1/8") != NetworkCidr.Parse("10.0.0.1/8"));
            Assert.True(NetworkCidr.Parse("10.0.0.1/8") != NetworkCidr.Parse("10.0.2.1/8"));
            Assert.True(NetworkCidr.Parse("10.0.0.1/8") != NetworkCidr.Parse("10.0.0.1/16"));

            Assert.Equal(NetworkCidr.Parse("10.0.0.1/8").GetHashCode(), NetworkCidr.Parse("10.0.0.1/8").GetHashCode());
            Assert.NotEqual(NetworkCidr.Parse("10.0.0.1/8").GetHashCode(), NetworkCidr.Parse("10.0.0.1/16").GetHashCode());
            Assert.NotEqual(NetworkCidr.Parse("10.0.0.1/8").GetHashCode(), NetworkCidr.Parse("10.0.0.2/8").GetHashCode());
        }
    }
}
