// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_TypeCastImplemention
    {
        private const string TypeCastHeaderPath = "source/hpp/qt/bridge/typecast.h";

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task TypeCast_IsGenerated_When_No_ReferenceTypes()
        {
            const string source = """
                namespace N1
                {
                    public struct S { public int X; }
                }
                namespace N2
                {
                    public enum E { A, B }
                }
            """;

            var result = await TestCodeGenerator.GenerateAsync([source],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(TypeCastHeaderPath, out _),
                "typecast.h missing.");
        }

        [TestMethod]
        public async Task TypeCast_DoesNotGenerate_As_For_Value_Enum_Interface_Abstract()
        {
            const string source = """
                namespace T
                {
                    public struct Point { public int X; }
                    public enum Color { Red, Green }
                    public interface IFace {}
                    public abstract class AbstractBase {}
                }
            """;

            var result = await TestCodeGenerator.GenerateAsync([source],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(TypeCastHeaderPath, out var typeCastHeader),
                "typecast.h missing.");

            Assert.MatchesRegex(new Regex(@"Q_INVOKABLE\s+[A-Za-z0-9_:]+\s*\*\s*as[A-Za-z0-9_]"
                    + @"+\s*\(\s*QObject\s*\*\s*obj\s*\)\s*;", RegexOptions.Multiline),
                typeCastHeader, "No as* invokable expected for value/enum/interface/abstract types.");
        }

        [TestMethod]
        public async Task TypeCast_As_Method_Signature_Uses_QObject_Param()
        {
            const string source = """
                namespace Foo.A {
                    public class Widget {}
                }
            """;

            var result = await TestCodeGenerator.GenerateAsync([source],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(TypeCastHeaderPath, out var typeCastHeader),
                "typecast.h missing.");

            Assert.MatchesRegex(new Regex(@"Q_INVOKABLE\s+Foo::A::Widget\s*\*\s*as[A-Za-z0-9_]"
                + @"+\s*\(\s*QObject\s*\*\s*obj\s*\)\s*;", RegexOptions.Multiline),
                typeCastHeader, "Expected at least one as* method with QObject* obj");
        }

        [TestMethod]
        public async Task TypeCast_Has_Qml_Singleton_Macros()
        {
            var result = await TestCodeGenerator.GenerateAsync(["namespace T { public class X {} }"],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(TypeCastHeaderPath, out var typeCastHeader),
                "typecast.h missing.");

            Assert.Contains("QML_ELEMENT", typeCastHeader);
            Assert.Contains("QML_SINGLETON", typeCastHeader);
        }

        [TestMethod]
        public async Task TypeCast_As_Method_Names_Are_Unique_For_Same_SimpleName()
        {
            const string source = """
                namespace Foo.A
                {
                    public class Widget { public Widget() {} }
                }
                namespace Bar.B
                {
                    public class Widget { public Widget() {} }
                }
            """;

            var result = await TestCodeGenerator.GenerateAsync([source],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(TypeCastHeaderPath, out var typeCastHeader),
                "typecast.h missing.");

            var rx = new Regex(@"Q_INVOKABLE\s+(?<ret>[A-Za-z_][A-Za-z0-9_:]*)\s*\*\s*(?<name>as"
                + @"[A-Za-z0-9_]+)\s*\(\s*QObject\s*\*\s*obj\s*\)\s*;", RegexOptions.Multiline);

            var matches = rx.Matches(typeCastHeader).ToArray();
            var widgets = matches
                .Where(m => m.Groups["ret"].Value is "Foo::A::Widget" or "Bar::B::Widget")
                .ToArray();

            Assert.AreEqual(2, widgets.Length, "Expected 2 as*-methods for both widget types.");

            var names = widgets.Select(m => m.Groups["name"].Value).ToList();
            var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).ToList();

            if (duplicates.Any()) {
                var detail = string.Join("\n", duplicates
                    .Select(d => $"  {d.Key} -> [{string.Join(", ", widgets
                        .Where(m => m.Groups["name"].Value == d.Key)
                        .Select(m => m.Groups["ret"].Value)
                        .Distinct())}]"));
                Assert.Fail($"Collision found (overload by return type is illegal in C++):\n{detail}");
            }
        }
    }
}
