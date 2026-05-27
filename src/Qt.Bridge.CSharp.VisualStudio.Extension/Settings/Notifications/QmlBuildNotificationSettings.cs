// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Qt.Bridge.CSharp.VisualStudio.Core;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.Notifications
{
    internal sealed class QmlBuildNotificationSettings : IQmlBuildNotificationSettings
    {
        private const int CurrentVersion = 1;
        private const int DefaultSuppressedProjectExpirationDays = 180;
        private const int MaxSuppressedProjectCount = 200;
        private const int MaxSuppressedProjectExpirationDays = 365;
        private const string SettingsFileName = "qml-build-notifications.json";

        private readonly SemaphoreSlim settingsLock = new(1, 1);
        private readonly IExtensionLog log;
        private readonly string settingsFilePath;

        public QmlBuildNotificationSettings(IExtensionLog log)
            : this(log, DefaultSettingsFilePath)
        { }

        internal QmlBuildNotificationSettings(IExtensionLog log, string settingsFilePath)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.settingsFilePath = string.IsNullOrWhiteSpace(settingsFilePath)
                ? throw new ArgumentException("Settings file path must not be empty.",
                    nameof(settingsFilePath))
                : settingsFilePath;
        }

        internal static string DefaultSettingsFilePath => Path.Combine(
            QtBridgeUserDataPaths.VisualStudioNotificationsDirectory, SettingsFileName);

        internal static string DefaultSettingsFileDisplayPath => Path.Combine(
            QtBridgeUserDataPaths.VisualStudioNotificationsDirectoryDisplayPath, SettingsFileName);

        public async Task<bool> ShouldShowMissingBuildOutputNotificationAsync(
            string projectFilePath,
            CancellationToken ct)
        {
            var normalizedPath = NormalizeProjectPath(projectFilePath);
            await settingsLock.WaitAsync(ct);
            try {
                var settings = ReadSettings();
                return settings.MissingBuildOutputNotificationsEnabled != false
                    && !settings.SuppressedProjects.Any(project =>
                        IsSameProject(project.ProjectFilePath, normalizedPath));
            } finally {
                settingsLock.Release();
            }
        }

        public async Task<bool> GetMissingBuildOutputNotificationsEnabledAsync(CancellationToken ct)
        {
            await settingsLock.WaitAsync(ct);
            try {
                var settings = ReadSettings();
                return settings.MissingBuildOutputNotificationsEnabled != false;
            } finally {
                settingsLock.Release();
            }
        }

        public async Task<QmlBuildNotificationOptions> GetOptionsAsync(CancellationToken ct)
        {
            await settingsLock.WaitAsync(ct);
            try {
                return ToOptions(ReadSettings());
            } finally {
                settingsLock.Release();
            }
        }

        public async Task SetOptionsAsync(QmlBuildNotificationOptions options, CancellationToken ct)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            await settingsLock.WaitAsync(ct);
            try {
                var settings = new SettingsDto
                {
                    MissingBuildOutputNotificationsEnabled =
                        options.MissingBuildOutputNotificationsEnabled,
                    SuppressedProjectExpirationDays = NormalizeExpirationDays(
                        options.SuppressedProjectExpirationDays),
                    SuppressedProjects = options.SuppressedProjects
                        .Select(project => new SuppressedProjectDto
                        {
                            ProjectFilePath = NormalizeProjectPath(project.ProjectFilePath),
                            DisplayName = project.DisplayName,
                            SuppressedAtUtc = ToRoundTripFormat(project.SuppressedAtUtc)
                        })
                        .ToList()
                };
                WriteSettings(settings);
            } finally {
                settingsLock.Release();
            }
        }

        public async Task SetMissingBuildOutputNotificationsEnabledAsync(
            bool enabled,
            CancellationToken ct)
        {
            await settingsLock.WaitAsync(ct);
            try {
                var settings = ReadSettings();
                settings.MissingBuildOutputNotificationsEnabled = enabled;
                WriteSettings(settings);
            } finally {
                settingsLock.Release();
            }
        }

        public async Task<int> GetSuppressedProjectExpirationDaysAsync(CancellationToken ct)
        {
            await settingsLock.WaitAsync(ct);
            try {
                var settings = ReadSettings();
                return settings.SuppressedProjectExpirationDays
                    ?? DefaultSuppressedProjectExpirationDays;
            } finally {
                settingsLock.Release();
            }
        }

        public async Task SetSuppressedProjectExpirationDaysAsync(
            int expirationDays,
            CancellationToken ct)
        {
            await settingsLock.WaitAsync(ct);
            try {
                var settings = ReadSettings();
                settings.SuppressedProjectExpirationDays = NormalizeExpirationDays(expirationDays);
                WriteSettings(settings);
            } finally {
                settingsLock.Release();
            }
        }

        public async Task SuppressMissingBuildOutputNotificationAsync(
            string projectFilePath,
            CancellationToken ct)
        {
            var normalizedPath = NormalizeProjectPath(projectFilePath);
            await settingsLock.WaitAsync(ct);
            try {
                var settings = ReadSettings();
                if (settings.SuppressedProjects.Any(project =>
                        IsSameProject(project.ProjectFilePath, normalizedPath))) {
                    return;
                }

                settings.SuppressedProjects.Add(new SuppressedProjectDto
                {
                    ProjectFilePath = normalizedPath,
                    DisplayName = Path.GetFileNameWithoutExtension(normalizedPath),
                    SuppressedAtUtc = ToRoundTripFormat(DateTimeOffset.UtcNow)
                });
                settings.SuppressedProjects.Sort((left, right) =>
                    string.Compare(left.ProjectFilePath, right.ProjectFilePath,
                        StringComparison.OrdinalIgnoreCase));
                WriteSettings(settings);
            } finally {
                settingsLock.Release();
            }
        }

        public async Task<IReadOnlyList<QmlBuildNotificationSuppression>>
            GetSuppressedProjectsAsync(CancellationToken ct)
        {
            await settingsLock.WaitAsync(ct);
            try {
                var settings = ReadSettings();
                return settings.SuppressedProjects
                    .Select(project => new QmlBuildNotificationSuppression(
                        project.ProjectFilePath,
                        project.DisplayName,
                        ParseDateTimeOffset(project.SuppressedAtUtc)))
                    .ToList();
            } finally {
                settingsLock.Release();
            }
        }

        public async Task RemoveSuppressedProjectAsync(string projectFilePath, CancellationToken ct)
        {
            var normalizedPath = NormalizeProjectPath(projectFilePath);
            await settingsLock.WaitAsync(ct);
            try {
                var settings = ReadSettings();
                var removed = settings.SuppressedProjects.RemoveAll(project =>
                    IsSameProject(project.ProjectFilePath, normalizedPath));
                if (removed > 0)
                    WriteSettings(settings);
            } finally {
                settingsLock.Release();
            }
        }

        private SettingsDto ReadSettings()
        {
            if (!File.Exists(settingsFilePath))
                return new SettingsDto();

            try {
                using var stream = File.OpenRead(settingsFilePath);
                var serializer = new DataContractJsonSerializer(typeof(SettingsDto));
                if (serializer.ReadObject(stream) is not SettingsDto settings)
                    return new SettingsDto();

                if (settings.Version <= 0)
                    settings.Version = CurrentVersion;
                NormalizeSettings(settings);
                return settings;
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or SerializationException or ArgumentException) {
                log.Warning($"Qt Bridge: failed to read QML build notification settings: "
                    + $"{ex.Message}");
                return new SettingsDto();
            }
        }

        private void WriteSettings(SettingsDto settings)
        {
            NormalizeSettings(settings);
            settings.Version = CurrentVersion;
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);

            var tempFilePath = settingsFilePath + ".tmp";
            try {
                using (var stream = File.Create(tempFilePath)) {
                    var serializer = new DataContractJsonSerializer(typeof(SettingsDto));
                    using var writer = JsonReaderWriterFactory.CreateJsonWriter(
                        stream,
                        Encoding.UTF8,
                        ownsStream: false,
                        indent: true,
                        indentChars: "  ");
                    serializer.WriteObject(writer, settings);
                }

                if (File.Exists(settingsFilePath))
                    File.Replace(tempFilePath, settingsFilePath, null);
                else
                    File.Move(tempFilePath, settingsFilePath);
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or SerializationException) {
                log.Warning($"Qt Bridge: failed to write QML build notification settings: "
                    + $"{ex.Message}");
                try {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                } catch (IOException) {
                } catch (UnauthorizedAccessException) {
                }
            }
        }

        private static void NormalizeSettings(SettingsDto settings)
        {
            settings.SuppressedProjectExpirationDays = NormalizeExpirationDays(
                settings.SuppressedProjectExpirationDays ?? DefaultSuppressedProjectExpirationDays);

            foreach (var project in settings.SuppressedProjects) {
                project.ProjectFilePath = NormalizeProjectPath(project.ProjectFilePath);
                project.DisplayName = string.IsNullOrWhiteSpace(project.DisplayName)
                    ? Path.GetFileNameWithoutExtension(project.ProjectFilePath)
                    : project.DisplayName;

                if (string.IsNullOrWhiteSpace(project.SuppressedAtUtc))
                    project.SuppressedAtUtc = ToRoundTripFormat(DateTimeOffset.UtcNow);
            }

            var expirationDays = settings.SuppressedProjectExpirationDays
                ?? DefaultSuppressedProjectExpirationDays;
            var cutoff = expirationDays == 0
                ? DateTimeOffset.MinValue
                : DateTimeOffset.UtcNow.AddDays(-expirationDays);

            settings.SuppressedProjects = settings.SuppressedProjects
                .Where(project => !string.IsNullOrWhiteSpace(project.ProjectFilePath))
                .Where(project => ParseDateTimeOffset(project.SuppressedAtUtc) >= cutoff)
                .GroupBy(project => NormalizeProjectPath(project.ProjectFilePath),
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(project => ParseDateTimeOffset(project.SuppressedAtUtc))
                    .First())
                .OrderByDescending(project => ParseDateTimeOffset(project.SuppressedAtUtc))
                .Take(MaxSuppressedProjectCount)
                .OrderBy(project => project.ProjectFilePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static QmlBuildNotificationOptions ToOptions(SettingsDto settings)
        {
            return new QmlBuildNotificationOptions(
                settings.MissingBuildOutputNotificationsEnabled != false,
                settings.SuppressedProjectExpirationDays
                    ?? DefaultSuppressedProjectExpirationDays,
                settings.SuppressedProjects
                    .Select(project => new QmlBuildNotificationSuppression(
                        project.ProjectFilePath,
                        project.DisplayName,
                        ParseDateTimeOffset(project.SuppressedAtUtc)))
                    .ToList());
        }

        private static int NormalizeExpirationDays(int expirationDays)
        {
            if (expirationDays < 0)
                return 0;
            if (expirationDays > MaxSuppressedProjectExpirationDays)
                return MaxSuppressedProjectExpirationDays;
            return expirationDays;
        }

        private static string NormalizeProjectPath(string projectFilePath) =>
            string.IsNullOrWhiteSpace(projectFilePath) ? "" : Path.GetFullPath(projectFilePath);

        private static bool IsSameProject(string left, string right)
        {
            return string.Equals(
                NormalizeProjectPath(left),
                NormalizeProjectPath(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private static DateTimeOffset ParseDateTimeOffset(string value)
        {
            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed)
                ? parsed
                : DateTimeOffset.MinValue;
        }

        private static string ToRoundTripFormat(DateTimeOffset value) =>
            value.ToString("O", CultureInfo.InvariantCulture);

        [DataContract]
        private sealed class SettingsDto
        {
            [DataMember(Name = "version", Order = 0)]
            public int Version { get; set; } = CurrentVersion;

            [DataMember(Name = "missingBuildOutputNotificationsEnabled", Order = 1)]
            public bool? MissingBuildOutputNotificationsEnabled { get; set; } = true;

            [DataMember(Name = "suppressedProjectExpirationDays", Order = 2)]
            public int? SuppressedProjectExpirationDays { get; set; } =
                DefaultSuppressedProjectExpirationDays;

            [DataMember(Name = "suppressedProjects", Order = 3)]
            public List<SuppressedProjectDto> SuppressedProjects { get; set; } = [];
        }

        [DataContract]
        private sealed class SuppressedProjectDto
        {
            [DataMember(Name = "projectFilePath")]
            public string ProjectFilePath { get; set; } = "";

            [DataMember(Name = "displayName")]
            public string DisplayName { get; set; } = "";

            [DataMember(Name = "suppressedAtUtc")]
            public string SuppressedAtUtc { get; set; } = "";
        }
    }
}
