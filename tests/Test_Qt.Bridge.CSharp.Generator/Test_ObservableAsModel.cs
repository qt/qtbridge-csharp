// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_ObservableAsModel
    {
        public TestContext TestContext { get; set; }

        private const string Source = """
            using System;
            using System.Collections.ObjectModel;

            namespace Test
            {
                public class Foo
                {
                    public ObservableCollection<string> A { get; set; }
                    public ReadOnlyObservableCollection<int> B { get; set; }
                }

                public class PersonName
                {
                    public string FirstName { get; set; }
                    public string LastName  { get; set; }
                    public PersonName(string first, string last)
                    {
                        FirstName = first; LastName = last;
                    }
                }

                public class NameList : ObservableCollection<PersonName>
                {
                    public NameList()
                    {
                        Add(new PersonName("Willa", "Cather"));
                        Add(new PersonName("Isak", "Dinesen"));
                        Add(new PersonName("Victor", "Hugo"));
                        Add(new PersonName("Jules", "Verne"));
                    }
                }
            }
            """;
        private const string PathPrefix = "source/hpp/system/collections/objectmodel";

        [TestMethod]
        public async Task ObservableCollections_AreExposedAsQAbstractListModel()
        {
            var result = await TestCodeGenerator.GenerateAsync([Source],
                sourceRefs: [typeof(ObservableCollection<>).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files
                .TryGetValue($"{PathPrefix}/observablecollection.h", out var oc),
                "Missing 'observablecollection.h' in generated outputs.");

            Assert.MatchesRegex(@"class\s+System::Collections::ObjectModel::ObservableCollection_1__"
                + @"String\s*:\s*public\s+QAbstractListModel,",
                oc, "ObservableCollection<T> should be generated as a QAbstractListModel");

            Assert.IsTrue(result.Sink.Files
                    .TryGetValue($"{PathPrefix}/readonlyobservablecollection.h", out var roc),
                "Missing 'readonlyobservablecollection.h' in generated outputs.");

            Assert.MatchesRegex(@"class\s+System::Collections::ObjectModel::"
                + @"ReadOnlyObservableCollection_1__Int32\s*:\s*public\s+QAbstractListModel,",
                roc, "ReadOnlyObservableCollection<T> should be generated as a QAbstractListModel");

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/namelist.h", out var n),
                "Missing 'namelist.h' in generated outputs.");

            Assert.MatchesRegex(@"class\s+Test::NameList\s*:\s*public\s+QAbstractListModel,",
                n, "Derived ObservableCollection should surface as a QAbstractListModel");
        }

        [TestMethod]
        public async Task ObservableCollection_ModelSurface_And_Wrappers_Present()
        {
            var result = await TestCodeGenerator.GenerateAsync([Source],
                sourceRefs: [typeof(ObservableCollection<>).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files
                .TryGetValue($"{PathPrefix}/observablecollection.h", out var hpp),
                "Missing 'observablecollection.h' in generated outputs.");

            Assert.Contains("class System::Collections::ObjectModel::ObservableCollection_1__", hpp,
                "Generic ObservableCollection<T> class name was not found.");

            Assert.MatchesRegex(@"class\s+System::Collections::ObjectModel::ObservableCollection"
                + @"(?:_\d+__\w+)?\s*:\s*public\s+QAbstractListModel,",
                hpp, "ObservableCollection<T> should derive from QAbstractListModel.");

            Assert.Contains("void handleCollectionChanged(QObject* qEvArgs);", hpp,
                "handleCollectionChanged declaration missing.");

            Assert.MatchesRegex(new Regex(@"Q_SIGNAL\s+void\s+collectionChanged\s*\(", RegexOptions
                .Singleline), hpp, "collectionChanged signal missing.");

            Assert.MatchesRegex(new Regex(@"void\s+connectNotify\s*\(\s*const\s+QMetaMethod\s*&\s*"
                + @"signal\s*\)\s*override", RegexOptions.Singleline),
                hpp, "connectNotify override missing.");
        }

        [TestMethod]
        public async Task Observable_HandleCollectionChanged_Covers_All_Actions_And_Uses_Wrappers()
        {
            var result = await TestCodeGenerator.GenerateAsync([Source],
                sourceRefs: [typeof(ObservableCollection<>).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(
                "source/cpp/system/collections/objectmodel/observablecollection.cpp", out var cpp),
                "Missing 'observablecollection.cpp' in generated outputs.");

            Assert.Contains("#include <system/collections/objectmodel/observablecollection.h>", cpp,
                "Missing '#include <observablecollection.h>' in generated sources.");

            Assert.Contains("#include <QMetaMethod>", cpp,
                "Missing '#include <QMetaMethod>' in generated sources for ObservableCollection.");

            Assert.Contains("#include <QThread>", cpp,
                "Missing '#include <QThread>' in generated sources for ObservableCollection.");

            Assert.MatchesRegex(@"void\s+System::Collections::ObjectModel::ObservableCollection_1__"
                + @"\w+::handleCollectionChanged\(QObject\*\s+qEvArgs\)",
                cpp, "handleCollectionChanged definition missing.");

            const string pattern = @"case\s+System::Collections::Specialized::"
                + @"NotifyCollectionChangedAction::(Add|Remove|Replace|Move|Reset)\s*:(\s*//.*)?";

            string[] expectedCases = ["Add", "Remove", "Replace", "Move", "Reset"];
            var matches = Regex.Matches(cpp, pattern);
            foreach (var expectedCase in expectedCases) {
                var found = matches.Any(m => m.Groups[1].Value == expectedCase);
                Assert.IsTrue(found, $"The expected case '{expectedCase}' was not found.");
            }
        }

        private const string ObservableListSource = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Collections.Specialized;

            namespace Test
            {
                public class PersonName
                {
                    public string FirstName { get; set; }
                    public string LastName  { get; set; }
                }

                public class ObservableList<T> : IList<T>, INotifyCollectionChanged
                {
                    private readonly List<T> _items = [];
                    public event NotifyCollectionChangedEventHandler CollectionChanged;

                    public void Add(T item)
                    {
                        _items.Add(item);
                        CollectionChanged?.Invoke(this,
                            new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Add, item, _items.Count - 1));
                    }

                    public T this[int index]
                    {
                        get => throw new NotImplementedException();
                        set => throw new NotImplementedException();
                    }
                    public int Count => throw new NotImplementedException();
                    public bool IsReadOnly => throw new NotImplementedException();
                    public void Clear() => throw new NotImplementedException();
                    public bool Contains(T item) => throw new NotImplementedException();
                    public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();
                    public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
                    public int IndexOf(T item) => throw new NotImplementedException();
                    public void Insert(int index, T item) => throw new NotImplementedException();
                    public bool Remove(T item) => throw new NotImplementedException();
                    public void RemoveAt(int index) => throw new NotImplementedException();
                    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
                }

                public class ViewModel
                {
                    public ObservableList<PersonName> People { get; } = new();
                }
            }
            """;

        [TestMethod]
        public async Task ObservableList_IsExposed_As_QAbstractListModel()
        {
            var result = await TestCodeGenerator.GenerateAsync([ObservableListSource],
                sourceRefs: [typeof(ObservableCollection<>).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files
                    .TryGetValue("source/hpp/test/observablelist.h", out var hpp),
                "Missing 'observablelist.h' in generated outputs.");

            Assert.IsTrue(result.Sink.Files.TryGetValue(
                    "source/cpp/test/observablelist.cpp", out var cpp),
                "Missing 'observablelist.cpp' in generated outputs.");

            Assert.Contains("public QAbstractListModel", hpp,
                "ObservableList<T> should surface as QAbstractListModel.");

            Assert.Contains("void handleCollectionChanged(QObject* qEvArgs);", hpp,
                "Missing handleCollectionChanged declaration on the generated class.");

            const string pattern = @"case\s+System::Collections::Specialized::"
              + @"NotifyCollectionChangedAction::(Add|Remove|Replace|Move|Reset)\s*:(\s*//.*)?";

            string[] expectedCases = ["Add", "Remove", "Replace", "Move", "Reset"];
            var matches = Regex.Matches(cpp, pattern);
            foreach (var expectedCase in expectedCases) {
                var found = matches.Any(m => m.Groups[1].Value == expectedCase);
                Assert.IsTrue(found, $"The expected case '{expectedCase}' was not found.");
            }
        }
    }
}
