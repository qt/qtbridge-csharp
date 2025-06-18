/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Diagnostics;

namespace Qt.DotNet.Utils.Concurrent
{
    public static class Timestamp
    {
        private static readonly object criticalSection = new();
        private static long lastTimestamp = 0;
        public static long Next()
        {
            lock (criticalSection) {
                long t = Stopwatch.GetTimestamp();
                if (t <= lastTimestamp)
                    t = lastTimestamp + 1;
                return lastTimestamp = t;
            }
        }
    }
}
