/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator.Support
{
    internal enum DiffFormat
    {
        Unified /* Context, SideBySide */
    }

    internal static class DiffAssert
    {
        /// <summary>Optional sanitizer (e.g., strip timestamps/paths).</summary>
        internal static Func<string, string> Sanitize { get; set; } = s => s;

        /// <summary>
        /// Compare two strings in memory.
        /// </summary>
        internal static void ContentEquals(string actual, string expected, int contextLines = 3,
            DiffFormat format = DiffFormat.Unified)
        {
            var actualContent = Sanitize(NormalizeNewlines(actual));
            var expectedContent = Sanitize(NormalizeNewlines(expected));
            if (!string.Equals(expectedContent, actualContent, StringComparison.Ordinal))
                FailWithDiff(expectedContent, actualContent, contextLines, format);
        }

        /// <summary>
        /// Compare a string in memory against a file on disk.
        /// </summary>
        internal static void ContentEquals(string actual, FileInfo expected, int contextLines = 3,
            DiffFormat format = DiffFormat.Unified)
        {
            if (expected is not { Exists: true })
                Assert.Fail($"Expected file not found: {expected?.FullName ?? "<null>"}");

            var actualContent = Sanitize(NormalizeNewlines(actual));
            var expectedContent = Sanitize(NormalizeNewlines(File.ReadAllText(expected.FullName,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))));

            if (!string.Equals(expectedContent, actualContent, StringComparison.Ordinal))
                FailWithDiff(expectedContent, actualContent, contextLines, format);
        }

        private static string NormalizeNewlines(string text)
        {
            return string.IsNullOrEmpty(text) ? "" : text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static void FailWithDiff(string expected, string actual, int contextLines,
            DiffFormat format)
        {
            var diff = format switch
            {
                DiffFormat.Unified => BuildUnifiedDiff(expected, actual, contextLines),
                _ => throw new NotSupportedException($"Diff format '{format}' not implemented.")
            };
            Assert.Fail("Content mismatch:\n\n" + diff);
        }

        private static string BuildUnifiedDiff(string expected, string actual, int contextLines)
        {
            var differ = new Differ();
            var diffModel = new SideBySideDiffBuilder(differ).BuildDiffModel(expected, actual);

            var expectedLines = diffModel.OldText.Lines;
            var actualLines = diffModel.NewText.Lines;
            var maxLineCount = Math.Max(expectedLines.Count, actualLines.Count);

            var changedLineIndices = Enumerable.Range(0, maxLineCount)
                .Where(i =>
                {
                    var expectedLineType = i < expectedLines.Count
                        ? expectedLines[i].Type : ChangeType.Imaginary;
                    var actualLineType = i < actualLines.Count
                        ? actualLines[i].Type : ChangeType.Imaginary;
                    return expectedLineType != ChangeType.Unchanged
                        || actualLineType != ChangeType.Unchanged;
                })
                .ToArray();

            var diffBuilder = new StringBuilder();
            diffBuilder.AppendLine("--- a/expected");
            diffBuilder.AppendLine("+++ b/actual");

            if (changedLineIndices.Length == 0)
                return diffBuilder.ToString();

            for (var changedIndex = 0; changedIndex < changedLineIndices.Length;) {
                var hunkStart = Math.Max(changedLineIndices[changedIndex] - contextLines, 0);
                var hunkEnd = Math.Min(changedLineIndices[changedIndex] + contextLines,
                    maxLineCount - 1);

                // Merge adjacent hunks within context
                var nextChangedIndex = changedIndex + 1;
                while (nextChangedIndex < changedLineIndices.Length &&
                       changedLineIndices[nextChangedIndex] <= hunkEnd + contextLines) {
                    hunkEnd = Math.Min(changedLineIndices[nextChangedIndex] + contextLines,
                        maxLineCount - 1);
                    nextChangedIndex++;
                }

                var oldLineStart = FirstRealLineNumber(expectedLines, hunkStart);
                var newLineStart = FirstRealLineNumber(actualLines, hunkStart);
                var oldLineCount = CountRealLines(expectedLines, hunkStart, hunkEnd);
                var newLineCount = CountRealLines(actualLines, hunkStart, hunkEnd);

                diffBuilder.AppendLine($"@@ -{oldLineStart},{oldLineCount} +{newLineStart},"
                    + $"{newLineCount} @@");

                for (var lineIndex = hunkStart; lineIndex <= hunkEnd; lineIndex++) {
                    var expectedLine = lineIndex < expectedLines.Count
                        ? expectedLines[lineIndex]
                        : new DiffPiece("", ChangeType.Imaginary, lineIndex + 1);
                    var actualLine = lineIndex < actualLines.Count
                        ? actualLines[lineIndex]
                        : new DiffPiece("", ChangeType.Imaginary, lineIndex + 1);

                    if (expectedLine.Type == ChangeType.Unchanged
                        && actualLine.Type == ChangeType.Unchanged) {
                        diffBuilder.Append(' ').AppendLine(expectedLine.Text ?? "");
                        continue;
                    }

                    if (expectedLine.Type != ChangeType.Unchanged
                        && expectedLine.Type != ChangeType.Imaginary) {
                        diffBuilder.Append('-').AppendLine(expectedLine.Text ?? "");
                    }

                    if (actualLine.Type != ChangeType.Unchanged
                        && actualLine.Type != ChangeType.Imaginary) {
                        diffBuilder.Append('+').AppendLine(actualLine.Text ?? "");
                    }
                }

                changedIndex = nextChangedIndex;
            }

            return diffBuilder.ToString();
        }

        private static int FirstRealLineNumber(IReadOnlyList<DiffPiece> lines, int index)
        {
            // Search forward from the given index
            for (var i = index; i < lines.Count; ++i) {
                if (lines[i].Position.HasValue)
                    return lines[i].Position.Value;
            }

            // Search backward from the given index
            for (var i = Math.Min(index, lines.Count - 1); i >= 0; --i) {
                if (lines[i].Position.HasValue)
                    return lines[i].Position.Value + 1;
            }
            return 1; // Default if no real line is found
        }

        private static int CountRealLines(IReadOnlyList<DiffPiece> lines, int start, int end)
        {
            return Enumerable.Range(start, end - start + 1)
                .Count(i => i < lines.Count && lines[i].Position.HasValue);
        }
    }
}
