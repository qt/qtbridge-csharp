// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using System.Text;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{
    [VisualStudioContribution]
    internal sealed partial class WhatsNew : Command
    {
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

            var assemblyDir = Path.GetDirectoryName(
                typeof(WhatsNew).Assembly.Location) ?? "";
            var whatsNewPath = Path.Combine(assemblyDir, "Resources", "WhatsNew", "whatsnew.html");

            if (!File.Exists(whatsNewPath)) {
                VsShellUtilities.ShowMessageBox(
                    ServiceProvider.GlobalProvider,
                    $"What's New page not found: {whatsNewPath}",
                    "Qt Bridge for C#",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var theme = VisualStudioVersion.IsDarkTheme() ? "dark" : "light";
            var html = File.ReadAllText(whatsNewPath)
                .Replace("data-theme=\"THEME\"", $"data-theme=\"{theme}\"");

            var tempPath = Path.Combine(Path.GetTempPath(), "qtbridge-whatsnew.html");
            File.WriteAllText(tempPath, html, Encoding.UTF8);

            OpenBrowserWithFile(tempPath);
        }
    }
}
