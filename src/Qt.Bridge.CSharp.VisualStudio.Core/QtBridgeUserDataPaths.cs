// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using static System.Environment.SpecialFolder;

namespace Qt.Bridge.CSharp.VisualStudio.Core
{
    public static class QtBridgeUserDataPaths
    {
        private const string ProductDirectoryName = "QtBridge";

        public static string RootDirectory =>
            Path.Combine(Environment.GetFolderPath(LocalApplicationData), ProductDirectoryName);
    }
}
