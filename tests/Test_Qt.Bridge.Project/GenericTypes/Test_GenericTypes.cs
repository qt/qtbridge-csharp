// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.GenericTypes
{
    [TestClass]
    public class Test_GenericTypes : ManagedTestBase
    {
        [TestMethod]
        public async Task SourceCode_GenericClassesAndArrays_Build()
        {
            using var temp = new TempProject();
            temp.Create(new() {
                PackageReferences = [Packages.QtBridge],
                AfterSdkProps = """
                    <PropertyGroup>
                      <QtExportAs>source</QtExportAs>
                    </PropertyGroup>
                    """
            });
            temp.AddFile("Program.cs", """
                namespace MyApp;
                public static class Program { public static void Main() { } }
                public class Foo { }
                public class Foo<T> { }
                public class Foo<T, U> { }
                public class Outer<T> { public enum State { Off, On } }
                public class Bar
                {
                    public Foo Plain { get; set; }
                    public Foo<int> Number { get; set; }
                    public Foo<string> Text { get; set; }
                    public Foo<int, string> Pair { get; set; }
                    public Foo<Foo> Object { get; set; }
                    public Foo<Foo<int>> Nested { get; set; }
                    public Foo[] PlainArray { get; set; }
                    public Foo<int>[] NumberArray { get; set; }
                    public Foo<string>[] TextArray { get; set; }
                    public Foo<Foo>[] ObjectArray { get; set; }
                    public Foo<Foo<int>>[] NestedArray { get; set; }
                    public Outer<int>.State NumberState { get; set; }
                    public Outer<string>.State TextState { get; set; }
                }
                """);
            var build = await temp.BuildAsync();
            temp.SaveLog();
            Assert.IsTrue(build.Ok, build.Output);
        }
        [TestMethod]
        public async Task SourceCode_GenericEnumProperties_RoundTripInQml()
        {
            using var temp = new TempProject();
            var options = CreateQtQuickTestOptions(
                Path.Combine("Models", "ModelFixtures", "main.cpp"));
            options.AfterSdkProps = """
                <PropertyGroup>
                  <QtExportAs>source</QtExportAs>
                </PropertyGroup>
                """;
            await InitializeAndBuildAsync(temp, options, project => {
                project.AddFile("Program.cs", """
                    using Qt.Quick;
                    [assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]
                    namespace MyApp;
                    public static class Program { public static void Main() => Qml.WaitForExit(); }
                    public class Outer<T> { public enum State { Off, On } }
                    [QmlElement]
                    public class EnumProperties
                    {
                        public Outer<int>.State Number { get; set; }
                        public Outer<string>.State Text { get; set; }
                    }
                    """);
                project.AddFile("tst_genericenums.qml", """
                    import QtQuick
                    import QtTest
                    import Application
                    TestCase {
                        name: "GenericEnumSetters"
                        EnumProperties { id: fixture }
                        function test_roundTrip() {
                            compare(fixture.number, 0)
                            compare(fixture.text, 0)
                            fixture.number = 1
                            compare(fixture.number, 1)
                            compare(fixture.text, 0)
                            fixture.text = 1
                            compare(fixture.text, 1)
                            fixture.number = 0
                            compare(fixture.number, 0)
                            compare(fixture.text, 1)
                        }
                    }
                    """);
            });
            var run = await temp.RunAsync(new() {
                Args = ["-input", Path.Combine(temp.ExeDir, "Application", "tst_genericenums.qml")],
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1"), ("QML_DISABLE_DISK_CACHE", "1")],
                StdErr = Redirect.StdOut
            });
            Assert.AreEqual(0, run.ExitCode, run.StdOut);
            Assert.Contains("GenericEnumSetters::test_roundTrip()", run.StdOut);
            Assert.Contains("PASS   :", run.StdOut);
        }
    }
}
