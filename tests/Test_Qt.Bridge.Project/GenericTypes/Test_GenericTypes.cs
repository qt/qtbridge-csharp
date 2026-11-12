// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

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
                }
                """);
            var build = await temp.BuildAsync();
            temp.SaveLog();
            Assert.IsTrue(build.Ok, build.Output);
        }
    }
}
