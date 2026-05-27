// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.Notifications
{
    internal interface IQmlBuildNotificationSettings
    {
        Task<bool> ShouldShowMissingBuildOutputNotificationAsync(
            string projectFilePath,
            CancellationToken ct);

        Task<QmlBuildNotificationOptions> GetOptionsAsync(CancellationToken ct);

        Task SetOptionsAsync(QmlBuildNotificationOptions options, CancellationToken ct);

        Task<bool> GetMissingBuildOutputNotificationsEnabledAsync(CancellationToken ct);

        Task SetMissingBuildOutputNotificationsEnabledAsync(bool enabled, CancellationToken ct);

        Task<int> GetSuppressedProjectExpirationDaysAsync(CancellationToken ct);

        Task SetSuppressedProjectExpirationDaysAsync(int expirationDays, CancellationToken ct);

        Task SuppressMissingBuildOutputNotificationAsync(
            string projectFilePath,
            CancellationToken ct);

        Task<IReadOnlyList<QmlBuildNotificationSuppression>> GetSuppressedProjectsAsync(
            CancellationToken ct);

        Task RemoveSuppressedProjectAsync(string projectFilePath, CancellationToken ct);
    }
}
