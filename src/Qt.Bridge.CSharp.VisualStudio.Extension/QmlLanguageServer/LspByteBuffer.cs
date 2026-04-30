// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer.Contracts;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    /// <summary>
    /// Accumulates raw bytes from an LSP stream and extracts complete framed messages
    /// (Content-Length header + body) one at a time.
    /// </summary>
    internal sealed class LspByteBuffer
    {
        private readonly List<byte> data = [];

        public void Append(ReadOnlySpan<byte> bytes)
        {
            data.AddRange(bytes.ToArray());
        }

        public bool TryExtractMessage(out byte[] message)
        {
            message = [];
            var headerEnd = FindHeaderEnd();
            if (headerEnd < 0)
                return false;

            var headerText = Encoding.ASCII.GetString([..data.GetRange(0, headerEnd)]);
            var contentLength = ParseContentLength(headerText);
            if (contentLength < 0)
                return false;

            var totalLength = headerEnd + 4 + contentLength;
            if (data.Count < totalLength)
                return false;

            message = [..data.GetRange(0, totalLength)];
            data.RemoveRange(0, totalLength);
            return true;
        }

        /// <summary>
        /// Parses the JSON body of a framed LSP message and returns the <c>method</c> field,
        /// or <see langword="null"/> if the message has no method (e.g. a response) or if the
        /// body cannot be parsed.
        /// </summary>
        public static string? TryExtractMethod(byte[] message)
        {
            var body = TryExtractBody(message);
            if (body == null)
                return null;

            try {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(body));
                var serializer = new DataContractJsonSerializer(typeof(LspMethodDto));
                return (serializer.ReadObject(ms) as LspMethodDto)?.Method;
            } catch (Exception) {
                return null;
            }
        }

        public static string? TryExtractBody(byte[] message)
        {
            var text = Encoding.UTF8.GetString(message);
            var sep = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            return sep < 0 ? null : text.Substring(sep + 4);
        }

        private int FindHeaderEnd()
        {
            for (var i = 0; i <= data.Count - 4; i++) {
                if (data[i] == '\r' && data[i + 1] == '\n'
                    && data[i + 2] == '\r' && data[i + 3] == '\n')
                    return i;
            }
            return -1;
        }

        private static int ParseContentLength(string headerText)
        {
            foreach (var line in headerText.Split('\n')) {
                var trimmed = line.Trim('\r', ' ');
                if (!trimmed.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    continue;
                var value = trimmed.Substring("Content-Length:".Length).Trim();
                if (int.TryParse(value, out var length))
                    return length;
            }
            return -1;
        }
    }
}
