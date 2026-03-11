// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt.Quick;

namespace Tutorial
{
    internal class ModelsAndViews
    {
        static void Main(string[] args)
        {
            Qml.LoadFromRootModule("ListViewApp");
            Qml.LoadFromRootModule("TableViewApp");
            Qml.LoadFromRootModule("TreeViewApp");
            Qml.WaitForExit();
        }
    }
}
