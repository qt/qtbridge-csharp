// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Text;
using Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Extension
{
    [TestClass]
    public sealed class Test_SemanticTokenCapabilitiesFilter
    {
        private const string InitializeResponse =
            """{"id":2,"jsonrpc":"2.0","result":{"capabilities":{"semanticTokensProvider":"""
            + """{"full":{"delta":true},"legend":{"tokenModifiers":"""
            + """["declaration","readonly"],"tokenTypes":["type"]}}}}}""";

        private const string LogMessage =
            """{"jsonrpc":"2.0","method":"window/logMessage","params":{"type":4,"message":"x"}}""";

        private const string FullWithDelta = "\"full\":{\"delta\":true}";
        private const string FullOnly = "\"full\":true";
        private const string Modifiers = "\"tokenModifiers\":[\"declaration\",\"readonly\"]";
        private const string NoModifiers = "\"tokenModifiers\":[]";

        private static string Frame(string body) =>
            $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}";

        private static string Filter(SemanticTokenCapabilitiesFilter filter, string text) =>
            Encoding.UTF8.GetString(filter.Filter(Encoding.UTF8.GetBytes(text)));

        [TestMethod]
        public void Removes_Delta_Support_And_Modifiers_And_Updates_Content_Length()
        {
            var filter = new SemanticTokenCapabilitiesFilter();

            var output = Filter(filter, Frame(InitializeResponse));

            var expected = InitializeResponse.Replace(FullWithDelta, FullOnly)
                .Replace(Modifiers, NoModifiers);
            Assert.AreEqual(Frame(expected), output);
            Assert.IsTrue(filter.IsPassThrough);
        }

        [TestMethod]
        public void Forwards_Messages_Before_The_Capabilities_Unchanged()
        {
            var filter = new SemanticTokenCapabilitiesFilter();

            var output = Filter(filter, Frame(LogMessage));

            Assert.AreEqual(Frame(LogMessage), output);
            Assert.IsFalse(filter.IsPassThrough);
        }

        [TestMethod]
        public void Holds_Back_A_Split_Message_Until_It_Is_Complete()
        {
            var filter = new SemanticTokenCapabilitiesFilter();
            var framed = Frame(InitializeResponse);

            var first = Filter(filter, framed.Substring(0, 40));
            var second = Filter(filter, framed.Substring(40));

            Assert.AreEqual("", first);
            Assert.Contains(FullOnly, second);
        }

        [TestMethod]
        public void Forwards_The_Rest_Of_The_Chunk_After_The_Capabilities()
        {
            var filter = new SemanticTokenCapabilitiesFilter();
            var partial = Frame(LogMessage).Substring(0, 25);

            var output = Filter(filter, Frame(InitializeResponse) + partial);

            Assert.IsTrue(output.EndsWith(partial, StringComparison.Ordinal));
            Assert.AreEqual(Frame(LogMessage), Filter(filter, Frame(LogMessage)));
        }
    }
}
