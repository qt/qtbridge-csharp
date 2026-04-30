// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.LanguageServer;
using Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    /// <summary>
    /// Registers the <c>qml</c> document type for <c>.qml</c> files so that the
    /// <see cref="QmlLanguageServerProvider"/> can filter on it.
    /// </summary>
    internal static class QmlDocumentTypes
    {
        [VisualStudioContribution]
        internal static DocumentTypeConfiguration Qml => new("qml")
        {
            FileExtensions = [".qml"],
            BaseDocumentType = LanguageServerProvider.LanguageServerBaseDocumentType
        };
    }
}
