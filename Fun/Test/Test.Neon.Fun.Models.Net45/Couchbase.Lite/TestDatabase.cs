//-----------------------------------------------------------------------------
// FILE:	    TestDatabase.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Couchbase;
using Couchbase.Lite;
using Couchbase.Lite.Auth;

using Neon.Stack.Common;

using Xunit;

namespace Test.Neon.Fun.Models.Couchbase.Lite
{
    /// <summary>
    /// Creates a temporary test database.
    /// </summary>
    public sealed class TestDatabase : IDisposable
    {
        private bool        isDisposed = false;
        private string      folder;
        private Manager     manager;

        public TestDatabase()
        {
            folder  = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            manager = new Manager(new DirectoryInfo(folder), new ManagerOptions())
            {
                // $todo(jeff.lill):
                //
                // I can't get ForestDB working here for .NET 4.x unit tests.  SQLite seems
                // to work fine and I am able to enable ForestDB in the apps.

                //StorageType = StorageEngineTypes.ForestDB
            };

            Database = manager.GetEntityDatabase("test");
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            Database.Dispose();
            Database = null;

            manager.Close();
            manager = null;

            isDisposed = false;

            Directory.Delete(folder, recursive: true);
        }

        public EntityDatabase Database { get; private set; }
    }
}
