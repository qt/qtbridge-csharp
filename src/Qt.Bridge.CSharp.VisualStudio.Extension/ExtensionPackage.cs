// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer;

using Task = System.Threading.Tasks.Task;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    [Guid(PackageGuidString)]
    [ProvideOptionPage(typeof(LoggingOptionsPage), "Qt Bridge for C#",
        "Logging", 0, 0, true, SupportsProfiles = true,
        IsInUnifiedSettings = true)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    public sealed class ExtensionPackage : AsyncPackage
    {
        internal const string PackageGuidString = "D4E5F6A7-B8C9-4D2E-8F1A-3B5C7D9E1F2A";

        internal static LoggingOptionsPage? LoggingPage { get; private set; }

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            LoggingPage = (LoggingOptionsPage)GetDialogPage(typeof(LoggingOptionsPage));
        }
    }
}
