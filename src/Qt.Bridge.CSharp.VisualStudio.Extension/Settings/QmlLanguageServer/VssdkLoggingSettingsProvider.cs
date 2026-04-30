// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    internal sealed class VssdkLoggingSettingsProvider : ILoggingSettingsProvider
    {
        public async Task<LoggingOptions> GetOptionsAsync(CancellationToken ct)
        {
            if (ExtensionPackage.LoggingPage == null)
                await EnsurePackageLoadedAsync(ct);

            var page = ExtensionPackage.LoggingPage;
            if (page == null)
                return LoggingOptions.Default;

            return LoggingOptions.Create(
                page.QmllsLogEnabled, page.QmllsLogDirectory,
                page.LspLogEnabled, page.LspLogDirectory);
        }

        private static async Task EnsurePackageLoadedAsync(CancellationToken ct)
        {
            try {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                if (Package.GetGlobalService(typeof(SVsShell)) is IVsShell vsShell) {
                    var guid = new Guid(ExtensionPackage.PackageGuidString);
                    vsShell.LoadPackage(ref guid, out _);
                }
            } catch { }
        }
    }
}
