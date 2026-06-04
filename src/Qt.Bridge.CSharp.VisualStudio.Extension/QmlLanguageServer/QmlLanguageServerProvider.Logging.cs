// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Collections.Concurrent;
using System.IO;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    internal sealed partial class QmlLanguageServerProvider
    {
        private static readonly ConcurrentDictionary<string, bool> QmllsLogPathsReset =
            new(StringComparer.OrdinalIgnoreCase);

        private async Task<LoggingOptions> ReadLoggingConfigAsync(CancellationToken ct)
        {
            try {
                return await loggingSettingsProvider.GetOptionsAsync(ct);
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                log.Warning($"QML Language Server: failed to read logging settings "
                    + $"({ex.Message}); logging disabled for this session.");
                return LoggingOptions.Default;
            }
        }

        private static void ResetQmlLanguageServerLogOnce(string path)
        {
            if (!QmllsLogPathsReset.TryAdd(path, true))
                return;
            try {
                File.WriteAllText(path, "");
            } catch (Exception) {}
        }
    }
}
