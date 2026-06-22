// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata
{
    public static class QmlBuildMetadataContract
    {
        public static string? GetReadyMarkerPath(QmlMetadata metadata)
        {
            var readyFile = metadata.QmlLanguageServer.ReadyFile;
            return string.IsNullOrWhiteSpace(readyFile) ? null : readyFile;
        }
    }
}
