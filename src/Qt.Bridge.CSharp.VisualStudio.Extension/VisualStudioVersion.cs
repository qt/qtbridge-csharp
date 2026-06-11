// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    internal static class VisualStudioVersion
    {
        private const string QtBridgeSettingsMoniker = "qtBridge";

        public static bool SupportsVisualStudioExtensibility()
        {
            try {
                if (Process.GetCurrentProcess().MainModule is {} module)
                    return FileVersionInfo.GetVersionInfo(module.FileName).FileMajorPart >= 18;
            } catch (Exception) {}

            return false;
        }

        public static bool IsDarkTheme()
        {
            return VSColorTheme
                .GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey)
                .GetBrightness() < 0.5f;
        }

        public static void OpenSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (SupportsVisualStudioExtensibility() && TryOpenUnifiedSettings())
                return;

            if (ExtensionPackage.Instance == null) {
                if (Package.GetGlobalService(typeof(EnvDTE.DTE)) is EnvDTE.DTE dte)
                    dte.ExecuteCommand("Tools.Options");
            } else {
                ExtensionPackage.Instance.ShowOptionPage(typeof(LoggingOptionsPage));
            }
        }

        public static async Task OpenSettingsAsync(CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
            OpenSettings();
        }

        private static bool TryOpenUnifiedSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Package.GetGlobalService(typeof(SVsUIShell)) is not IVsUIShell uiShell)
                return false;

            var commandSet = VSConstants.GUID_VSStandardCommandSet97;
            object parameter = QtBridgeSettingsMoniker;
            return ErrorHandler.Succeeded(uiShell.PostExecCommand(
                ref commandSet,
                VSConstants.cmdidToolsOptions,
                0,
                ref parameter));
        }
    }
}
