// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            AttachToOwner();
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

        private void AttachToOwner()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var hwnd = GetVsMainWindowHandle();
            if (hwnd == IntPtr.Zero)
                return;

            new WindowInteropHelper(this).Owner = hwnd;

            ownerWindow = HwndSource.FromHwnd(hwnd)?.RootVisual as Window;
            if (ownerWindow == null)
                return;

            locationChangeDebounce = new DispatcherTimer(
                TimeSpan.FromMilliseconds(500),
                DispatcherPriority.Background,
                OnLocationChangeThrottleTick,
                ownerWindow.Dispatcher);
            locationChangeDebounce.Stop();

            ownerLocationChangedHandler = OnOwnerLocationChanged;
            ownerSizeChangedHandler = (_, _) => PositionWindow();
            ownerWindow.LocationChanged += ownerLocationChangedHandler;
            ownerWindow.SizeChanged += ownerSizeChangedHandler;
        }

        private void OnOwnerLocationChanged(object? sender, EventArgs e)
        {
            locationChangeDebounce?.Start();
            PositionWindow();
        }

        private void OnLocationChangeThrottleTick(object? sender, EventArgs e)
        {
            locationChangeDebounce?.Stop();
            PositionWindow();
        }

        private static IntPtr GetVsMainWindowHandle()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Package.GetGlobalService(typeof(SVsUIShell)) is not IVsUIShell uiShell)
                return IntPtr.Zero;
            uiShell.GetDialogOwnerHwnd(out var hwnd);
            return hwnd;
        }

        private void PositionWindow()
        {
            if (ownerWindow == null)
                return;

            var left = (int)ownerWindow.Left;
            var top = (int)ownerWindow.Top;

            var ownerWidth = (int)(ownerWindow.ActualWidth > 0
                ? ownerWindow.ActualWidth
                : ownerWindow.Width);
            var ownerHeight = (int)(ownerWindow.ActualHeight > 0
                ? ownerWindow.ActualHeight
                : ownerWindow.Height);

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
            public static extern IntPtr MonitorFromPoint(NativePoint pt, uint dwFlags);

            [DllImport("shcore.dll")]
            public static extern int GetDpiForMonitor(IntPtr mon, int type, out uint x, out uint y);
        }
    }
}
