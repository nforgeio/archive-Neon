﻿//-----------------------------------------------------------------------------
// FILE:	    PauseStep.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Stack.Common;

namespace Neon.Cluster
{
    /// <summary>
    /// Pauses cluster configuration for a period of time.
    /// </summary>
    public class PauseStep : ConfigStep
    {
        private TimeSpan    delay;

        /// <summary>
        /// Constructs a configuration step that ensures that a Docker volume has
        /// been created on a specific Docker node.
        /// </summary>
        /// <param name="delay">The amount of time to pause.</param>
        public PauseStep(TimeSpan delay)
        {
            Covenant.Requires<ArgumentException>(delay >= TimeSpan.Zero);

            this.delay = delay;
        }

        /// <inheritdoc/>
        public override void Run(ClusterProxy cluster)
        {
            Covenant.Requires<ArgumentNullException>(cluster != null);

            foreach (var node in cluster.Nodes)
            {
                node.Status = $"pause {delay}";
            }

            Thread.Sleep(delay);

            foreach (var node in cluster.Nodes)
            {
                node.Status = string.Empty;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"pause delay={delay}";
        }
    }
}
