/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.DotNet.Generator
{
    using Qt.DotNet.CodeGeneration;
    using Support;

    [TestClass]
    public class Test_RuleDependsOn
    {
        private static volatile bool IsRuleAFinished;
        private static readonly ConcurrentQueue<string> Log = new();

        private static void ResetTestState()
        {
            while (Log.TryDequeue(out _))
            { }
            IsRuleAFinished = false;
            Rules.Reset();
            FilePlaceholder.All.Reset();
        }

        private static MemberInfo FindTypeByFullName(string fullName)
        {
            var graph = Rules.SourceGraph;
            var b = graph?
                .Where(kv => kv.Key is { } type && (type.FullName ?? type.Name) == "Dep.B")
                .Select(kv => kv.Key)
                .FirstOrDefault();
            return b?.Assembly?.GetType(fullName);
        }

        private class RuleA_Succeeds : Rule
        {
            public override bool Matches(MemberInfo source)
                => source is Type type && (type.FullName ?? type.Name) == "Dep.A";

            public override Task<Result> ExecuteAsync(MemberInfo source)
            {
                Log.Enqueue("A:start");
                IsRuleAFinished = true;
                Log.Enqueue("A:end");
                return Task.FromResult(Ok);
            }
        }

        private class RuleA_Fails : Rule
        {
            public override bool Matches(MemberInfo source)
                => source is Type type && (type.FullName ?? type.Name) == "Dep.A";

            public override Task<Result> ExecuteAsync(MemberInfo source)
            {
                Log.Enqueue("A:fail");
                return Task.FromResult(Error("A failed intentionally"));
            }
        }

        private class RuleA_DependsOnB : Rule
        {
            public override bool Matches(MemberInfo source)
                => source is Type type && (type.FullName ?? type.Name) == "Dep.A";

            public override IEnumerable<MemberInfo> DependsOn
            {
                get {
                    var b = FindTypeByFullName("Dep.B");
                    return b is null ? Array.Empty<MemberInfo>() : new[] { b };
                }
            }

            public override Task<Result> ExecuteAsync(MemberInfo source)
            {
                Log.Enqueue("A:start");
                Log.Enqueue("A:end");
                return Task.FromResult(Ok);
            }
        }

        private class RuleB_DependsOnA : Rule
        {
            public override IEnumerable<MemberInfo> DependsOn
            {
                get {
                    var a = FindTypeByFullName("Dep.A");
                    return a is null ? Array.Empty<MemberInfo>() : new[] { a };
                }
            }

            public override bool Matches(MemberInfo source)
                => source is Type type && (type.FullName ?? type.Name) == "Dep.B";

            public override Task<Result> ExecuteAsync(MemberInfo source)
            {
                Log.Enqueue("B:start");
                if (!IsRuleAFinished)
                    return Task.FromResult(Error("B ran before A finished"));
                Log.Enqueue("B:end");
                return Task.FromResult(Ok);
            }
        }

        private const string Source = """
            namespace Dep {
                public class A { public A() {} }
                public class B { public B() {} }
            }
        """;

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task DependsOn_B_Runs_After_A()
        {
            ResetTestState();
            await TestCodeGenerator.GenerateAsync([Source],
                extraRules: [typeof(RuleA_Succeeds), typeof(RuleB_DependsOnA)],
                ct: TestContext.CancellationTokenSource.Token);

            var log = Log.ToArray();
            var aEndIndex = Array.FindIndex(log, s => s == "A:end");
            var bStartIndex = Array.FindIndex(log, s => s == "B:start");

            Assert.IsTrue(aEndIndex >= 0, "A:end missing");
            Assert.IsTrue(bStartIndex >= 0, "B:start missing");
            Assert.IsTrue(aEndIndex < bStartIndex, "B must start after A:end (DependsOn not respected).");
            Assert.IsFalse(log.Any(s => s.Contains("fail", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public async Task DependsOn_B_Is_Suppressed_When_A_Fails()
        {
            ResetTestState();
            try {
                _ = await TestCodeGenerator.GenerateAsync(
                    [Source],
                    extraRules: [typeof(RuleA_Fails), typeof(RuleB_DependsOnA)],
                    ct: TestContext.CancellationTokenSource.Token);
                Assert.Fail("Generator should have failed when A failed.");
            } catch (InvalidOperationException ex) {
                Assert.Contains("failed", ex.Message, StringComparison.OrdinalIgnoreCase);
            }

            var log = Log.ToArray();
            Assert.IsTrue(log.Contains("A:fail"), "A:fail not logged");
            Assert.IsFalse(log.Contains("B:start"), "B should not run when A fails.");
        }

        [TestMethod]
        public async Task DependsOn_MissingDependency_Fails_B()
        {
            const string onlyB
                = "namespace Dep { internal class A { } public class B { public B() {} } }";

            ResetTestState();
            try {
                _ = await TestCodeGenerator.GenerateAsync([onlyB],
                    extraRules: [typeof(RuleB_DependsOnA)],
                    ct: TestContext.CancellationTokenSource.Token);
                Assert.Fail("Generator should have failed (missing dependency A).");
            } catch (InvalidOperationException ex) {
                Assert.Contains("failed", ex.Message, StringComparison.OrdinalIgnoreCase);
            }

            var log = Log.ToArray();
            Assert.IsFalse(log.Contains("B:start"), "B should not run without A.");
        }

        [TestMethod]
        public async Task DependsOn_Cycle_ShouldBeDetected_AndFailFast()
        {
            ResetTestState();

            var generatorTaskObject = TestCodeGenerator.GenerateAsync([Source],
                extraRules: [typeof(RuleA_DependsOnB), typeof(RuleB_DependsOnA)],
                ct: TestContext.CancellationTokenSource.Token);

            var finishedTaskObject = await Task.WhenAny(generatorTaskObject, Task.Delay(1000));
            Assert.AreEqual(generatorTaskObject, finishedTaskObject,
                "Cycle NOT detected: Generation did not complete fast (it is hanging).");

            try {
                await generatorTaskObject;
                Assert.Fail("Generation completed without error despite a dependency cycle.");
            } catch (InvalidOperationException ex) {
                Assert.Contains("fail", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
