// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_ObjectDispatch
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task ObjectDispatch_UsesStableTypeNamesForRegistryAndLookup()
        {
            const string source = """
                namespace DispatchTypes
                {
                    public class Payload<T>
                    {
                        public T Value { get; set; }
                    }

                    public class Source
                    {
                        public Payload<int> GetPayload() => new();
                    }
                }
            """;

            using var result = await TestCodeGenerator.GenerateAsync([source],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/cpp/object_dispatch.cpp",
                out var dispatchCpp));

            var coreLib = typeof(int).Assembly.GetName().Name;
            Assert.Contains($"QStringLiteral(\"DispatchTypes.Payload`1[[System.Int32, {coreLib}]], "
                    + result.SourceAssembly.GetName().Name + "\")", dispatchCpp);
            Assert.Contains("key = args.type().stableAssemblyQualifiedName()", dispatchCpp);

            Assert.DoesNotContain("Version=", dispatchCpp);
            Assert.DoesNotContain("Culture=", dispatchCpp);
            Assert.DoesNotContain("PublicKeyToken=", dispatchCpp);
        }
    }
}
