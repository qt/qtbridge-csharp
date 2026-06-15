// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using System.Text;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Qt.Bridge.CSharp.VisualStudio.Core;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{
    [VisualStudioContribution]
    internal sealed partial class WhatsNew : Command
    {
        private const string WhatsNewLightPath = "whatsnew-light.html";
        private const string WhatsNewDarkPath = "whatsnew-dark.html";
        private const string WhatsNewVersionPath = "whatsnew.version";

        public override CommandConfiguration CommandConfiguration =>
            new("%QtBridge.WhatsNew.DisplayName%")
            {
                Icon = new CommandIconConfiguration(ImageMoniker.KnownValues.Megaphone,
                    IconSettings.IconAndText)
            };

        public override async Task ExecuteCommandAsync(IClientContext ctx, CancellationToken ct) =>
            await OpenWhatsNewPageAsync(ct);

        internal static async Task OpenWhatsNewPageAsync(CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            if (!TryPrepareWhatsNewPages(out var lightPath, out var darkPath, out var errorMsg)) {
                VsShellUtilities.ShowMessageBox(
                    ServiceProvider.GlobalProvider,
                    errorMsg,
                    "Qt Bridge for C#",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            } else {
                OpenBrowserWithFile(GetThemePath(lightPath, darkPath));
            }
        }

        internal static void RefreshOpenWhatsNewPageForThemeChange()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!HasOpenBrowser())
                return;

            if (!TryPrepareWhatsNewPages(out var lightPath, out var darkPath, out _))
                return;

            OpenBrowserWithFile(GetThemePath(lightPath, darkPath));
        }

        private static bool TryPrepareWhatsNewPages(
            out string lightPath,
            out string darkPath,
            out string errorMessage)
        {
            lightPath = "";
            darkPath = "";
            errorMessage = "";

            var assemblyDir = ExtensionAssemblyInfo.GetLocation();
            var sourcePath = Path.Combine(assemblyDir, "Resources", "WhatsNew", "whatsnew.html");

            if (!File.Exists(sourcePath)) {
                errorMessage = $"What's New page not found: {sourcePath}";
                return false;
            }

            try {
                var outputDir = QtBridgeUserDataPaths.VisualStudioWhatsNewDirectory;
                Directory.CreateDirectory(outputDir);

                lightPath = Path.Combine(outputDir, WhatsNewLightPath);
                darkPath = Path.Combine(outputDir, WhatsNewDarkPath);
                var versionPath = Path.Combine(outputDir, WhatsNewVersionPath);
                var currentVersion = ExtensionVersionInfo.GetInstalledVersion();

                if (!NeedsWhatsNewPageRewrite(versionPath, lightPath, darkPath, currentVersion))
                    return true;

                var htmlTemplate = File.ReadAllText(sourcePath);
                File.WriteAllText(
                    lightPath,
                    htmlTemplate.Replace("data-theme=\"THEME\"", "data-theme=\"light\""),
                    Encoding.UTF8);
                File.WriteAllText(
                    darkPath,
                    htmlTemplate.Replace("data-theme=\"THEME\"", "data-theme=\"dark\""),
                    Encoding.UTF8);
                File.WriteAllText(versionPath, currentVersion, Encoding.UTF8);
                return true;
            } catch (Exception ex) {
                errorMessage = $"Could not prepare What's New pages: {ex.Message}";
                return false;
            }
        }

        private static string GetThemePath(string lightPath, string darkPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return VisualStudioVersion.IsDarkTheme() ? darkPath : lightPath;
        }

        private static bool NeedsWhatsNewPageRewrite(
            string versionPath,
            string lightPath,
            string darkPath,
            string currentVersion)
        {
            if (!File.Exists(lightPath) || !File.Exists(darkPath) || !File.Exists(versionPath))
                return true;

            var cachedVersion = File.ReadAllText(versionPath).Trim();
            return !string.Equals(cachedVersion, currentVersion, StringComparison.OrdinalIgnoreCase);
        }
    }
}
