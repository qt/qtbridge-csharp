/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections;

namespace Qt.Bridge.Utils.Collections
{
    public static class GraphExtensions
    {
        public static IEnumerable<T> FindCycle<T, LT>(this IDictionary<T, LT> graph)
            where LT : IEnumerable<T>
        {
            HashSet<T> visited = new();
            HashSet<T> inPath = new();
            Stack<T> stack = new();
            foreach (var node in graph.Keys) {
                if (visited.Contains(node))
                    continue;
                stack.Push(node);
                while (stack.Any()) {
                    var top = stack.Peek();
                    if (!visited.Contains(top)) {
                        visited.Add(top);
                        inPath.Add(top);
                    } else {
                        inPath.Remove(top);
                        stack.Pop();
                    }
                    if (!graph.ContainsKey(top))
                        continue;
                    foreach (var adj in graph[top]) {
                        if (!visited.Contains(adj))
                            stack.Push(adj);
                        else if (inPath.Contains(adj))
                            return stack.Prepend(adj);
                    }
                }
            }
            return null;
        }

        public static bool IsCyclic<T, LT>(this IDictionary<T, LT> graph)
            where LT : IEnumerable<T>
        {
            return graph.FindCycle()?.Any() ?? false;
        }
    }
}
