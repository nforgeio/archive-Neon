//-----------------------------------------------------------------------------
// FILE:	    Test_ConsulInfo.cs
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
    public class Test_ConsulInfo
    {
        [Fact]
        public void Parse()
        {
            const string output =
@"agent:
    check_monitors = 0
    check_ttls = 0
    checks = 0
    services = 0
consul:
    bootstrap = true
    known_datacenters = 1
    leader = true
    server = true
raft:
    applied_index = 45832
    commit_index = 45832
    fsm_pending = 0
    last_log_index = 45832
    last_log_term = 4
    last_snapshot_index = 45713
    last_snapshot_term = 1
    num_peers = 2
    state = Leader
    term = 4
serf_lan:
    event_queue = 0
    event_time = 2
    failed = 0
    intent_queue = 0
    left = 0
    member_time = 7
    members = 3
    query_queue = 0
    query_time = 1
serf_wan:
    event_queue = 0
    event_time = 1
    failed = 0
    intent_queue = 0
    left = 0
    member_time = 1
    members = 1
    query_queue = 0
    query_time = 1
";
            var info = new ConsulInfo(output);

            Assert.Equal("0", info["agent.check_monitors"]);
            Assert.Equal("true", info["consul.bootstrap"]);
            Assert.Equal("1", info["consul.known_datacenters"]);
            Assert.Equal("true", info["consul.leader"]);
            Assert.Equal("2", info["raft.num_peers"]);
        }
    }
}
