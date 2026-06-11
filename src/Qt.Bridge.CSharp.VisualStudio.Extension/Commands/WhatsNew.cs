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
            await OpenWelcomePageAsync(ct);

        internal static async Task OpenWelcomePageAsync(CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            var assemblyDir = Path.GetDirectoryName(
                typeof(WhatsNew).Assembly.Location) ?? "";
            var welcomePath = Path.Combine(assemblyDir, "Resources", "WhatsNew", "welcome.html");

            if (!File.Exists(welcomePath)) {
                VsShellUtilities.ShowMessageBox(
                    ServiceProvider.GlobalProvider,
                    $"Welcome page not found: {welcomePath}",
                    "Qt Bridge for C#",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var theme = VisualStudioVersion.IsDarkTheme() ? "dark" : "light";
            var html = File.ReadAllText(welcomePath)
                .Replace("data-theme=\"THEME\"", $"data-theme=\"{theme}\"");

            var tempPath = Path.Combine(Path.GetTempPath(), "qtbridge-welcome.html");
            File.WriteAllText(tempPath, html, Encoding.UTF8);

            OpenBrowserWithFile(tempPath);
        }
    }
}
