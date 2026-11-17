// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Linq;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.KnownIssues
{
    internal static class KnownIssueRunValidation
    {
        internal static void Validate(int exitCode, string output, string testCase,
            string expectedAbort = null)
        {
            var message = $"Exit code {exitCode}: {ExitCodeHelper.ToString(exitCode)}\n{output}";
            Assert.Contains($"PASS   : {testCase}::initTestCase()", output, message);

            if (expectedAbort != null && output.Contains(expectedAbort)) {
                // Unix SIGABRT, Windows CLR exception, fail-fast, or CRT abort.
                Assert.IsTrue(exitCode is 134 or unchecked((int)0xE0434352)
                    or unchecked((int)0xC0000409) or 3, message);
                Assert.DoesNotContain($"PASS   : {testCase}::cleanupTestCase()", output, message);
                return;
            }

            Assert.Contains("Totals: ", output, message);
            Assert.Contains($"PASS   : {testCase}::cleanupTestCase()", output, message);
            var failures = output.Split('\n').Count(line => line.StartsWith("FAIL!  : "));
            Assert.AreEqual(System.Math.Min(failures, 127), exitCode, message);
        }
    }
}
