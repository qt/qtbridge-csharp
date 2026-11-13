// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_CollectionCount
    {
        public TestContext TestContext { get; set; }

        private const string Source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            namespace AAA
            {
                public interface IOther { int Count { get; } }
            }
            namespace Test
            {
                public class List : IList<int>, IReadOnlyList<int>, AAA.IOther
                {
                    int ICollection<int>.Count => 42;
                    int IReadOnlyCollection<int>.Count => 43;
                    int AAA.IOther.Count => 999;
                    public int this[int index] { get => index; set { } }
                    public bool IsReadOnly => false;
                    public void Add(int item) { }
                    public void Clear() { }
                    public bool Contains(int item) => false;
                    public void CopyTo(int[] array, int index) { }
                    public int IndexOf(int item) => 0;
                    public void Insert(int index, int item) { }
                    public bool Remove(int item) => false;
                    public void RemoveAt(int index) { }
                    public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public sealed class DerivedList : List { }
                public class ReadOnly : IReadOnlyList<int>, AAA.IOther
                {
                    int IReadOnlyCollection<int>.Count => 42;
                    int AAA.IOther.Count => 999;
                    public int this[int index] => index;
                    public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public sealed class DerivedReadOnly : ReadOnly { }
                public class NonGeneric : IList, AAA.IOther
                {
                    int ICollection.Count => 42;
                    int AAA.IOther.Count => 999;
                    public object this[int index] { get => index; set { } }
                    public bool IsFixedSize => false;
                    public bool IsReadOnly => false;
                    public bool IsSynchronized => false;
                    public object SyncRoot => this;
                    public int Add(object item) => 0;
                    public void Clear() { }
                    public bool Contains(object item) => false;
                    public void CopyTo(Array array, int index) { }
                    public int IndexOf(object item) => 0;
                    public void Insert(int index, object item) { }
                    public void Remove(object item) { }
                    public void RemoveAt(int index) { }
                    public IEnumerator GetEnumerator() => throw new NotImplementedException();
                }
                public sealed class DerivedNonGeneric : NonGeneric { }
            }
            """;

        private const string MutableCount = "System.Collections.Generic.ICollection`1"
            + "[[System.Int32, System.Private.CoreLib]], System.Private.CoreLib";
        private const string ReadOnlyCount = "System.Collections.Generic.IReadOnlyCollection`1"
            + "[[System.Int32, System.Private.CoreLib]], System.Private.CoreLib";
        private const string NonGenericCount = "System.Collections.ICollection, System.Private.CoreLib";

        [TestMethod]
        [DataRow("list", MutableCount)]
        [DataRow("derivedlist", MutableCount)]
        [DataRow("readonly", ReadOnlyCount)]
        [DataRow("derivedreadonly", ReadOnlyCount)]
        [DataRow("nongeneric", NonGenericCount)]
        [DataRow("derivednongeneric", NonGenericCount)]
        public async Task Count_UsesCollectionContract(string file, string contract)
        {
            using var result = await TestCodeGenerator.GenerateAsync([Source],
                defaultExportOptions: Qt.ExportAs.SourceCode,
                ct: TestContext.CancellationTokenSource.Token);
            var header = result.Sink.Files[$"source/hpp/test/{file}.h"];
            var implementation = result.Sink.Files[$"source/cpp/test/{file}.cpp"];
            Assert.Contains("Q_PROPERTY(qint32 count READ count NOTIFY countChanged)", header);
            Assert.Contains($"method(\"{contract}::get_Count\", d->fnModelCount)", implementation);
        }
    }
}
