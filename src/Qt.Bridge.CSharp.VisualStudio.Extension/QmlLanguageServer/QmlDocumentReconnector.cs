// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    /// <summary>
    /// Reconnects the open QML documents to the language server that just started.
    /// </summary>
    /// <remarks>
    /// The semantic token tagger of Visual Studio binds a document to the first language
    /// server it finds and never to a later one, only disconnecting the document from its
    /// views resets it. A content type change does that without closing the document: it
    /// disconnects and connects the views again.
    /// </remarks>
    internal static class QmlDocumentReconnector
    {
        public static async Task ReconnectAsync(IExtensionLog log, CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
            if (Package.GetGlobalService(typeof(SComponentModel)) is not IComponentModel model)
                return;

            var obj = Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            if (obj is not IVsRunningDocumentTable table)
                return;

            var adapters = model.GetService<IVsEditorAdaptersFactoryService>();
            var contentTypes = model.GetService<IContentTypeRegistryService>();
            if (contentTypes.GetContentType("text") is not {} text)
                return;
            if (table.GetRunningDocumentsEnum(out var documents) != 0)
                return;

            var cookies = new uint[1];
            var count = 0;
            while (documents.Next(1, cookies, out var fetched) == 0 && fetched == 1) {
                table.GetDocumentInfo(cookies[0], out _, out _, out _, out var path, out _,
                    out _, out var data);
                if (data == IntPtr.Zero)
                    continue;
                try {
                    if (Marshal.GetObjectForIUnknown(data) is not IVsTextBuffer vsBuffer)
                        continue;
                    var buffer = adapters.GetDocumentBuffer(vsBuffer);
                    if (buffer == null || !buffer.ContentType.IsOfType(QmlDocumentTypes.QmlName))
                        continue;
                    var qml = buffer.ContentType;
                    buffer.ChangeContentType(text, null);
                    buffer.ChangeContentType(qml, null);
                    ++count;
                    log.Verbose($"QML Language Server: reconnected {path}.");
                } finally {
                    Marshal.Release(data);
                }
            }
            log.Info($"QML Language Server: reconnected {count} open QML documents.");
        }
    }
}
