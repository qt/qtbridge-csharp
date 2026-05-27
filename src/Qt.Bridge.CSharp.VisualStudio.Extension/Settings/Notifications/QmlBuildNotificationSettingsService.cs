// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.Notifications
{
    internal sealed class QmlBuildNotificationSettingsService :
        IQmlBuildNotificationSettingsService,
        IDisposable
    {
        private const string EnabledMoniker = "missingBuildOutputNotificationsEnabled";
        private const string ExpirationDaysMoniker = "suppressedProjectExpirationDays";
        private const string SuppressedProjectsMoniker = "suppressedProjects";
        private const string DisplayNameProperty = "displayName";
        private const string ProjectFilePathProperty = "projectFilePath";
        private const string SuppressedAtUtcProperty = "suppressedAtUtc";

        private readonly AsyncPackage package;
        private readonly IQmlBuildNotificationSettings settings;
        private readonly SemaphoreSlim cacheLock = new(1, 1);
        private readonly FileSystemWatcher? settingsFileWatcher;
        private QmlBuildNotificationOptions cachedOptions = new(true, 180, []);
        private bool hasPendingChanges;
        private int settingsFileRefreshScheduled;

        public QmlBuildNotificationSettingsService(
            AsyncPackage package,
            IQmlBuildNotificationSettings settings)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            settingsFileWatcher = CreateSettingsFileWatcher();
            if (settingsFileWatcher == null)
                return;

            settingsFileWatcher.Changed += OnSettingsFileChanged;
            settingsFileWatcher.Created += OnSettingsFileChanged;
            settingsFileWatcher.Renamed += OnSettingsFileChanged;
            settingsFileWatcher.EnableRaisingEvents = true;
        }

        public event EventHandler<ExternalSettingsChangedEventArgs>? SettingValuesChanged;

        public event EventHandler<EnumSettingChoicesChangedEventArgs>? EnumSettingChoicesChanged
        {
            add { }
            remove { }
        }

        public event EventHandler<DynamicMessageTextChangedEventArgs>? DynamicMessageTextChanged
        {
            add { }
            remove { }
        }

        public event EventHandler? ErrorConditionResolved
        {
            add { }
            remove { }
        }

        public async Task<ExternalSettingOperationResult> RefreshCacheAsync(CancellationToken ct)
        {
            await cacheLock.WaitAsync(ct);
            try {
                await RefreshCacheAsync(raiseChangeEvents: true, ct);
                hasPendingChanges = false;
                return ExternalSettingOperationResult.Success.Instance;
            } finally {
                cacheLock.Release();
            }
        }

        public async Task<ExternalSettingOperationResult> CommitPendingChangesAsync(
            CancellationToken cancellationToken)
        {
            await cacheLock.WaitAsync(cancellationToken);
            try {
                if (!hasPendingChanges)
                    return ExternalSettingOperationResult.Success.Instance;

                await settings.SetOptionsAsync(cachedOptions, cancellationToken);
                hasPendingChanges = false;
                return ExternalSettingOperationResult.Success.Instance;
            } finally {
                cacheLock.Release();
            }
        }

        public async Task<ExternalSettingOperationResult<T>> GetValueAsync<T>(
            string moniker,
            CancellationToken cancellationToken)
            where T : notnull
        {
            await cacheLock.WaitAsync(cancellationToken);
            try {
                if (!hasPendingChanges)
                    await RefreshCacheAsync(raiseChangeEvents: false, cancellationToken);

                return moniker switch
                {
                    EnabledMoniker => ExternalSettingOperationResult
                        .ConvertSuccessResult<T>(cachedOptions.MissingBuildOutputNotificationsEnabled),
                    ExpirationDaysMoniker => ExternalSettingOperationResult
                        .ConvertSuccessResult<T>(cachedOptions.SuppressedProjectExpirationDays),
                    SuppressedProjectsMoniker => ExternalSettingOperationResult
                        .ConvertSuccessResult<T>(ToTable(cachedOptions.SuppressedProjects)),
                    _ => new ExternalSettingOperationResult<T>.Failure(
                        $"Unknown Qt Bridge notification setting '{moniker}'.",
                        ExternalSettingsErrorScope.SingleSettingOnly,
                        isTransient: false)
                };
            } finally {
                cacheLock.Release();
            }
        }

        public async Task<ExternalSettingOperationResult> SetValueAsync<T>(
            string moniker,
            T value,
            CancellationToken cancellationToken)
            where T : notnull
        {
            await cacheLock.WaitAsync(cancellationToken);
            try {
                if (!hasPendingChanges)
                    await RefreshCacheAsync(raiseChangeEvents: false, cancellationToken);

                switch (moniker) {
                case EnabledMoniker when value is bool enabled:
                    cachedOptions = cachedOptions with
                    {
                        MissingBuildOutputNotificationsEnabled = enabled
                    };
                    hasPendingChanges = true;
                    return ExternalSettingOperationResult.Success.Instance;

                case ExpirationDaysMoniker when value is int expirationDays:
                    cachedOptions = cachedOptions with
                    {
                        SuppressedProjectExpirationDays = expirationDays
                    };
                    hasPendingChanges = true;
                    return ExternalSettingOperationResult.Success.Instance;

                case SuppressedProjectsMoniker:
                    if (TryGetTable(value, out var table)) {
                        cachedOptions = cachedOptions with
                        {
                            SuppressedProjects = FromTable(
                                table,
                                cachedOptions.SuppressedProjects)
                        };
                        hasPendingChanges = true;
                        return ExternalSettingOperationResult.Success.Instance;
                    }
                    break;
                }

                return new ExternalSettingOperationResult.Failure(
                    $"Invalid value for Qt Bridge notification setting '{moniker}'.",
                    ExternalSettingsErrorScope.SingleSettingOnly,
                    isTransient: false);
            } finally {
                cacheLock.Release();
            }
        }

        public Task<string> GetMessageTextAsync(string messageId, CancellationToken ct) =>
            Task.FromResult(string.Empty);

        public Task<ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>>
            GetEnumChoicesAsync(string enumSettingMoniker, CancellationToken cancellationToken) =>
            ExternalSettingOperationResult.SuccessResultTask<IReadOnlyList<EnumChoice>>([]);

        public async Task OpenBackingStoreAsync(CancellationToken cancellationToken)
        {
            var options = await settings.GetOptionsAsync(cancellationToken);
            await settings.SetOptionsAsync(options, cancellationToken);

            await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            VsShellUtilities.OpenDocument(
                package,
                QmlBuildNotificationSettings.DefaultSettingsFilePath);
        }

        public void Dispose()
        {
            if (settingsFileWatcher != null) {
                settingsFileWatcher.Changed -= OnSettingsFileChanged;
                settingsFileWatcher.Created -= OnSettingsFileChanged;
                settingsFileWatcher.Renamed -= OnSettingsFileChanged;
                settingsFileWatcher.Dispose();
            }

            cacheLock.Dispose();
        }

        private static FileSystemWatcher? CreateSettingsFileWatcher()
        {
            var settingsFilePath = QmlBuildNotificationSettings.DefaultSettingsFilePath;
            var directory = Path.GetDirectoryName(settingsFilePath);
            var fileName = Path.GetFileName(settingsFilePath);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                return null;

            Directory.CreateDirectory(directory);
            return new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.CreationTime
                    | NotifyFilters.FileName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size
            };
        }

        private void OnSettingsFileChanged(object sender, FileSystemEventArgs e)
        {
            ScheduleSettingsFileRefresh();
        }

        private void ScheduleSettingsFileRefresh()
        {
            if (Interlocked.Exchange(ref settingsFileRefreshScheduled, 1) == 1)
                return;

            _ = Task.Run(async () =>
            {
                try {
                    await Task.Delay(250).ConfigureAwait(false);
                    await cacheLock.WaitAsync().ConfigureAwait(false);
                    try {
                        if (!hasPendingChanges) {
                            await RefreshCacheAsync(
                                    raiseChangeEvents: true,
                                    CancellationToken.None)
                                .ConfigureAwait(false);
                        }
                    } finally {
                        cacheLock.Release();
                    }
                } catch (Exception) {
                    // The next explicit refresh or value read will retry loading the JSON file.
                } finally {
                    Interlocked.Exchange(ref settingsFileRefreshScheduled, 0);
                }
            });
        }

        private async Task RefreshCacheAsync(bool raiseChangeEvents, CancellationToken ct)
        {
            var options = await settings.GetOptionsAsync(ct);
            var changedMonikers = GetChangedMonikers(cachedOptions, options);
            cachedOptions = options;
            if (raiseChangeEvents && changedMonikers.Count > 0) {
                SettingValuesChanged?.Invoke(this,
                    ExternalSettingsChangedEventArgs.Multiple(changedMonikers));
            }
        }

        private static IReadOnlyList<string> GetChangedMonikers(
            QmlBuildNotificationOptions current,
            QmlBuildNotificationOptions updated)
        {
            var changed = new List<string>();
            if (current.MissingBuildOutputNotificationsEnabled
                != updated.MissingBuildOutputNotificationsEnabled) {
                changed.Add(EnabledMoniker);
            }
            if (current.SuppressedProjectExpirationDays
                != updated.SuppressedProjectExpirationDays) {
                changed.Add(ExpirationDaysMoniker);
            }
            if (!current.SuppressedProjects.SequenceEqual(updated.SuppressedProjects))
                changed.Add(SuppressedProjectsMoniker);
            return changed;
        }

        private static IReadOnlyList<IDictionary<string, object>> ToTable(
            IEnumerable<QmlBuildNotificationSuppression> projects)
        {
            return projects
                .Select(IDictionary<string, object> (project) => new Dictionary<string, object>
                {
                    [DisplayNameProperty] = project.DisplayName,
                    [ProjectFilePathProperty] = project.ProjectFilePath
                })
                .ToList();
        }

        private static bool TryGetTable<T>(
            T value,
            out IReadOnlyList<IReadOnlyDictionary<string, object>> table)
            where T : notnull
        {
            switch (value) {
            case IReadOnlyList<IReadOnlyDictionary<string, object>> readOnlyTable:
                table = readOnlyTable;
                return true;
            case IReadOnlyList<IDictionary<string, object>> dictionaryTable:
                table = dictionaryTable
                    .Select(item => (IReadOnlyDictionary<string, object>)item)
                    .ToList();
                return true;
            default:
                table = [];
                return false;
            }
        }

        private static IReadOnlyList<QmlBuildNotificationSuppression> FromTable(
            IEnumerable<IReadOnlyDictionary<string, object>> table,
            IReadOnlyList<QmlBuildNotificationSuppression> currentProjects)
        {
            return table
                .Select(row => ToSuppression(row, currentProjects))
                .Where(project => !string.IsNullOrWhiteSpace(project.ProjectFilePath))
                .ToList();
        }

        private static QmlBuildNotificationSuppression ToSuppression(
            IReadOnlyDictionary<string, object> row,
            IEnumerable<QmlBuildNotificationSuppression> currentProjects)
        {
            var projectFilePath = GetString(row, ProjectFilePathProperty);
            var displayName = GetString(row, DisplayNameProperty);
            if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(projectFilePath))
                displayName = Path.GetFileNameWithoutExtension(projectFilePath);

            var suppressedAt = DateTimeOffset.TryParse(
                GetString(row, SuppressedAtUtcProperty),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(GetString(row, SuppressedAtUtcProperty))) {
                suppressedAt = currentProjects
                    .FirstOrDefault(project => string.Equals(
                        project.ProjectFilePath,
                        projectFilePath,
                        StringComparison.OrdinalIgnoreCase))
                    ?.SuppressedAtUtc
                    ?? DateTimeOffset.UtcNow;
            }

            return new QmlBuildNotificationSuppression(projectFilePath, displayName, suppressedAt);
        }

        private static string GetString(
            IReadOnlyDictionary<string, object> row,
            string propertyName)
        {
            return row.TryGetValue(propertyName, out var value) && value is string text
                ? text
                : string.Empty;
        }
    }
}
