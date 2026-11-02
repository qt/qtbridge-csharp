// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;

namespace Test_Qt.Bridge.Project.Shared
{
    internal static class KnownIssue
    {
        public const string Category = "KnownIssue";

        // Whether the issue shows in the export mode of a data row. A mode without the issue
        // must keep passing, so a failure there is a regression, not the known issue.
        public const bool Affected = true;
        public const bool Unaffected = false;

        public static void Check(bool isFixed, string issue, string actual, string fix,
            bool affected = Affected)
        {
            if (isFixed)
                return;
            var nl = Environment.NewLine;
            var message = $"Known issue: {issue}{nl}Actual: {actual}{nl}To fix: {fix}";
            if (!affected)
                Assert.Fail($"Regression in an export mode without this known issue.{nl}{message}");
            Assert.Inconclusive(message);
        }
    }
}
