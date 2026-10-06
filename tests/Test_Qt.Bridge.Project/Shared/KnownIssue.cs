// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;

namespace Test_Qt.Bridge.Project.Shared
{
    internal static class KnownIssue
    {
        public const string Category = "KnownIssue";

        public static void Check(bool isFixed, string issue, string actual, string fix)
        {
            if (isFixed)
                return;
            var nl = Environment.NewLine;
            Assert.Inconclusive($"Known issue: {issue}{nl}Actual: {actual}{nl}To fix: {fix}");
        }
    }
}
