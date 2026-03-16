// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_ListAsModel
    {
        public TestContext TestContext { get; set; }

        public const string Source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            namespace Test {
                public class Foo
                {
                    public ArrayList X { get; set; }
                    public List<string> Y { get; set; }
                    public ArraySegment<int> Z { get; set; }
                    public Foo[] W { get; set; }
                }
            }
            """;

        [TestMethod]
        public async Task ListAsModel()
        {
            using var result = await TestCodeGenerator.GenerateAsync([Source],
                ct: TestContext.CancellationTokenSource.Token);
            Assert.IsTrue(result.Sink.Files.TryGetValue(
                "source/hpp/system/collections/arraylist.h", out var x) && Regex.IsMatch(x,
                @"class System::Collections::ArrayList\s+:\s+public\s+QAbstractListModel,"));
            Assert.IsTrue(result.Sink.Files.TryGetValue(
                "source/hpp/system/collections/generic/list.h", out var y) && Regex.IsMatch(y,
                @"class System::Collections::Generic::List_1__String\s+:\s+public\s+QAbstractListModel,"));
            Assert.IsTrue(result.Sink.Files.TryGetValue(
                "source/hpp/system/arraysegment.h", out var z) && Regex.IsMatch(z,
                @"class System::ArraySegment_1__Int32\s+:\s+public\s+QAbstractListModel,"));
            Assert.IsTrue(result.Sink.Files.TryGetValue(
                "source/hpp/test/array_foo.h", out var w) && Regex.IsMatch(w,
                @"class Test::Array_Foo\s+:\s+public\s+QAbstractListModel,"));
        }

        private const string SourceWithArray = """
            using System;
            namespace Test
            {
                public class PersonName
                {
                    public string FirstName { get; set; }
                    public string LastName  { get; set; }
                    public PersonName(string f, string l) { FirstName=f; LastName=l; }
                }
                public class Foo { public PersonName[] Names { get; set; } = new PersonName[0]; }
            }
            """;

        [TestMethod]
        public async Task ArrayOfComplexItems_DeclaresItemAndPropertyRoles_AndProvidesDataBranches()
        {
            using var result = await TestCodeGenerator.GenerateAsync([SourceWithArray],
                ct: TestContext.CancellationTokenSource.Token);

            const string sourceFile = "source/cpp/test/array_personname.cpp";
            Assert.IsTrue(result.Sink.Files.TryGetValue(sourceFile, out var cpp),
                $"Expected generated cpp ({sourceFile}), not found in generated output.");

            Assert.Contains("\"item\"", cpp);
            Assert.Contains("\"firstName\"", cpp);
            Assert.Contains("\"lastName\"", cpp);

            Assert.Contains("Array_PersonName::data", cpp);
            Assert.Contains("firstName(", cpp);
            Assert.Contains("lastName(", cpp);
        }

        private const string SourceWithList = """
            using System;
            using System.Collections.Generic;

            namespace Test
            {
                public class PersonName
                {
                    public string FirstName { get; set; }
                    public string LastName  { get; set; }

                    public PersonName(string first, string last)
                    {
                        FirstName = first;
                        LastName  = last;
                    }
                }

                public class NameList : List<PersonName>
                {
                    public NameList()
                    {
                        Add(new PersonName("Willa",  "Cather"));
                        Add(new PersonName("Isak",   "Dinesen"));
                        Add(new PersonName("Victor", "Hugo"));
                        Add(new PersonName("Jules",  "Verne"));
                    }
                }
            }
            """;

        private const string File = "source/cpp/test/namelist.cpp";

        [TestMethod]
        public async Task ListOfComplexItems_DeclaresItemAndPropertyRoles_AndProvidesDataBranches()
        {
            using var result = await TestCodeGenerator.GenerateAsync([SourceWithList],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(File, out var cpp),
                $"Expected generated cpp for Test::NameList not found ({File}).");

            // roleNames(): contains "item", "firstName", "lastName"
            Assert.Contains("\"item\"", cpp);
            Assert.Contains("\"firstName\"", cpp);
            Assert.Contains("\"lastName\"", cpp);

            // roleNames(): explicit mapping of role IDs
            Assert.MatchesRegex(@"\{[^}]*Qt::UserRole\s*,\s*""item""", cpp,
                "roleNames(): missing mapping for 'item' -> Qt::UserRole.");

            Assert.MatchesRegex(@"\{[^}]*Qt::UserRole\s*\+\s*1\s*,\s*""firstName""", cpp,
                "roleNames(): missing mapping for 'firstName' -> Qt::UserRole + 1.");

            Assert.MatchesRegex(@"\{[^}]*Qt::UserRole\s*\+\s*2\s*,\s*""lastName""", cpp,
                "roleNames(): missing mapping for 'lastName' -> Qt::UserRole + 2.");

            // data(): correct branches + getter calls for the correct role IDs
            Assert.MatchesRegex(new Regex(@"NameList::data\s*\([^)]*\)\s*const[\s\S]*if\s*\(\s*role\s*"
                + @"==\s*Qt::UserRole\s*\+\s*1\s*\)[\s\S]*firstName\s*\(", RegexOptions.Singleline),
                cpp, "data(): missing or wrong branch for Qt::UserRole + 1 (firstName).");

            Assert.MatchesRegex(new Regex(@"NameList::data\s*\([^)]*\)\s*const[\s\S]*if\s*\(\s*role\s*"
                + @"==\s*Qt::UserRole\s*\+\s*2\s*\)[\s\S]*lastName\s*\(", RegexOptions.Singleline),
                cpp, "data(): missing or wrong branch for Qt::UserRole + 2 (lastName).");
        }

        [TestMethod]
        public async Task ItemRole_ReturnsQObjectPointer_And_PersonName_DeclaresProperties()
        {
            using var result = await TestCodeGenerator.GenerateAsync([SourceWithList],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(File, out var cpp),
                $"Expected generated cpp for Test::NameList not found ({File}).");

            // roleNames() method signature
            Assert.MatchesRegex(@"QHash<int,\s*QByteArray>\s+Test::NameList::roleNames\s*\(\s*\)\s"
                + "*const", cpp, "roleNames() method signature not found on Test::NameList.");

            // "item" role present
            Assert.Contains("\"item\"", "roleNames() does not declare the \"item\" role.", cpp);

            // data(): item role returns QObject*
            Assert.MatchesRegex(new Regex(@"QVariant\s+Test::NameList::data\s*\([^)]*\)\s*const[\s\"
                + @"S]*role\s*==\s*Qt::UserRole[\s\S]*QVariant::fromValue\s*\(\s*item"
                + @"\s*\(\s*index\.row\(\)\s*\)\s*\)", RegexOptions.Singleline), cpp,
                "data() returns no QVariant::fromValue<QObject*>(item(index.row())) for item role.");

            const string header = "source/hpp/test/personname.h";
            Assert.IsTrue(
                result.Sink.Files.TryGetValue(header, out var hpp),
                $"Expected generated header for Test::PersonName not found ({header}).");

            // PersonName has the expected Q_PROPERTY declarations
            Assert.Contains("Q_PROPERTY(QString firstName READ firstName WRITE setFirstName)", hpp,
                "Q_PROPERTY for firstName not found on Test::PersonName.");

            Assert.Contains("Q_PROPERTY(QString lastName READ lastName WRITE setLastName)", hpp,
                "Q_PROPERTY for lastName not found on Test::PersonName.");
        }
    }
}
