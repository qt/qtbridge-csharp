// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Api
{
    [TestClass]
    public class Test_Resources
    {
        [TestCleanup]
        public void Cleanup()
        {
            Qt.Resources.InstanceOverride = null;
        }

        // --- URI validation (no FakeQtResources needed) ---

        [TestMethod]
        public void Exists_NullUrl_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsExactly<ArgumentException>(() => Qt.Resources.Exists(null));
            Assert.AreEqual("qrcUrl", ex.ParamName);
        }

        [TestMethod]
        public void Exists_EmptyUrl_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsExactly<ArgumentException>(() => Qt.Resources.Exists(""));
            Assert.AreEqual("qrcUrl", ex.ParamName);
        }

        [TestMethod]
        public void Exists_NonQrcUrl_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsExactly<ArgumentException>(
                () => Qt.Resources.Exists("https://example.com/icon.svg"));
            Assert.AreEqual("qrcUrl", ex.ParamName);
        }

        [TestMethod]
        public void ReadAllBytes_NullUrl_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsExactly<ArgumentException>(
                () => Qt.Resources.ReadAllBytes(null));
            Assert.AreEqual("qrcUrl", ex.ParamName);
        }

        [TestMethod]
        public void ReadAllText_NonQrcUrl_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsExactly<ArgumentException>(
                () => Qt.Resources.ReadAllText("file:///icon.svg"));
            Assert.AreEqual("qrcUrl", ex.ParamName);
        }

        // --- Behavior via injected FakeQtResources ---

        [TestMethod]
        public void ReadAllBytes_SizeNegative_ThrowsFileNotFoundException()
        {
            Qt.Resources.InstanceOverride = new FakeQtResources { SizeFn = _ => -1 };
            Assert.ThrowsExactly<FileNotFoundException>(
                () => Qt.Resources.ReadAllBytes("qrc:/foo.png"));
        }

        [TestMethod]
        public void ReadAllBytes_SizeZero_ReturnsEmptyArray()
        {
            Qt.Resources.InstanceOverride = new FakeQtResources { SizeFn = _ => 0 };
            var result = Qt.Resources.ReadAllBytes("qrc:/empty.bin");
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ReadAllBytes_ShortRead_ThrowsIOException()
        {
            Qt.Resources.InstanceOverride = new FakeQtResources
            {
                SizeFn = _ => 10,
                ReadFn = (_, _, _) => 5
            };
            Assert.ThrowsExactly<IOException>(() => Qt.Resources.ReadAllBytes("qrc:/data.bin"));
        }

        [TestMethod]
        public void ReadAllText_DefaultsToUtf8()
        {
            const string expected = "hello world";
            var bytes = Encoding.UTF8.GetBytes(expected);
            Qt.Resources.InstanceOverride = new FakeQtResources
            {
                SizeFn = _ => bytes.Length,
                ReadFn = (_, dest, len) => { Marshal.Copy(bytes, 0, dest, len); return len; }
            };
            Assert.AreEqual(expected, Qt.Resources.ReadAllText("qrc:/text.txt"));
        }

        [TestMethod]
        public void ReadAllText_HonorsSuppliedEncoding()
        {
            const string expected = "café";
            var encoding = Encoding.Latin1;
            var bytes = encoding.GetBytes(expected);
            Qt.Resources.InstanceOverride = new FakeQtResources
            {
                SizeFn = _ => bytes.Length,
                ReadFn = (_, dest, len) => { Marshal.Copy(bytes, 0, dest, len); return len; }
            };
            Assert.AreEqual(expected, Qt.Resources.ReadAllText("qrc:/text.txt", encoding));
        }

        // -------------------------------------------------------------------------

        private sealed class FakeQtResources : Qt.IQtResources
        {
            public Func<string, bool> ExistsFn { get; init; } = _ => false;
            public Func<string, int> SizeFn { get; init; } = _ => -1;
            public Func<string, IntPtr, int, int> ReadFn { get; init; } = (_, _, _) => -1;

            public bool Exists(string qrcUrl) => ExistsFn(qrcUrl);
            public int Size(string qrcUrl) => SizeFn(qrcUrl);
            public int Read(string qrcUrl, IntPtr destination, int destinationLength) =>
                ReadFn(qrcUrl, destination, destinationLength);
        }
    }
}
