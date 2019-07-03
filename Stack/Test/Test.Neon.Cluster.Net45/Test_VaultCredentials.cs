//-----------------------------------------------------------------------------
// FILE:	    Test_VaultCredentials.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Cluster;
using Neon.Stack.Common;

using Xunit;

namespace TestNeonCluster
{
    public class Test_VaultCredentials
    {
        [Fact]
        public void ParseInit()
        {
            // Parse simulated output from: vault init

            var credentials = VaultCredentials.FromInit(
@"Unseal Key 1: HPRPqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=
Initial Root Token: f4aadf41-44c8-118b-07a0-61357b24eb54

Vault initialized with 1 keys and a key threshold of 1. Please
securely distribute the above keys. When the Vault is re-sealed,
restarted, or stopped, you must provide at least 1 of these keys
to unseal it again.

Vault does not store the master key. Without at least 1 keys,
your Vault will remain permanently sealed.
", keyThreshold: 1);

            Assert.Equal("f4aadf41-44c8-118b-07a0-61357b24eb54", credentials.RootToken);
            Assert.Equal(1, credentials.KeyThreshold);
            Assert.Equal(1, credentials.UnsealKeys.Count);
            Assert.Equal("HPRPqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=", credentials.UnsealKeys[0]);

            credentials = VaultCredentials.FromInit(
@"Unseal Key 1: HPRPqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=
Unseal Key 2: 666PqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=
Initial Root Token: f4aadf41-44c8-118b-07a0-61357b24eb54

Vault initialized with 1 keys and a key threshold of 1. Please
securely distribute the above keys. When the Vault is re-sealed,
restarted, or stopped, you must provide at least 1 of these keys
to unseal it again.

Vault does not store the master key. Without at least 1 keys,
your Vault will remain permanently sealed.
", keyThreshold: 1);

            Assert.Equal("f4aadf41-44c8-118b-07a0-61357b24eb54", credentials.RootToken);
            Assert.Equal(1, credentials.KeyThreshold);
            Assert.Equal(2, credentials.UnsealKeys.Count);
            Assert.Equal("HPRPqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=", credentials.UnsealKeys[0]);
            Assert.Equal("666PqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=", credentials.UnsealKeys[1]);

            credentials = VaultCredentials.FromInit(
@"Unseal Key 1: HPRPqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=
Unseal Key 2: 666PqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=
Unseal Key 3: 777PqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=
Initial Root Token: f4aadf41-44c8-118b-07a0-61357b24eb54

Vault initialized with 1 keys and a key threshold of 1. Please
securely distribute the above keys. When the Vault is re-sealed,
restarted, or stopped, you must provide at least 1 of these keys
to unseal it again.

Vault does not store the master key. Without at least 1 keys,
your Vault will remain permanently sealed.
", keyThreshold: 2);

            Assert.Equal("f4aadf41-44c8-118b-07a0-61357b24eb54", credentials.RootToken);
            Assert.Equal(2, credentials.KeyThreshold);
            Assert.Equal(3, credentials.UnsealKeys.Count);
            Assert.Equal("HPRPqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=", credentials.UnsealKeys[0]);
            Assert.Equal("666PqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=", credentials.UnsealKeys[1]);
            Assert.Equal("777PqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=", credentials.UnsealKeys[2]);
        }

        [Fact]
        public void ParseJson()
        {
            var credentials = VaultCredentials.FromJson(
@"
{
    ""RootToken"": ""f4aadf41-44c8-118b-07a0-61357b24eb54"",
    ""KeyThreshold"": 2,
    ""UnsealKeys"" : [
        ""HPRPqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14="",
        ""666PqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14="",
        ""777PqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14="",
    ]
}
");
            Assert.Equal("f4aadf41-44c8-118b-07a0-61357b24eb54", credentials.RootToken);
            Assert.Equal(2, credentials.KeyThreshold);
            Assert.Equal(3, credentials.UnsealKeys.Count);
            Assert.Equal("HPRPqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=", credentials.UnsealKeys[0]);
            Assert.Equal("666PqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=", credentials.UnsealKeys[1]);
            Assert.Equal("777PqCNzpd8zELP0ILTWa41v1DrbRPj8mXe2D6SVQ14=", credentials.UnsealKeys[2]);
        }
    }
}
