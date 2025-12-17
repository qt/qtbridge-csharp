/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

global using System.Threading.Tasks;
global using Microsoft.Build.Logging.StructuredLogger;
global using BuildTask = Microsoft.Build.Logging.StructuredLogger.Task;
global using Task = System.Threading.Tasks.Task;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Test_Qt.Bridge.Project
{
    public static class BinLogExtensions
    {
        public static bool TryFindTarget(this TreeNode node, string name, out Target target)
        {
            target = node?.FindFirstDescendant<Target>(t => t.Name == name && !t.Skipped);
            return target != null;
        }

        public static IEnumerable<string> GetMessages(this Target target)
        {
            return target
                .Children.OfType<BuildTask>()
                .SelectMany(t => t.GetMessages()).Select(m => m.Text);
        }

        public static bool HasMessage(this Target target, Regex pattern)
        {
            return target.GetMessages().Any(msg => pattern.Match(msg).Success);
        }

        public static IEnumerable<BuildTask> GetTasks(this Target target, string taskName)
        {
            return target.FindChildrenRecursive<BuildTask>(t => t.Name == taskName);
        }

        public static IEnumerable<Item> GetParamItems(this BuildTask task, string paramName)
        {
            return task.FindChildrenRecursive<Parameter>(p => p.Name == paramName)
                .SelectMany(p => p.FindChildrenRecursive<Item>());
        }
    }
}
