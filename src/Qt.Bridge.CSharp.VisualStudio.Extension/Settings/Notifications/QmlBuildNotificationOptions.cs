// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.Notifications
{
    internal sealed record QmlBuildNotificationOptions(
        bool MissingBuildOutputNotificationsEnabled,
        int SuppressedProjectExpirationDays,
        IReadOnlyList<QmlBuildNotificationSuppression> SuppressedProjects);
}
