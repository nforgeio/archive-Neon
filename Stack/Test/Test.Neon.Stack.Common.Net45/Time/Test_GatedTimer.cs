﻿//-----------------------------------------------------------------------------
// FILE:	    Test_GatedTimer.cs
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
using Neon.Stack.Time;

using Xunit;

// $todo(jeff.lill):
//
// This is very old code that could be simplified using the
// C# => operator and closures.

namespace TestCommon
{
    public class Test_GatedTimer
    {
        GatedTimer      timer;
        private int     wait;
        private int     count;
        private int     maxCount;
        private object  state;
        private bool    dispose;
        private int     change;

        private void OnTimer(object state)
        {
            if (count < maxCount)
            {
                count++;
            }

            this.state = state;

            Thread.Sleep(wait);

            if (dispose)
            {
                timer.Dispose();
            }

            if (change > 0)
            {
                timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(change));
            }
        }

        [Fact]
        public void Basic()
        {
            count    = 0;
            maxCount = int.MaxValue;
            state    = null;
            wait     = 2000;
            dispose  = false;
            change   = 0;
            timer    = new GatedTimer(new TimerCallback(OnTimer), 10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

            Thread.Sleep(1000);
            timer.Dispose();
            Assert.Equal(1, count);
            Assert.Equal(10, (int)state);

            count    = 0;
            maxCount = 10;
            state    = null;
            wait     = 0;
            dispose  = false;
            change   = 0;
            timer    = new GatedTimer(new TimerCallback(OnTimer), 10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

            Thread.Sleep(2000);
            timer.Dispose();
            Assert.Equal(10, count);
            Assert.Equal(10, (int)state);
        }

        [Fact]
        public void Dispose()
        {
            count    = 0;
            maxCount = int.MaxValue;
            state    = null;
            wait     = 0;
            dispose  = true;
            change   = 0;
            timer    = new GatedTimer(new TimerCallback(OnTimer), 10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

            Thread.Sleep(1000);
            Assert.Equal(1, count);
            Assert.Equal(10, (int)state);
        }

        [Fact]
        public void Change()
        {
            // $todo(jeff.lill): Need to implement this.
        }
    }
}