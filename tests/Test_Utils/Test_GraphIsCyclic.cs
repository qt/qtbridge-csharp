/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Qt.Bridge.Utils.Collections;

namespace Test_Utils
{
    [TestClass]
    public class Test_GraphIsCyclic
    {
        [TestMethod]
        public void CyclicGraph_ShouldReturnTrue()
        {
            Dictionary<string, List<string>> graph = new()
            {
                { "A", [ "B" ] },
                { "B", [ "C" ] },
                { "C", [ "A" ] }
            };
            Assert.IsTrue(graph.IsCyclic());
        }

        [TestMethod]
        public void AcyclicGraph_ShouldReturnFalse()
        {
            Dictionary<string, List<string>> graph = new()
            {
                { "A", [ "B", "C" ] },
                { "B", [ "C" ] },
                { "C", [ ] }
            };
            Assert.IsFalse(graph.IsCyclic());
        }
    }
}
