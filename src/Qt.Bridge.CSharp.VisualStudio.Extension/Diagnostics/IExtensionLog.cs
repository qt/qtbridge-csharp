// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics
{
    internal interface IExtensionLog
    {
        void Verbose(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception? exception = null);
    }
}
