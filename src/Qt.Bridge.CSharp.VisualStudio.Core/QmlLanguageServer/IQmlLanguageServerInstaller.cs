// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Downloads, verifies, and installs the QML Language Server executable, returning a record of
    /// the active installation.
    /// </summary>
    public interface IQmlLanguageServerInstaller
    {
        /// <summary>
        /// Ensures the latest QML Language Server is installed and returns the installation record.
        /// Downloads and installs only if the current local version is missing or does not match
        /// the latest release.
        /// </summary>
        Task<QmlLanguageServerInstallation> EnsureInstalledAsync(CancellationToken ct);
    }
}
