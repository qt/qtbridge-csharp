// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.Notifications
{
    internal sealed partial class NotificationOptionsControl
    {
        private const double ProjectColumnMinWidth = 140;
        private const double PathColumnMinWidth = 180;
        private const double ListViewChromeWidth = 36;

        private readonly IQmlBuildNotificationSettings settings;
        private readonly ObservableCollection<ProjectItem> suppressedProjects = [];
        private bool loading;

        public NotificationOptionsControl(IQmlBuildNotificationSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            InitializeComponent();
            SettingsFilePathText.Text = "The build notification settings are stored in "
                + QmlBuildNotificationSettings.DefaultSettingsFileDisplayPath + ".";
            SuppressedProjectsList.ItemsSource = suppressedProjects;
            Loaded += (_, _) => _ = RefreshAsync();
            SizeChanged += (_, _) => UpdateListColumnWidths();
        }

        private async Task RefreshAsync()
        {
            loading = true;
            try {
                ShowNotificationsCheckBox.IsChecked = await settings
                    .GetMissingBuildOutputNotificationsEnabledAsync(CancellationToken.None);
                ExpirationDaysTextBox.Text = (await settings
                    .GetSuppressedProjectExpirationDaysAsync(CancellationToken.None))
                    .ToString(CultureInfo.InvariantCulture);

                suppressedProjects.Clear();
                var projects = await settings.GetSuppressedProjectsAsync(CancellationToken.None);
                foreach (var project in projects) {
                    suppressedProjects.Add(new ProjectItem(
                        project.ProjectFilePath,
                        project.DisplayName));
                }
                StatusText.Text = suppressedProjects.Count == 0
                    ? "No suppressed projects."
                    : $"{suppressedProjects.Count} suppressed project(s).";
            } catch (Exception ex) {
                StatusText.Text = $"Could not load notification settings: {ex.Message}";
            } finally {
                loading = false;
                UpdateListColumnWidths();
                UpdateButtonState();
            }
        }

        private Task SetNotificationsEnabledAsync(bool enabled)
        {
            if (loading)
                return Task.CompletedTask;

            UpdateButtonState();
            return settings.SetMissingBuildOutputNotificationsEnabledAsync(
                enabled, CancellationToken.None);
        }

        private Task SetSuppressedProjectExpirationDaysAsync()
        {
            if (loading)
                return Task.CompletedTask;

            if (!int.TryParse(ExpirationDaysTextBox.Text, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var expirationDays)) {
                StatusText.Text = "Expiration days must be a number from 0 to 365.";
                return RefreshAsync();
            }

            if (expirationDays is < 0 or > 365) {
                StatusText.Text = "Expiration days must be between 0 and 365.";
                return RefreshAsync();
            }

            return settings.SetSuppressedProjectExpirationDaysAsync(
                expirationDays, CancellationToken.None);
        }

        private Task RemoveSelectedAsync()
        {
            if (SuppressedProjectsList.SelectedItem is not ProjectItem selected)
                return Task.CompletedTask;

            return RemoveProjectAsync(selected.ProjectFilePath);
        }

        private async Task RemoveAllAsync()
        {
            foreach (var project in suppressedProjects.ToList())
                await RemoveProjectAsync(project.ProjectFilePath);
        }

        private async Task RemoveProjectAsync(string projectFilePath)
        {
            try {
                await settings.RemoveSuppressedProjectAsync(
                    projectFilePath, CancellationToken.None);
                await RefreshAsync();
            } catch (Exception ex) {
                StatusText.Text = $"Could not remove suppressed project: {ex.Message}";
            }
        }

        private void RunUiTask(Func<Task> action)
        {
            _ = RunUiTaskAsync(action);
        }

        private async Task RunUiTaskAsync(Func<Task> action)
        {
            try {
                await action();
            } catch (Exception ex) {
                StatusText.Text = $"Could not update notification settings: {ex.Message}";
            }
        }

        private void UpdateButtonState()
        {
            var notificationsEnabled = ShowNotificationsCheckBox.IsChecked == true;
            ExpirationDaysTextBox.IsEnabled = notificationsEnabled;
            SuppressedProjectsList.IsEnabled = notificationsEnabled;
            RemoveButton.IsEnabled = notificationsEnabled
                && SuppressedProjectsList.SelectedItem != null;
            RemoveAllButton.IsEnabled = notificationsEnabled && suppressedProjects.Count > 0;
        }

        private void UpdateListColumnWidths()
        {
            var availableWidth = SuppressedProjectsList.ActualWidth - ListViewChromeWidth;
            if (availableWidth <= 0)
                return;

            var projectWidth = Math.Max(
                ProjectColumnMinWidth,
                Math.Min(180, availableWidth * 0.28));
            var pathWidth = Math.Max(
                PathColumnMinWidth,
                availableWidth - projectWidth);

            if (projectWidth + pathWidth > availableWidth) {
                pathWidth = Math.Max(PathColumnMinWidth, availableWidth - projectWidth);
                projectWidth = Math.Max(ProjectColumnMinWidth, availableWidth - pathWidth);
            }

            ProjectColumn.Width = projectWidth;
            PathColumn.Width = pathWidth;
        }

        private void ShowNotificationsCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            RunUiTask(() => SetNotificationsEnabledAsync(true));
        }

        private void ShowNotificationsCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            RunUiTask(() => SetNotificationsEnabledAsync(false));
        }

        private void ExpirationDaysTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            RunUiTask(SetSuppressedProjectExpirationDaysAsync);
        }

        private void SuppressedProjectsList_OnSelectionChanged(object s, SelectionChangedEventArgs e)
        {
            UpdateButtonState();
        }

        private void SuppressedProjectsList_OnSizeChanged(object s, SizeChangedEventArgs e)
        {
            UpdateListColumnWidths();
        }

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            RunUiTask(RemoveSelectedAsync);
        }

        private void RemoveAllButton_OnClick(object sender, RoutedEventArgs e)
        {
            RunUiTask(RemoveAllAsync);
        }

        private sealed record ProjectItem(string ProjectFilePath, string DisplayName);
    }
}
