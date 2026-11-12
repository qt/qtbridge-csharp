// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_Delegates
    {
        public TestContext TestContext { get; set; }

        // The property brings in Action, the collection brings in Action<ProblemClass>.
        [TestMethod]
        public async Task SourceCode_GenericAndNonGenericDelegate_GetOwnHeaders()
        {
            const string source = """
                using System;
                using System.Collections.ObjectModel;

                namespace Test
                {
                    public sealed class ReferencingClass
                    {
                        public ObservableCollection<ProblemClass> Problems { get; set; }
                    }

                    public sealed class ProblemClass
                    {
                        public Action ProblemProperty { get; }
                    }
                }
                """;

            using var result = await TestCodeGenerator.GenerateAsync([source],
                sourceRefs: [typeof(System.Collections.ObjectModel.ObservableCollection<>).Assembly],
                defaultExportOptions: Qt.ExportAs.SourceCode,
                ct: TestContext.CancellationTokenSource.Token);

            Assert.Contains("struct System::Action :", result.Sink.Files["source/hpp/system/action.h"]);
            Assert.Contains("struct System::Action_1__ProblemClass :",
                result.Sink.Files["source/hpp/system/action_1.h"]);
            Assert.Contains("#include <system/action.h>",
                result.Sink.Files["source/hpp/test/problemclass.h"]);
            Assert.Contains("#include <system/action_1.h>",
                result.Sink.Files["source/hpp/system/collections/generic/list_1.h"]);
        }

        [TestMethod]
        public async Task GenericDelegateArities_GetOwnHeadersAndShareInstantiations()
        {
            const string source = """
                using System;
                namespace Test
                {
                    public class Callbacks
                    {
                        public Action Empty { get; set; }
                        public Action<int> Number { get; set; }
                        public Action<string> Text { get; set; }
                        public Action<int, string> Pair { get; set; }
                        public EventHandler Event { get; set; }
                        public EventHandler<EventArgs> GenericEvent { get; set; }
                    }
                }
                """;
            using var result = await TestCodeGenerator.GenerateAsync([source],
                defaultExportOptions: Qt.ExportAs.SourceCode,
                ct: TestContext.CancellationTokenSource.Token);

            Assert.Contains("struct System::Action :", result.Sink.Files["source/hpp/system/action.h"]);
            var action1 = result.Sink.Files["source/hpp/system/action_1.h"];
            Assert.Contains("struct System::Action_1__Int32 :", action1);
            Assert.Contains("struct System::Action_1__String :", action1);
            Assert.Contains("struct System::Action_2__Int32__String :",
                result.Sink.Files["source/hpp/system/action_2.h"]);
            Assert.Contains("struct System::EventHandler :",
                result.Sink.Files["source/hpp/system/eventhandler.h"]);
            Assert.Contains("struct System::EventHandler_1__EventArgs :",
                result.Sink.Files["source/hpp/system/eventhandler_1.h"]);
        }
    }
}
