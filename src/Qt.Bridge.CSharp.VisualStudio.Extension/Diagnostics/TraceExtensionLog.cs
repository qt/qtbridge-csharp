// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics
{
    internal sealed class TraceExtensionLog : IExtensionLog
    {
        private readonly TraceSource traceSource;

        public TraceExtensionLog()
            : this(new TraceSource("QtBridge"))
        {
        }

        public TraceExtensionLog(TraceSource source)
        {
            traceSource = source ?? throw new ArgumentNullException(nameof(source));
            traceSource.Switch.Level = SourceLevels.Verbose;
        }

        public void Verbose(string message)
        {
            traceSource.TraceEvent(TraceEventType.Verbose, 0, message);
        }

        public void Info(string message)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, message);
        }

        public void Warning(string message)
        {
            traceSource.TraceEvent(TraceEventType.Warning, 0, message);
        }

        public void Error(string message, Exception? exception = null)
        {
            traceSource.TraceEvent(TraceEventType.Error, 0,
                exception == null
                    ? message
                    : $"{message}{Environment.NewLine}{exception}");
        }
    }
}
