// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.Build.Tasks;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Core
{
    [TestClass]
    public sealed class Test_QmlBuildMetadataContract
    {
        [TestMethod]
        public void GetReadyMarkerPath_ReturnsBuildProducerMarker()
        {
            var metadata = CreateMetadata($"build/.qt/{BuildReadyMarker.FileName}");

            Assert.AreEqual($"build/.qt/{BuildReadyMarker.FileName}",
                QmlBuildMetadataContract.GetReadyMarkerPath(metadata));
        }

        [TestMethod]
        public void GetReadyMarkerPath_ReturnsNullForLegacyProducer()
        {
            Assert.IsNull(QmlBuildMetadataContract.GetReadyMarkerPath(CreateMetadata(null)));
            Assert.IsNull(QmlBuildMetadataContract.GetReadyMarkerPath(CreateMetadata("  ")));
        }

        private static QmlMetadata CreateMetadata(string? readyFile)
        {
            return new QmlMetadata(
                version: 1,
                projectFile: "test.csproj",
                configuration: "Debug",
                targetFramework: null,
                qml: new QmlMetadata.QmlSection(
                    sourceDir: "source",
                    projectSourceDir: "project",
                    buildDirs: ["build"],
                    importPaths: [],
                    files: []),
                qmlLanguageServer: new QmlMetadata.QmlLanguageServerSection(
                    disableCMakeCalls: true,
                    readyFile: readyFile));
        }
    }
}
