// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    public sealed class LoggingOptionsPage : DialogPage
    {
        [Category("QML Language Server")]
        [DisplayName("Enable QML Language Server verbose log")]
        [Description("Passes --verbose -l <path> to QML Language Server on startup so its "
            + "internal diagnostics are written to qtbridge-qmlls.log in the configured directory.")]
        [DefaultValue(false)]
        public bool QmllsLogEnabled { get; set; }

        [Category("QML Language Server")]
        [DisplayName("QML Language Server log directory")]
        [Description("Directory where qtbridge-qmlls.log is written. "
            + "Has no effect when the QML Language Server log is disabled.")]
        [DefaultValue("")]
        public string QmllsLogDirectory { get; set; } = string.Empty;

        [Category("LSP Traffic")]
        [DisplayName("Enable LSP traffic log")]
        [Description("Writes every LSP message exchanged between Visual Studio and "
            + "QML Language Server to qtbridge-lsp.log in the configured directory.")]
        [DefaultValue(false)]
        public bool LspLogEnabled { get; set; }

        [Category("LSP Traffic")]
        [DisplayName("LSP log directory")]
        [Description("Directory where qtbridge-lsp.log is written. "
            + "Has no effect when the LSP log is disabled.")]
        [DefaultValue("")]
        public string LspLogDirectory { get; set; } = string.Empty;
    }
}
