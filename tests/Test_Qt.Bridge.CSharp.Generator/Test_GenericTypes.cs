// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Qt.Bridge.CodeGeneration;
    using Qt.Bridge.CodeGeneration.MetaFunctions;
    using Support;

    [TestClass]
    public class Test_GenericTypes
    {
        public TestContext TestContext { get; set; }

        public class Foo { }
        public class Foo<T> { }
        public class Outer<T>
        {
            public enum State { Off, On }
        }

        private sealed class FilePaths : Paths
        {
            public string File(Type type) => Eval(type, TraitFlags.File);
        }

        [TestMethod]
        public void GenericFileNames_AreUniquePerFileOwner()
        {
            Type[] types = [
                typeof(Foo), typeof(Foo<int>), typeof(Foo[]), typeof(Foo<int>[]),
                typeof(Foo<Foo>), typeof(Foo<Foo<int>>), typeof(Foo<string>[]),
                typeof(Foo<Foo>[]), typeof(Foo<Foo<int>>[]),
                typeof(Foo<int[]>[]), typeof(Foo<int[,]>[]),
                typeof(Foo<int>[][]), typeof(Foo<int>[,]),
                typeof(Outer<int>.State), typeof(Outer<string>.State),
                typeof(Foo).MakeArrayType(1)
            ];
            var paths = new FilePaths();
            foreach (var group in types.GroupBy(paths.File)) {
                var owners = group.Select(type => type.IsGenericType && !type.IsEnum
                    ? type.GetGenericTypeDefinition() : type).Distinct().ToArray();
                Assert.HasCount(1, owners,
                    $"Different file owners share {group.Key}: {string.Join(", ", owners.Select(t => t.ToString()))}");
                Assert.IsFalse(group.Key.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0,
                    group.Key);
            }
            Assert.AreEqual(paths.File(typeof(Foo<int>)), paths.File(typeof(Foo<Foo<int>>)));
        }

        [TestMethod]
        public async Task GenericArities_GetSeparateFiles()
        {
            const string source = """
                namespace MyApp;
                public class Foo { }
                public class Foo<T> { }
                public class Foo<T, U> { }
                public class Bar
                {
                    public Foo Plain { get; set; }
                    public Foo<int> Number { get; set; }
                    public Foo<int, string> Pair { get; set; }
                }
                """;
            using var result = await Generate(source);
            var includes = result.Sink.Files["source/hpp/myapp/bar.h"];
            foreach (var (file, name) in new[] {
                ("foo", "Foo"), ("foo_1", "Foo_1__Int32"),
                ("foo_2", "Foo_2__Int32__String")
            }) {
                Assert.Contains($"class MyApp::{name} :", result.Sink.Files[$"source/hpp/myapp/{file}.h"]);
                Assert.Contains($"#include <myapp/{file}.h>", includes);
                Assert.Contains($"#include <myapp/{file}.h>", result.Sink.Files[$"source/cpp/myapp/{file}.cpp"]);
            }
        }

        [TestMethod]
        public async Task ConstructedGenericTypes_ShareDefinitionsAndIncludes()
        {
            const string source = """
                namespace MyApp;
                public class Foo<T> { }
                public class Leaf { }
                public class Bar
                {
                    public Foo<int> Number { get; set; }
                    public Foo<string> Text { get; set; }
                    public Foo<Leaf> Leaf { get; set; }
                    public Foo<Foo<int>> Nested { get; set; }
                }
                """;
            using var result = await Generate(source);
            var header = result.Sink.Files["source/hpp/myapp/foo_1.h"];
            foreach (var name in new[] { "Foo_1__Int32", "Foo_1__String", "Foo_1__Leaf", "Foo_1__Foo_1__Int32" })
                Assert.Contains($"class MyApp::{name} :", header);
            Assert.Contains("#include <myapp/foo_1.h>", result.Sink.Files["source/hpp/myapp/bar.h"]);
            Assert.AreEqual(1, result.Sink.Files.Keys.Count(file =>
                file.StartsWith("source/hpp/myapp/foo")), "All constructions must share one header.");
        }

        [TestMethod]
        public async Task ArraysOfGenericTypes_GetConcreteFilesAndDefinitionIncludes()
        {
            const string source = """
                namespace MyApp;
                public class Foo { }
                public class Foo<T> { }
                public class Bar
                {
                    public Foo[] Plain { get; set; }
                    public Foo<int>[] Numbers { get; set; }
                    public Foo<string>[] Texts { get; set; }
                    public Foo<Foo>[] Objects { get; set; }
                    public Foo<Foo<int>>[] Nested { get; set; }
                }
                """;
            using var result = await Generate(source);
            var bar = result.Sink.Files["source/hpp/myapp/bar.h"];
            foreach (var (file, name) in new[] {
                ("array_foo", "Array_Foo"),
                ("array_foo_1__system_int32", "Array_Foo_1__Int32"),
                ("array_foo_1__system_string", "Array_Foo_1__String"),
                ("array_foo_1__myapp_foo", "Array_Foo_1__Foo"),
                ("array_foo_1__myapp_foo_1__system_int32", "Array_Foo_1__Foo_1__Int32")
            }) {
                Assert.Contains($"class MyApp::{name} :", result.Sink.Files[$"source/hpp/myapp/{file}.h"]);
                Assert.Contains($"#include <myapp/{file}.h>", bar);
                Assert.Contains($"#include <myapp/{file}.h>", result.Sink.Files[$"source/cpp/myapp/{file}.cpp"]);
            }
            Assert.Contains("#include <myapp/foo_1.h>",
                result.Sink.Files["source/hpp/myapp/array_foo_1__system_int32.h"]);
        }

        [TestMethod]
        public async Task EnumsNestedInGenericTypes_GetConcreteFiles()
        {
            const string source = """
                namespace MyApp;
                public class Outer<T> { public enum State { Off, On } }
                public class Bar
                {
                    public Outer<int>.State Number { get; set; }
                    public Outer<string>.State Text { get; set; }
                }
                """;
            using var result = await Generate(source);
            var bar = result.Sink.Files["source/hpp/myapp/bar.h"];
            foreach (var (file, name) in new[] {
                ("outer_1_state__system_int32", "Outer_State_1__Int32"),
                ("outer_1_state__system_string", "Outer_State_1__String")
            }) {
                Assert.Contains($"using {name} =", result.Sink.Files[$"source/hpp/myapp/{file}.h"]);
                Assert.Contains($"#include <myapp/{file}.h>", bar);
            }
        }

        private Task<TestCodeGenerator.Result> Generate(string source)
            => TestCodeGenerator.GenerateAsync([source], defaultExportOptions: Qt.ExportAs.SourceCode,
                ct: TestContext.CancellationTokenSource.Token);
    }
}
