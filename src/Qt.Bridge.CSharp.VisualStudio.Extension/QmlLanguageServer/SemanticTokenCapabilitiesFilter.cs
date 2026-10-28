// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Text;
using System.Text.RegularExpressions;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    /// <summary>
    /// Removes the semantic token delta support and the token modifiers from the server
    /// capabilities that qmlls sends to VS, so that VS requests the full tokens on each change.
    /// With deltas, VS loses the QML highlighting while typing until the document is reopened.
    /// </summary>
    internal sealed class SemanticTokenCapabilitiesFilter
    {
        private static readonly Regex FullWithDelta = new(
            @"""full""\s*:\s*\{\s*""delta""\s*:\s*true\s*\}", RegexOptions.CultureInvariant);
        private static readonly Regex TokenModifiers = new(
            @"""tokenModifiers""\s*:\s*\[[^\]]*\]", RegexOptions.CultureInvariant);

        private LspByteBuffer? pending = new();

        /// <summary>
        /// True once the capabilities have passed; the bytes then pass unchanged.
        /// </summary>
        public bool IsPassThrough => pending == null;

        /// <summary>
        /// Returns the bytes to forward to VS. Holds back an incomplete message until the
        /// capabilities have passed.
        /// </summary>
        public byte[] Filter(ReadOnlySpan<byte> bytes)
        {
            if (pending == null)
                return [.. bytes];

            pending.Append(bytes);
            var output = new List<byte>();
            while (pending.TryExtractMessage(out var message)) {
                var body = LspByteBuffer.TryExtractBody(message);
                if (body == null || !body.Contains("\"semanticTokensProvider\"")) {
                    output.AddRange(message);
                    continue;
                }

                body = FullWithDelta.Replace(body, "\"full\":true", 1);
                body = TokenModifiers.Replace(body, "\"tokenModifiers\":[]", 1);
                output.AddRange(Encoding.UTF8.GetBytes(QmlLanguageServerTransportPipe
                    .FrameLspMessage(body)));
                output.AddRange(pending.TakeRemaining());
                pending = null;
                break;
            }
            return [..output];
        }
    }
}
