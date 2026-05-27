// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Globalization;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.Notifications;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Extension
{
    [TestClass]
    public sealed class Test_QmlBuildNotificationSettings
    {
        private string? tempDirectory;

        public TestContext TestContext { get; set; } = null!;

        [TestCleanup]
        public void Cleanup()
        {
            if (tempDirectory != null && Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }

        [TestMethod]
        public async Task SuppressProjectBlocksNotification()
        {
            var settings = CreateSettings();
            var projectPath = ProjectPath("App", "App.csproj");

            await settings.SuppressMissingBuildOutputNotificationAsync(
                projectPath, TestContext.CancellationTokenSource.Token);

            Assert.IsFalse(await settings.ShouldShowMissingBuildOutputNotificationAsync(
                projectPath, TestContext.CancellationTokenSource.Token));
            Assert.IsTrue(await settings.ShouldShowMissingBuildOutputNotificationAsync(
                ProjectPath("Other", "Other.csproj"), TestContext.CancellationTokenSource.Token));
        }

        [TestMethod]
        public async Task GlobalDisableBlocksNotification()
        {
            var settings = CreateSettings();

            await settings.SetMissingBuildOutputNotificationsEnabledAsync(
                false, TestContext.CancellationTokenSource.Token);

            Assert.IsFalse(await settings.ShouldShowMissingBuildOutputNotificationAsync(
                ProjectPath("App", "App.csproj"), TestContext.CancellationTokenSource.Token));
        }

        [TestMethod]
        public async Task DuplicateProjectPathsCollapseCaseInsensitively()
        {
            var settings = CreateSettings();
            var projectPath = ProjectPath("App", "App.csproj");
            var duplicatePath = projectPath.ToUpperInvariant();

            await settings.SetOptionsAsync(new QmlBuildNotificationOptions(
                true,
                180,
                [
                    new QmlBuildNotificationSuppression(
                        projectPath,
                        "Older",
                        DateTimeOffset.UtcNow.AddDays(-1)),
                    new QmlBuildNotificationSuppression(
                        duplicatePath,
                        "Newer",
                        DateTimeOffset.UtcNow)
                ]), TestContext.CancellationTokenSource.Token);

            var projects = await settings.GetSuppressedProjectsAsync(TestContext
                .CancellationTokenSource.Token);

            Assert.HasCount(1, projects);
            Assert.AreEqual("Newer", projects[0].DisplayName);
            Assert.IsFalse(await settings.ShouldShowMissingBuildOutputNotificationAsync(
                projectPath, TestContext.CancellationTokenSource.Token));
        }

        [TestMethod]
        public async Task ExpirationRemovesOldSuppressions()
        {
            var settings = CreateSettings();
            var oldProjectPath = ProjectPath("Old", "Old.csproj");
            var freshProjectPath = ProjectPath("Fresh", "Fresh.csproj");

            await settings.SetOptionsAsync(new QmlBuildNotificationOptions(
                true,
                30,
                [
                    new QmlBuildNotificationSuppression(
                        oldProjectPath,
                        "Old",
                        DateTimeOffset.UtcNow.AddDays(-31)),
                    new QmlBuildNotificationSuppression(
                        freshProjectPath,
                        "Fresh",
                        DateTimeOffset.UtcNow.AddDays(-29))
                ]), TestContext.CancellationTokenSource.Token);

            var projects = await settings.GetSuppressedProjectsAsync(TestContext
                .CancellationTokenSource.Token);

            Assert.HasCount(1, projects);
            Assert.AreEqual(freshProjectPath, projects[0].ProjectFilePath);
            Assert.IsTrue(await settings.ShouldShowMissingBuildOutputNotificationAsync(
                oldProjectPath, TestContext.CancellationTokenSource.Token));
            Assert.IsFalse(await settings.ShouldShowMissingBuildOutputNotificationAsync(
                freshProjectPath, TestContext.CancellationTokenSource.Token));
        }

        [TestMethod]
        public async Task MaxSuppressedProjectCountIsEnforced()
        {
            var settings = CreateSettings();
            var projects = Enumerable.Range(0, 201)
                .Select(index => new QmlBuildNotificationSuppression(
                    ProjectPath("Project" + index.ToString("D3", CultureInfo.InvariantCulture),
                        "Project.csproj"),
                    "Project " + index.ToString(CultureInfo.InvariantCulture),
                    DateTimeOffset.UtcNow.AddMinutes(-index)))
                .ToList();

            await settings.SetOptionsAsync(new QmlBuildNotificationOptions(
                true, 180, projects), TestContext.CancellationTokenSource.Token);

            var suppressedProjects =
                await settings.GetSuppressedProjectsAsync(TestContext.CancellationTokenSource.Token);

            Assert.HasCount(200, suppressedProjects);
            Assert.IsFalse(suppressedProjects.Any(project =>
                string.Equals(project.DisplayName, "Project 200", StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task CorruptJsonFallsBackSafely()
        {
            var settingsFilePath = SettingsFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);
            File.WriteAllText(settingsFilePath, "{ this is not valid json");

            var settings = new QmlBuildNotificationSettings(new TestLog(), settingsFilePath);

            Assert.IsTrue(await settings.ShouldShowMissingBuildOutputNotificationAsync(
                ProjectPath("App", "App.csproj"), TestContext.CancellationTokenSource.Token));
            Assert.IsTrue(await settings.GetMissingBuildOutputNotificationsEnabledAsync(
                TestContext.CancellationTokenSource.Token));
            Assert.HasCount(0, await settings.GetSuppressedProjectsAsync(
                TestContext.CancellationTokenSource.Token));
        }

        private QmlBuildNotificationSettings CreateSettings()
        {
            return new QmlBuildNotificationSettings(new TestLog(), SettingsFilePath());
        }

        private string SettingsFilePath()
        {
            tempDirectory ??= Path.Combine(
                Path.GetTempPath(),
                "qtbridge-notification-tests",
                Guid.NewGuid().ToString("N"));
            return Path.Combine(tempDirectory, "qml-build-notifications.json");
        }

        private string ProjectPath(string directoryName, string fileName)
        {
            tempDirectory ??= Path.Combine(
                Path.GetTempPath(),
                "qtbridge-notification-tests",
                Guid.NewGuid().ToString("N"));
            return Path.Combine(tempDirectory, "projects", directoryName, fileName);
        }

        private sealed class TestLog : IExtensionLog
        {
            public void Verbose(string message)
            { }

            public void Info(string message)
            { }

            public void Warning(string message)
            { }

            public void Error(string message, Exception? exception = null)
            { }
        }
    }
}
