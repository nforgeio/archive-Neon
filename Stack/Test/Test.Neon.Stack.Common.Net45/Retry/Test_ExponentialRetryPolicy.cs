﻿//-----------------------------------------------------------------------------
// FILE:	    Test_ExponentialRetryPolicy.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Stack.Common;
using Neon.Stack.Retry;

using Xunit;

namespace TestCommon
{
    public class TestExponentialRetryPolicy
    {
        private class TransientException : Exception
        {
        }

        private bool TransientDetector(Exception e)
        {
            return e is TransientException;
        }

        private bool VerifyInterval(DateTime time0, DateTime time1, TimeSpan minInterval)
        {
            // Verify that [time1] is greater than [time0] by at least [minInterval]
            // allowing 100ms of slop due to the fact that Task.Delay() sometimes 
            // delays for less than the requested timespan.

            return time1 - time0 > minInterval - TimeSpan.FromMilliseconds(100);
        }

        /// <summary>
        /// Verify that operation retry times are consistent with the retry policy.
        /// </summary>
        /// <param name="times"></param>
        /// <param name="policy"></param>
        private void VerifyIntervals(List<DateTime> times, ExponentialRetryPolicy policy)
        {
            var interval = policy.InitialRetryInterval;

            for (int i = 0; i < times.Count - 1; i++)
            {
                Assert.True(VerifyInterval(times[i], times[i + 1], interval));

                interval = TimeSpan.FromTicks(interval.Ticks * 2);

                if (interval > policy.MaxRetryInterval)
                {
                    interval = policy.MaxRetryInterval;
                }
            }
        }

        [Fact]
        public void Defaults()
        {
            var policy = new ExponentialRetryPolicy(TransientDetector);

            Assert.Equal(5, policy.MaxAttempts);
            Assert.Equal(TimeSpan.FromSeconds(1), policy.InitialRetryInterval);
            Assert.Equal(TimeSpan.FromHours(24), policy.MaxRetryInterval);
        }

        [Fact]
        public async Task FailAll()
        {
            var policy = new ExponentialRetryPolicy(TransientDetector);
            var times  = new List<DateTime>();

            await Assert.ThrowsAsync<TransientException>(
                async () =>
                {
                    await policy.InvokeAsync(
                        async () =>
                        {
                            times.Add(DateTime.UtcNow);
                            await Task.Delay(0);
                            throw new TransientException();
                        });
                });

            Assert.Equal(policy.MaxAttempts , times.Count);
            VerifyIntervals(times, policy);
        }

        [Fact]
        public async Task FailAll_Result()
        {
            var policy = new ExponentialRetryPolicy(TransientDetector);
            var times  = new List<DateTime>();

            await Assert.ThrowsAsync<TransientException>(
                async () =>
                {
                    await policy.InvokeAsync<string>(
                        async () =>
                        {
                            times.Add(DateTime.UtcNow);
                            await Task.Delay(0);
                            throw new TransientException();
                        });
                });

            Assert.Equal(policy.MaxAttempts, times.Count);
            VerifyIntervals(times, policy);
        }

        [Fact]
        public async Task FailImmediate()
        {
            var policy = new ExponentialRetryPolicy(TransientDetector);
            var times  = new List<DateTime>();

            await Assert.ThrowsAsync<NotImplementedException>(
                async () =>
                {
                    await policy.InvokeAsync(
                        async () =>
                        {
                            times.Add(DateTime.UtcNow);
                            await Task.Delay(0);
                            throw new NotImplementedException();
                        });
                });

            Assert.Equal(1, times.Count);
        }

        [Fact]
        public async Task FailImmediate_Result()
        {
            var policy = new ExponentialRetryPolicy(TransientDetector);
            var times  = new List<DateTime>();

            await Assert.ThrowsAsync<NotImplementedException>(
                async () =>
                {
                    await policy.InvokeAsync<string>(
                        async () =>
                        {
                            times.Add(DateTime.UtcNow);
                            await Task.Delay(0);
                            throw new NotImplementedException();
                        });
                });

            Assert.Equal(1, times.Count);
        }

        [Fact]
        public async Task FailDelayed()
        {
            var policy = new ExponentialRetryPolicy(TransientDetector);
            var times  = new List<DateTime>();

            await Assert.ThrowsAsync<NotImplementedException>(
                async () =>
                {
                    await policy.InvokeAsync(
                        async () =>
                        {
                            times.Add(DateTime.UtcNow);
                            await Task.Delay(0);

                            if (times.Count < 2)
                            {
                                throw new TransientException();
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        });
                });

            Assert.Equal(2, times.Count);
            VerifyIntervals(times, policy);
        }

        [Fact]
        public async Task FailDelayed_Result()
        {
            var policy = new ExponentialRetryPolicy(TransientDetector);
            var times  = new List<DateTime>();

            await Assert.ThrowsAsync<NotImplementedException>(
                async () =>
                {
                    await policy.InvokeAsync<string>(
                        async () =>
                        {
                            times.Add(DateTime.UtcNow);
                            await Task.Delay(0);

                            if (times.Count < 2)
                            {
                                throw new TransientException();
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        });
                });

            Assert.Equal(2, times.Count);
            VerifyIntervals(times, policy);
        }

        [Fact]
        public async Task SuccessImmediate()
        {
            var policy  = new ExponentialRetryPolicy(TransientDetector);
            var times   = new List<DateTime>();
            var success = false;

            await policy.InvokeAsync(
                async () =>
                {
                    times.Add(DateTime.UtcNow);
                    await Task.Delay(0);

                    success = true;
                });

            Assert.Equal(1, times.Count);
            Assert.True(success);
        }

        [Fact]
        public async Task SuccessImmediate_Result()
        {
            var policy = new ExponentialRetryPolicy(TransientDetector);
            var times   = new List<DateTime>();

            var success = await policy.InvokeAsync(
                async () =>
                {
                    times.Add(DateTime.UtcNow);
                    await Task.Delay(0);

                    return "WOOHOO!";
                });

            Assert.Equal(1, times.Count);
            Assert.Equal("WOOHOO!", success);
        }

        [Fact]
        public async Task SuccessDelayed()
        {
            var policy  = new ExponentialRetryPolicy(TransientDetector);
            var times   = new List<DateTime>();
            var success = false;

            await policy.InvokeAsync(
                async () =>
                {
                    times.Add(DateTime.UtcNow);
                    await Task.Delay(0);

                    if (times.Count < policy.MaxAttempts)
                    {
                        throw new TransientException();
                    }

                    success = true;
                });

            Assert.True(success);
            Assert.Equal(policy.MaxAttempts, times.Count);
            VerifyIntervals(times, policy);
        }

        [Fact]
        public async Task SuccessDelayed_Result()
        {
            var policy  = new ExponentialRetryPolicy(TransientDetector);
            var times   = new List<DateTime>();

            var success = await policy.InvokeAsync(
                async () =>
                {
                    times.Add(DateTime.UtcNow);
                    await Task.Delay(0);

                    if (times.Count < policy.MaxAttempts)
                    {
                        throw new TransientException();
                    }

                    return "WOOHOO!";
                });

            Assert.Equal("WOOHOO!", success);
            Assert.Equal(policy.MaxAttempts, times.Count);
            VerifyIntervals(times, policy);
        }

        [Fact]
        public async Task SuccessCustom()
        {
            var policy  = new ExponentialRetryPolicy(TransientDetector, maxAttempts: 6, initialRetryInterval: TimeSpan.FromSeconds(0.5), maxRetryInterval: TimeSpan.FromSeconds(4));
            var times   = new List<DateTime>();
            var success = false;

            Assert.Equal(6, policy.MaxAttempts);
            Assert.Equal(TimeSpan.FromSeconds(0.5), policy.InitialRetryInterval);
            Assert.Equal(TimeSpan.FromSeconds(4), policy.MaxRetryInterval);

            await policy.InvokeAsync(
                async () =>
                {
                    times.Add(DateTime.UtcNow);
                    await Task.Delay(0);

                    if (times.Count < policy.MaxAttempts)
                    {
                        throw new TransientException();
                    }

                    success = true;
                });

            Assert.True(success);
            Assert.Equal(policy.MaxAttempts, times.Count);
            VerifyIntervals(times, policy);
        }

        [Fact]
        public async Task SuccessCustom_Result()
        {
            var policy = new ExponentialRetryPolicy(TransientDetector, maxAttempts: 6, initialRetryInterval: TimeSpan.FromSeconds(0.5), maxRetryInterval: TimeSpan.FromSeconds(4));
            var times  = new List<DateTime>();

            Assert.Equal(6, policy.MaxAttempts);
            Assert.Equal(TimeSpan.FromSeconds(0.5), policy.InitialRetryInterval);
            Assert.Equal(TimeSpan.FromSeconds(4), policy.MaxRetryInterval);

            var success = await policy.InvokeAsync(
                async () =>
                {
                    times.Add(DateTime.UtcNow);
                    await Task.Delay(0);

                    if (times.Count < policy.MaxAttempts)
                    {
                        throw new TransientException();
                    }

                    return "WOOHOO!";
                });

            Assert.Equal("WOOHOO!", success);
            Assert.Equal(policy.MaxAttempts, times.Count);
            VerifyIntervals(times, policy);
        }
    }
}
