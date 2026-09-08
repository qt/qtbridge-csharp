// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using System.Text;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public class Test_NativeHostPlaceholder : ManagedTestBase
    {
        private const string UnpatchedAppHostMarker =
            "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2";

        // The generated QtQuickTestSetup.cpp and main.cpp both include QT_DOTNET_HOST.
        // Prove that a multi-translation-unit host does not retain an unpatched marker copy.

        [TestMethod]
        public async Task NativeHostPlaceholderIsPatched()
        {
            using var temp = new TempProject();
            var options = CreateQtQuickTestOptions(Path.Combine("QtQuickTest", "main.cpp"));
            await InitializeAndBuildAsync(temp, options, project => {
                project.CopyFile("Program.cs", Path.Combine("QtQuickTest", "Program.cs"));
                project.CopyFile("tst_qtquicktest.qml", Path
                    .Combine("QtQuickTest", "tst_qtquicktest.qml"));
            });

            var executable = Encoding.ASCII.GetString(File.ReadAllBytes(temp.ExePath));
            Assert.DoesNotContain(UnpatchedAppHostMarker, executable);
        }
    }
}
