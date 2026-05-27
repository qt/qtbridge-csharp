// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Windows;
using Microsoft.VisualStudio.Shell;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.Notifications
{
    public sealed class NotificationOptionsPage : UIElementDialogPage
    {
        private NotificationOptionsControl? control;

        protected override UIElement Child => control
            ??= new NotificationOptionsControl(new QmlBuildNotificationSettings(new TraceExtensionLog()));
    }
}
