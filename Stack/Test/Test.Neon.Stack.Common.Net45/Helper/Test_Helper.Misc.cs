﻿//-----------------------------------------------------------------------------
// FILE:	    Test_NeonHelper.Misc.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Stack.Common;

using Xunit;

namespace TestCommon
{
    public partial class Test_Helper
    {
        [Fact]
        public void FromHex()
        {
            Assert.Equal(new byte[0], NeonHelper.FromHex(""), new CollectionComparer<byte>());
            Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0xAA, 0xCF }, NeonHelper.FromHex("000102AACF"), new CollectionComparer<byte>());
            Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0xAA, 0xCF }, NeonHelper.FromHex("000102aacf"), new CollectionComparer<byte>());
            Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0xAA, 0xCF }, NeonHelper.FromHex("00 010\r\n2AA\tCF"), new CollectionComparer<byte>());
        }

        [Fact]
        public void TryParseHex_Int()
        {
            int v;

            Assert.True(NeonHelper.TryParseHex("0", out v));
            Assert.Equal(0, v);

            Assert.True(NeonHelper.TryParseHex("0000", out v));
            Assert.Equal(0, v);

            Assert.True(NeonHelper.TryParseHex("FFFF", out v));
            Assert.Equal(0xFFFF, v);

            Assert.True(NeonHelper.TryParseHex("A", out v));
            Assert.Equal(0xA, v);

            Assert.True(NeonHelper.TryParseHex("Abcd", out v));
            Assert.Equal(0xABCD, v);

            Assert.False(NeonHelper.TryParseHex("", out v));
            Assert.False(NeonHelper.TryParseHex("1q", out v));
        }

        [Fact]
        public void TryParseHex_Array()
        {
            byte[] v;

            Assert.True(NeonHelper.TryParseHex("0000", out v));
            Assert.Equal(new byte[] { 0, 0 }, v, new CollectionComparer<byte>());

            Assert.True(NeonHelper.TryParseHex("FFFF", out v));
            Assert.Equal(new byte[] { 0xFF, 0xFF }, v, new CollectionComparer<byte>());

            Assert.True(NeonHelper.TryParseHex("A1", out v));
            Assert.Equal(new byte[] { 0xA1 }, v, new CollectionComparer<byte>());

            Assert.True(NeonHelper.TryParseHex("Abcd", out v));
            Assert.Equal(new byte[] { 0xAB, 0xCD }, v, new CollectionComparer<byte>());

            Assert.True(NeonHelper.TryParseHex("000102030405060708090A0B0C0D0E0F", out v));
            Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F }, v, new CollectionComparer<byte>());

            Assert.False(NeonHelper.TryParseHex("", out v));
            Assert.False(NeonHelper.TryParseHex("1", out v));
            Assert.False(NeonHelper.TryParseHex("1q", out v));
        }

        [Fact]
        public void HexDump()
        {
            byte[] data = Encoding.ASCII.GetBytes("0123456789ABCDEF");

            Assert.Equal("", NeonHelper.HexDump(data, 0, 0, 16, HexDumpOption.None));
            Assert.Equal("", NeonHelper.HexDump(data, 0, 0, 16, HexDumpOption.ShowAll));

            Assert.Equal("30 31 32 33 \r\n", NeonHelper.HexDump(data, 0, 4, 4, HexDumpOption.None));
            Assert.Equal("30 31 \r\n32 33 \r\n", NeonHelper.HexDump(data, 0, 4, 2, HexDumpOption.None));
            Assert.Equal("0000: 31 32 33 34 - 1234\r\n", NeonHelper.HexDump(data, 1, 4, 4, HexDumpOption.ShowAll));
            Assert.Equal("0000: 30 31 32 33 34 35 36 37 - 01234567\r\n", NeonHelper.HexDump(data, 0, 8, 8, HexDumpOption.ShowAll));
            Assert.Equal("0000: 30 31 32 33 34 35 36 37 - 01234567\r\n0008: 38 39 41 42 43 44 45 46 - 89ABCDEF\r\n", NeonHelper.HexDump(data, 0, 16, 8, HexDumpOption.ShowAll));
            Assert.Equal("0000: 30 31 32 - 012\r\n0003: 33 34    - 34\r\n", NeonHelper.HexDump(data, 0, 5, 3, HexDumpOption.ShowAll));
        }

        [Fact]
        public void JTokenEquals()
        {
            // NULL tests

            Assert.True(NeonHelper.JTokenEquals(null, null));
            Assert.False(NeonHelper.JTokenEquals(null, new JProperty("test", 10)));
            Assert.False(NeonHelper.JTokenEquals(new JProperty("test", 10), null));

            // Different token type tests

            Assert.False(NeonHelper.JTokenEquals(new JProperty("test", 10), new JValue(10)));
            Assert.False(NeonHelper.JTokenEquals(new JProperty("test", 10), new JArray()));
            Assert.False(NeonHelper.JTokenEquals(new JValue(10.123), new JValue(10)));

            // JProperty tests

            Assert.True(NeonHelper.JTokenEquals(new JProperty("test", 10), new JProperty("test", 10)));
            Assert.True(NeonHelper.JTokenEquals(new JProperty("test", null), new JProperty("test", null)));
            Assert.True(NeonHelper.JTokenEquals(new JProperty("test", new JValue((string)null)), new JProperty("test", null)));
            Assert.False(NeonHelper.JTokenEquals(new JProperty("test", 10), new JProperty("test", 20)));
            Assert.False(NeonHelper.JTokenEquals(new JProperty("test", 10), new JProperty("different", 10)));
            Assert.False(NeonHelper.JTokenEquals(new JProperty("test", 10), new JProperty("test", new JObject())));

            // JObject tests

            Assert.True(
                NeonHelper.JTokenEquals(
                    new JObject(),
                    new JObject()));

            Assert.True(
                NeonHelper.JTokenEquals(
                    new JObject(
                        new JProperty("prop1", "one"),
                        new JProperty("prop2", "two")),
                    new JObject(
                        new JProperty("prop1", "one"),
                        new JProperty("prop2", "two"))));

            Assert.False(
                NeonHelper.JTokenEquals(
                    new JObject(
                        new JProperty("prop1", "one"),
                        new JProperty("prop2", "TWO")),
                    new JObject(
                        new JProperty("prop1", "one"),
                        new JProperty("prop2", "two"))));

            Assert.False(
                NeonHelper.JTokenEquals(
                    new JObject(
                        new JProperty("prop1", "one"),
                        new JProperty("PROP2", "two")),
                    new JObject(
                        new JProperty("prop1", "one"),
                        new JProperty("prop2", "two"))));

            Assert.False(
                NeonHelper.JTokenEquals(
                    new JObject(
                        new JProperty("prop1", "one")),
                    new JObject(
                        new JProperty("prop1", "one"),
                        new JProperty("prop2", "two"))));

            // JArray tests

            Assert.True(
                NeonHelper.JTokenEquals(
                    new JArray(),
                    new JArray()));

            Assert.True(
                NeonHelper.JTokenEquals(
                    new JArray(1, 2, 3, 4),
                    new JArray(1, 2, 3, 4)));

            Assert.False(
                NeonHelper.JTokenEquals(
                    new JArray(1, 2, 3, 4),
                    new JArray(1, 2, 3, 4, 5)));

            Assert.False(
                NeonHelper.JTokenEquals(
                    new JArray(1, 2, 3, 4),
                    new JArray(1, 2, "FOO", 4)));

            // Value tests

            Assert.True(NeonHelper.JTokenEquals(
                new JValue(true),
                new JValue(true)));

            Assert.False(NeonHelper.JTokenEquals(
                new JValue(true),
                new JValue(false)));

            Assert.True(NeonHelper.JTokenEquals(
                new JValue(10),
                new JValue(10)));

            Assert.False(NeonHelper.JTokenEquals(
                new JValue(10),
                new JValue(20)));

            Assert.True(NeonHelper.JTokenEquals(
                new JValue("HELLO"),
                new JValue("HELLO")));

            Assert.False(NeonHelper.JTokenEquals(
                new JValue("FOO"),
                new JValue("BAR")));

            Assert.False(NeonHelper.JTokenEquals(
                new JValue("FOO"),
                new JValue(20)));

            Assert.True(NeonHelper.JTokenEquals(
                new JValue(Guid.Empty),
                new JValue(Guid.Empty)));

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();

            Assert.True(NeonHelper.JTokenEquals(
                new JValue(guid1),
                new JValue(guid1)));

            Assert.False(NeonHelper.JTokenEquals(
                new JValue(guid1),
                new JValue(guid2)));

            Assert.False(NeonHelper.JTokenEquals(
                new JValue(guid1),
                new JValue(Guid.Empty)));

            // Test value types that will roundtrip as strings.

            var now = DateTime.Now;
            var nowOffset = DateTimeOffset.Now;

            Assert.True(NeonHelper.JTokenEquals(
                new JValue(now),
                new JValue(now.ToString())));

            Assert.True(NeonHelper.JTokenEquals(
                new JValue(nowOffset),
                new JValue(nowOffset.ToString())));

            Assert.True(NeonHelper.JTokenEquals(
                new JValue(guid1),
                new JValue(guid1.ToString())));

            Assert.True(NeonHelper.JTokenEquals(
                new JValue(TimeSpan.FromMinutes(1.5)),
                new JValue(TimeSpan.FromMinutes(1.5).ToString())));

            Assert.True(NeonHelper.JTokenEquals(
                new JValue(new Uri("http://hello.com")),
                new JValue(new Uri("http://hello.com").ToString())));
        }

        [Fact]
        public void GetRandomPassword()
        {
            // Test generating passwords of various lengths.

            for (int i = 1; i < 100; i++)
            {
                Assert.Equal(i, NeonHelper.GetRandomPassword(i).Length);
            }

            // Generate a bunch of passwords and verify that we don't 
            // see any duplicates (I know this is a weak test).

            var existing = new HashSet<string>();

            for (int i = 0; i < 1000; i++)
            {
                var password = NeonHelper.GetRandomPassword(20);

                Assert.False(existing.Contains(password));

                existing.Add(password);
            }
        }

        public enum JsonTestEnum
        {
            Test,
            CamelCase
        }

        public class JsonTestClass
        {
            public JsonTestEnum EnumValue { get; set; }
        }

        [Fact]
        public void ArrayEquals_Byte()
        {
            Assert.True(NeonHelper.ArrayEquals(null, null));
            Assert.True(NeonHelper.ArrayEquals(new byte[0], new byte[0]));
            Assert.True(NeonHelper.ArrayEquals(new byte[] { 0, 1, 2, 3, 4 }, new byte[] { 0, 1, 2, 3, 4 }));

            Assert.False(NeonHelper.ArrayEquals(new byte[0], null));
            Assert.False(NeonHelper.ArrayEquals(null, new byte[0]));
            Assert.False(NeonHelper.ArrayEquals(new byte[] { 0, 1, 2, 3, 100 }, new byte[] { 0, 1, 2, 3, 4 }));
            Assert.False(NeonHelper.ArrayEquals(new byte[] { 0, 1, 2, 3 }, new byte[] { 0, 1, 2, 3, 4 }));
        }
    }
}
