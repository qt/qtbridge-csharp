// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using static System.Environment.SpecialFolder;

namespace Qt.Bridge.CSharp.VisualStudio.Core
{
    /// <summary>
    /// The per-user data of the Qt Bridge extension, under <c>%LOCALAPPDATA%\Qt\Bridge</c>.
    /// </summary>
    public static class QtBridgeUserDataPaths
    {
        private const string QtDirectoryName = "Qt";
        private const string ProductDirectoryName = "Bridge";
        private const string VisualStudioDirectoryName = "VisualStudio";
        private const string NotificationsDirectoryName = "Notifications";
        private const string WhatsNewDirectoryName = "WhatsNew";

        private const string LocalAppDataEnvironmentVariable = "%LocalAppData%";

        public static string RootDirectory =>
            Path.Combine(Environment.GetFolderPath(LocalApplicationData), QtDirectoryName,
                ProductDirectoryName);

        public static string RootDirectoryDisplayPath =>
            Path.Combine(LocalAppDataEnvironmentVariable, QtDirectoryName, ProductDirectoryName);

        public static string VisualStudioDirectory =>
            Path.Combine(RootDirectory, VisualStudioDirectoryName);

        public static string VisualStudioDirectoryDisplayPath =>
            Path.Combine(RootDirectoryDisplayPath, VisualStudioDirectoryName);

        public static string VisualStudioNotificationsDirectory =>
            Path.Combine(VisualStudioDirectory, NotificationsDirectoryName);

        public static string VisualStudioNotificationsDirectoryDisplayPath =>
            Path.Combine(VisualStudioDirectoryDisplayPath, NotificationsDirectoryName);

        public static string VisualStudioWhatsNewDirectory =>
            Path.Combine(VisualStudioDirectory, WhatsNewDirectoryName);

        public static string VisualStudioWhatsNewDirectoryDisplayPath =>
            Path.Combine(VisualStudioDirectoryDisplayPath, WhatsNewDirectoryName);
    }
}
