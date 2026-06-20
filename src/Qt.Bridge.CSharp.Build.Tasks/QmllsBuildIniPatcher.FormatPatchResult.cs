// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal static partial class QmllsBuildIniPatcher
    {
        private readonly struct FormatPatchResult(
            bool recognized,
            bool isReady,
            bool changed,
            IEnumerable<string> lines)
        {
            public bool Recognized { get; } = recognized;

            public bool IsReady { get; } = isReady;

            public bool Changed { get; } = changed;

            public IEnumerable<string> Lines { get; } = lines;

            public static FormatPatchResult NotRecognized(IEnumerable<string> lines) =>
                new(false, false, false, lines);

            public static FormatPatchResult NotReady(IEnumerable<string> lines) =>
                new(true, false, false, lines);

            public static FormatPatchResult Ready(
                IEnumerable<string> original,
                IReadOnlyList<string> updated)
            {
                return new FormatPatchResult(true, true, !original.SequenceEqual(updated), updated);
            }
        }
    }
}
