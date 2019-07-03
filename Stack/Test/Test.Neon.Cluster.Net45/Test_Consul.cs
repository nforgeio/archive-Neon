//-----------------------------------------------------------------------------
// FILE:	    Test_Consul.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Consul;

using Neon.Cluster;
using Neon.Stack.Common;

using Xunit;

namespace TestNeonCluster
{
    /// <summary>
    /// This test requires a running <b>dev</b> NeonCluster.
    /// </summary>
    public class Test_Consul
    {
        private TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

        public class TestClass
        {
            public string Field1 { get; set; }
            public int Field2 { get; set; }
        }

        [Fact]
        public async void KVBasic()
        {
            try
            {
                NeonClusterHelper.ConnectCluster();

                using (var consul = NeonClusterHelper.OpenConsul())
                {
                    // Test putting values.

                    Assert.True(await consul.KV.PutString("neon/unit-test/string", "foobar"));

                    Assert.True(await consul.KV.PutString("neon/unit-test/bool0", "0"));
                    Assert.True(await consul.KV.PutString("neon/unit-test/bool1", "no"));
                    Assert.True(await consul.KV.PutString("neon/unit-test/bool2", "false"));
                    Assert.True(await consul.KV.PutString("neon/unit-test/bool3", "False"));
                    Assert.True(await consul.KV.PutString("neon/unit-test/bool4", "1"));
                    Assert.True(await consul.KV.PutString("neon/unit-test/bool5", "yes"));
                    Assert.True(await consul.KV.PutString("neon/unit-test/bool6", "true"));
                    Assert.True(await consul.KV.PutString("neon/unit-test/bool7", "True"));

                    Assert.True(await consul.KV.PutInt("neon/unit-test/int", 123));
                    Assert.True(await consul.KV.PutLong("neon/unit-test/long", long.MaxValue));
                    Assert.True(await consul.KV.PutDouble("neon/unit-test/double", 123.456));

                    var v = new TestClass()
                    {
                        Field1 = "Hello World!",
                        Field2 = -666
                    };

                    Assert.True(await consul.KV.PutObject("neon/unit-test/json", v));

                    // Test getting values.

                    Assert.Equal("foobar", await consul.KV.GetString("neon/unit-test/string"));

                    Assert.False(await consul.KV.GetBool("neon/unit-test/bool0"));
                    Assert.False(await consul.KV.GetBool("neon/unit-test/bool1"));
                    Assert.False(await consul.KV.GetBool("neon/unit-test/bool2"));
                    Assert.False(await consul.KV.GetBool("neon/unit-test/bool3"));
                    Assert.True(await consul.KV.GetBool("neon/unit-test/bool4"));
                    Assert.True(await consul.KV.GetBool("neon/unit-test/bool5"));
                    Assert.True(await consul.KV.GetBool("neon/unit-test/bool6"));
                    Assert.True(await consul.KV.GetBool("neon/unit-test/bool7"));

                    Assert.Equal(123, await consul.KV.GetInt("neon/unit-test/int"));
                    Assert.Equal(long.MaxValue, await consul.KV.GetLong("neon/unit-test/long"));
                    Assert.Equal(123.456, await consul.KV.GetDouble("neon/unit-test/double"));

                    v = await consul.KV.GetObject<TestClass>("neon/unit-test/json");

                    Assert.Equal("Hello World!", v.Field1);
                    Assert.Equal(-666, v.Field2);

                    // Test Exists()

                    Assert.True(await consul.KV.Exists("neon/unit-test/bool0"));
                    Assert.False(await consul.KV.Exists("neon/unit-test/not-there"));

                    // Test error detection.

                    await Assert.ThrowsAsync<KeyNotFoundException>(async () => await consul.KV.GetString("neon/unit-test/not-there"));

                    await Assert.ThrowsAsync<FormatException>(async () => await consul.KV.GetBool("neon/unit-test/int"));
                    await Assert.ThrowsAsync<FormatException>(async () => await consul.KV.GetInt("neon/unit-test/bool1"));
                    await Assert.ThrowsAsync<FormatException>(async () => await consul.KV.GetLong("neon/unit-test/bool1"));
                    await Assert.ThrowsAsync<FormatException>(async () => await consul.KV.GetDouble("neon/unit-test/bool1"));
                    await Assert.ThrowsAsync<FormatException>(async () => await consul.KV.GetObject<TestClass>("neon/unit-test/bool1"));
                }
            }
            finally
            {
                using (var consul = NeonClusterHelper.OpenConsul())
                {
                    await consul.KV.DeleteTree("neon/unit-test");
                }

                NeonClusterHelper.DisconnectCluster();
            }
        }

        [Fact]
        public async void KVWatchKey()
        {
            // Test watching a key for changes without a timeout.

            try
            {
                NeonClusterHelper.ConnectCluster();

                using (var consul = NeonClusterHelper.OpenConsul())
                {
                    var key     = "neon/unit-test/foo";
                    var cts     = new CancellationTokenSource();
                    var ct      = cts.Token;
                    var changes = 0;

                    await consul.KV.PutInt(key, 0);

                    var watchTask = consul.KV.Watch(key,
                        async () =>
                        {
                            changes++;

                            await Task.Delay(0);
                        },
                        cancellationToken: ct);

                    // Verify that the initial action call is made.

                    NeonHelper.WaitFor(() => changes > 0, TestTimeout);

                    Assert.Equal(1, changes);

                    // Verify that we detect subsequent changes.

                    changes = 0;

                    for (int i = 1; i <= 10; i++)
                    {
                        await consul.KV.PutInt(key, i);

                        NeonHelper.WaitFor(() => changes >= i, TestTimeout);

                        Assert.Equal(i, changes);
                    }

                    // Stop the watch.

                    cts.Cancel();
                    Assert.Throws<AggregateException>(() => watchTask.Wait());
                }
            }
            finally
            {
                using (var consul = NeonClusterHelper.OpenConsul())
                {
                    await consul.KV.DeleteTree("neon/unit-test");
                }

                NeonClusterHelper.DisconnectCluster();
            }
        }

        [Fact]
        public async void KVWatchPrefix()
        {
            // Test watching a key prefix for changes without a timeout.

            try
            {
                NeonClusterHelper.ConnectCluster();

                using (var consul = NeonClusterHelper.OpenConsul())
                {
                    var keyPrefix = "neon/unit-test/";
                    var key       = $"{keyPrefix}foo";
                    var cts       = new CancellationTokenSource();
                    var ct        = cts.Token;
                    var changes   = 0;

                    await consul.KV.PutInt(key, 0);

                    var watchTask = consul.KV.Watch(keyPrefix,
                        async () =>
                        {
                            changes++;
                            await Task.Delay(0);
                        },
                        cancellationToken: ct);

                    // Verify that the initial action call is made.

                    NeonHelper.WaitFor(() => changes > 0, TestTimeout);

                    Assert.Equal(1, changes);

                    // Verify that we detect subsequent changes.

                    changes = 0;

                    for (int i = 1; i <= 10; i++)
                    {
                        await consul.KV.PutInt(key, i);

                        NeonHelper.WaitFor(() => changes >= i, TestTimeout);

                        Assert.Equal(i, changes);
                    }

                    // Stop the watch.

                    cts.Cancel();
                    Assert.Throws<AggregateException>(() => watchTask.Wait());
                }
            }
            finally
            {
                using (var consul = NeonClusterHelper.OpenConsul())
                {
                    await consul.KV.DeleteTree("neon/unit-test");
                }

                NeonClusterHelper.DisconnectCluster();
            }
        }
    }
}
