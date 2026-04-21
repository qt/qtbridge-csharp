// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Thrown when the QML Language Server process fails to start.
    /// </summary>
    public sealed class QmlLanguageServerLaunchException(
        string message,
        string executablePath,
        Exception? innerException = null)
        : Exception(message, innerException)
    {
        public string ExecutablePath { get; } = executablePath;
    }
}
