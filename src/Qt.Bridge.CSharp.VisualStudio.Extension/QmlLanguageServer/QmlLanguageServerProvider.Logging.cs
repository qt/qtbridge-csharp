// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections.Concurrent;
using System.IO;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    internal sealed partial class QmlLanguageServerProvider
    {
        private static readonly ConcurrentDictionary<string, bool> QmllsLogPathsReset =
            new(StringComparer.OrdinalIgnoreCase);

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
