// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics
{
    internal sealed partial class ToastWindow
    {
        private const double RightMargin = 28;
        private const double BottomMargin = 44;
        private static readonly Duration AnimationDuration = new(TimeSpan.FromMilliseconds(250));
        private static readonly Duration FadeInDuration = new(TimeSpan.FromMilliseconds(200));

        private readonly DispatcherTimer dismissTimer;
        private readonly IExtensionLog extensionLog;
        private bool closing;

        // Stored so they can be unsubscribed when the toast closes.
        private Window? ownerWindow;
        private EventHandler? ownerLocationChangedHandler;
        private SizeChangedEventHandler? ownerSizeChangedHandler;
        private DispatcherTimer? locationChangeDebounce;

        public ToastWindow(
            string title,
            string detail,
            NotificationAction? primaryAction,
            NotificationAction? secondaryAction,
            TimeSpan displayDuration,
            IExtensionLog log)
        {
            extensionLog = log ?? throw new ArgumentNullException(nameof(log));

            InitializeComponent();

            TitleText.Text = title;
            DetailText.Text = detail;
            DetailText.Visibility = string.IsNullOrEmpty(detail)
                ? Visibility.Collapsed
                : Visibility.Visible;

            BuildActionButtons(primaryAction, secondaryAction);

            dismissTimer = new DispatcherTimer { Interval = displayDuration };
            dismissTimer.Tick += (_, _) => BeginCloseAnimation();

            Loaded += OnLoaded;
            Closed += OnClosed;
            MouseEnter += (_, _) => dismissTimer.Stop();
            MouseLeave += (_, _) =>
            {
                if (!closing)
                    dismissTimer.Start();
            };
        }

        public static async Task<bool> TryShowAsync(
            string title,
            string detail,
            NotificationAction? primaryAction,
            NotificationAction? secondaryAction,
            TimeSpan displayDuration,
            IExtensionLog log,
            CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            var owner = GetVisibleVsMainWindow();
            if (owner == null) {
                await WaitForMainWindowVisibleAsync(ct);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                owner = GetVisibleVsMainWindow();
            }

            if (owner == null) {
                log.Warning("Toast notification skipped: Visual Studio main window was not ready.");
                return false;
            }

            var toast = new ToastWindow(title, detail, primaryAction, secondaryAction,
                displayDuration, log);
            toast.AttachToOwner(owner);
            toast.Show();
            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            PositionWindow();
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, FadeInDuration));
            dismissTimer.Start();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            dismissTimer.Stop();

            locationChangeDebounce?.Stop();
            locationChangeDebounce = null;

            if (ownerWindow == null)
                return;

            if (ownerLocationChangedHandler != null)
                ownerWindow.LocationChanged -= ownerLocationChangedHandler;
            if (ownerSizeChangedHandler != null)
                ownerWindow.SizeChanged -= ownerSizeChangedHandler;
            ownerWindow = null;
        }

        private void AttachToOwner(Window owner)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var hwnd = new WindowInteropHelper(owner).Handle;
            if (!NativeMethods.IsWindow(hwnd))
                return;

            new WindowInteropHelper(this).Owner = hwnd;

            ownerWindow = owner;

            locationChangeDebounce = new DispatcherTimer(
                TimeSpan.FromMilliseconds(500),
                DispatcherPriority.Background,
                OnLocationChangeThrottleTick,
                ownerWindow.Dispatcher);
            locationChangeDebounce.Stop();

            ownerLocationChangedHandler = OnOwnerLocationChanged;
            ownerSizeChangedHandler = OnOwnerSizeChanged;
            ownerWindow.LocationChanged += ownerLocationChangedHandler;
            ownerWindow.SizeChanged += ownerSizeChangedHandler;
        }

        private void OnOwnerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            PositionWindow();
        }

        private void OnOwnerLocationChanged(object? sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            locationChangeDebounce?.Start();
            PositionWindow();
        }

        private void OnLocationChangeThrottleTick(object? sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            locationChangeDebounce?.Stop();
            PositionWindow();
        }

        private static Window? GetVisibleVsMainWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow == null || !IsVsMainWindow(mainWindow))
                return null;

            var hwnd = new WindowInteropHelper(mainWindow).Handle;
            if (!NativeMethods.IsWindow(hwnd) || !NativeMethods.IsWindowVisible(hwnd))
                return null;

            return mainWindow;
        }

        private static bool IsVsMainWindow(Window? mainWindow)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return mainWindow is { IsVisible: true }
                && IsDteMainWindow(mainWindow)
                && VisualTreeHelper.GetChildrenCount(mainWindow) >= 1;
        }

        private static bool IsDteMainWindow(Window mainWindow)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try {
                if (Package.GetGlobalService(typeof(EnvDTE.DTE)) is not EnvDTE.DTE dte)
                    return false;
                if (dte.MainWindow?.Visible != true)
                    return false;
                return new WindowInteropHelper(mainWindow).Handle == dte.MainWindow.HWnd;
            } catch (Exception) {
                return false;
            }
        }

        private static async Task WaitForMainWindowVisibleAsync(CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            if (HasReadyMainWindow())
                return;

            if (Package.GetGlobalService(typeof(SVsShell)) is not IVsShell vsShell)
                return;

            if (IsShellReady(vsShell))
                return;

            var wait = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var sink = new MainWindowVisibilityEvents(vsShell, wait);
            uint cookie = 0;

            try {
                ErrorHandler.ThrowOnFailure(vsShell.AdviseShellPropertyChanges(sink, out cookie));
                using (ct.Register(() => wait.TrySetCanceled(ct)))
                    await wait.Task;
            } finally {
                if (cookie != 0)
                    vsShell.UnadviseShellPropertyChanges(cookie);
            }
        }

        private static bool IsShellReady(IVsShell vsShell)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return HasReadyMainWindow() && !IsShellModal(vsShell);
        }

        private static bool HasReadyMainWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetVisibleVsMainWindow() != null;
        }

        private static bool IsShellModal(IVsShell vsShell)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return TryGetShellBoolProperty(vsShell, (int)__VSSPROPID4.VSSPROPID_IsModal);
        }

        private static bool TryGetShellBoolProperty(IVsShell vsShell, int propertyId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ErrorHandler.Failed(vsShell.GetProperty(propertyId, out var value)))
                return false;

            return value switch
            {
                bool boolValue => boolValue,
                int intValue => intValue != 0,
                _ => false
            };
        }

        private void PositionWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var owner = ownerWindow;
            if (owner == null || !IsVsMainWindow(owner))
                return;

            var left = (int)owner.Left;
            var top = (int)owner.Top;

            var ownerWidth = (int)(owner.ActualWidth > 0 ? owner.ActualWidth : owner.Width);
            var ownerHeight = (int)(owner.ActualHeight > 0 ? owner.ActualHeight : owner.Height);

            if (SpansDpiBoundary(left, top, ownerWidth, ownerHeight)) {
                if (IsVisible)
                    Hide();
                return;
            }

            var toastHeight = (int)(ActualHeight > 0 ? ActualHeight : Height);

            Left = left + ownerWidth - (int)Width - (int)RightMargin;
            Top = top + ownerHeight - toastHeight - (int)BottomMargin;

            if (!IsVisible && !closing)
                Show();
        }

        private static bool SpansDpiBoundary(int left, int top, int width, int height)
        {
            var topLeft = new NativePoint(left, top);
            var bottomRight = new NativePoint(left + width, top + height);

            var monitorTopLeft = NativeMethods.MonitorFromPoint(topLeft, 2u);
            var monitorBottomRight = NativeMethods.MonitorFromPoint(bottomRight, 2u);
            if (monitorTopLeft == monitorBottomRight)
                return false;

            NativeMethods.GetDpiForMonitor(monitorTopLeft, 0, out var dpiTl, out _);
            NativeMethods.GetDpiForMonitor(monitorBottomRight, 0, out var dpiBr, out _);
            return dpiTl != dpiBr;
        }

        private void BeginCloseAnimation()
        {
            if (closing)
                return;

            closing = true;
            dismissTimer.Stop();

            var transform = new TranslateTransform();
            RenderTransform = transform;

            var fade = new DoubleAnimation(1, 0, AnimationDuration);
            Storyboard.SetTarget(fade, this);
            Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));

            var slide = new DoubleAnimation(0, -30, AnimationDuration);
            Storyboard.SetTarget(slide, transform);
            Storyboard.SetTargetProperty(slide, new PropertyPath(TranslateTransform.XProperty));

            var sb = new Storyboard();
            sb.Children.Add(fade);
            sb.Children.Add(slide);
            sb.Completed += (_, _) => Close();
            sb.Begin();
        }

        private void BuildActionButtons(NotificationAction? primary, NotificationAction? secondary)
        {
            if (primary == null && secondary == null) {
                ActionsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            var style = (Style)FindResource("ToastActionButtonStyle");
            foreach (var action in new[] { primary, secondary }) {
                if (action is null)
                    continue;
                var button = new Button { Content = action.Text, Style = style, Tag = action };
                button.Click += OnActionButtonClick;
                ActionsPanel.Children.Add(button);
            }
        }

        private void OnActionButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: NotificationAction action })
                return;
            BeginCloseAnimation();
            _ = ExecuteActionAsync(action);
        }

        private async Task ExecuteActionAsync(NotificationAction action)
        {
            try {
                await action.ExecuteAsync(CancellationToken.None);
            } catch (Exception ex) {
                extensionLog.Warning($"Toast action '{action.Text}' failed: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => BeginCloseAnimation();

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint(int x, int y)
        {
            public int X = x;
            public int Y = y;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromPoint(NativePoint pt, uint dwFlags);

            [DllImport("shcore.dll")]
            public static extern int GetDpiForMonitor(IntPtr mon, int type, out uint x, out uint y);
        }

        private sealed class MainWindowVisibilityEvents(
            IVsShell vsShell,
            TaskCompletionSource<object?> wait)
            : IVsShellPropertyEvents
        {
            public int OnShellPropertyChange(int propid, object var)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var isModalOrMainWindowProperty  = propid
                    is (int)__VSSPROPID4.VSSPROPID_IsModal
                    or (int)__VSSPROPID2.VSSPROPID_MainWindowVisibility;

                if (isModalOrMainWindowProperty  && IsShellReady(vsShell))
                    wait.TrySetResult(null);

                return VSConstants.S_OK;
            }
        }
    }
}
