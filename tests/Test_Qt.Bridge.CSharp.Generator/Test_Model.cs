// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qt.Bridge.Models;
using Qt.DotNet;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_Model
    {
        public TestContext TestContext { get; set; }

        private static readonly Assembly ApiAssembly = typeof(Model).Assembly;
        private static readonly Assembly AdapterAssembly = typeof(ModelIndex).Assembly;

        private const string SourceWithIgnoredModelOverride = """
            using System.Collections.Generic;
            using Qt;
            using Qt.Bridge.Models;
            using Qt.DotNet;

            namespace Test
            {
                public class MyItemModel : Model
                {
                    public override int RowCount(ModelIndex parent) => 0;
                    public override int ColumnCount(ModelIndex parent) => 0;
                    public override ModelIndex Index(int row, int column, ModelIndex parent) => default;
                    public override ModelIndex Parent(ModelIndex index) => default;
                    public override object Data(ModelIndex index, int role) => null;

                    public override Dictionary<int, string> RoleNames() => new();

                    [Qt.Ignore]
                    public override bool CanFetchMore(ModelIndex parent) => false;
                }
            }
            """;

        [TestMethod]
        public async Task Ignored_ModelOverride_IsNotGenerated()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                [SourceWithIgnoredModelOverride],
                sourceRefs: [ApiAssembly, AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);

            var combined = result.CombinedText;

            Assert.Contains("roleNames", combined,
                "Non-ignored override RoleNames must appear in the generated output.");
            Assert.DoesNotContain("canFetchMore", combined,
                "Ignored override CanFetchMore must not appear in the generated output.");
        }
    }
}
