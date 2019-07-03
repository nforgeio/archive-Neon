//-----------------------------------------------------------------------------
// FILE:	    Test_Signage.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Couchbase;
using Couchbase.Lite;
using Couchbase.Lite.Auth;

using Neon.Stack.Common;
using Neon.Stack.Data;
using Neon.Stack.Zip;

using Neon.Fun;
using Neon.Fun.Signage;

using Xunit;

namespace Test.Neon.Fun.Models.Couchbase.Lite
{
    public class Test_Signage
    {
        public Test_Signage()
        {
            // We need to make sure all generated entity 
            // classes have been registered.

            ModelTypes.Register();

            // Initialize the SharpZipLib virtual file system.

            ZipFileSystem.Initialize();
        }

        [Fact]
        public void ContentPackage()
        {
            // Verify that we can ZIP and UNZIP files into a signage content package.

            using (var test = new TestDatabase())
            {
                var db = test.Database;
                var doc = db.GetBinderDocument<SignageContentDocument>("test");

                using (var tempFolder = new TempFolder())
                {
                    File.WriteAllText(Path.Combine(tempFolder.Path, "file1.txt"), "Hello World!");
                    File.WriteAllBytes(Path.Combine(tempFolder.Path, "file2.dat"), new byte[] { 0, 1, 2, 3, 4 });

                    var subFolder = Path.Combine(tempFolder.Path, "Foo");

                    Directory.CreateDirectory(subFolder);
                    File.WriteAllText(Path.Combine(subFolder, "file3.txt"), "FOOBAR!");

                    doc.Zip(tempFolder.Path);
                }

                doc.Save();
                doc = db.GetBinderDocument<SignageContentDocument>("test");

                using (var tempFolder = new TempFolder())
                {
                    doc.Unzip(tempFolder.Path);

                    Assert.True(File.Exists(Path.Combine(tempFolder.Path, "file1.txt")));
                    Assert.True(File.Exists(Path.Combine(tempFolder.Path, "file2.dat")));
                    Assert.True(File.Exists(Path.Combine(tempFolder.Path, "Foo", "file3.txt")));

                    Assert.Equal("Hello World!", File.ReadAllText(Path.Combine(tempFolder.Path, "file1.txt")));
                    Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, File.ReadAllBytes(Path.Combine(tempFolder.Path, "file2.dat")));
                    Assert.Equal("FOOBAR!", File.ReadAllText(Path.Combine(tempFolder.Path, "Foo", "file3.txt")));
                }
            }
        }
    }
}