/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
using Qt.DotNet;

namespace Tutorial
{
    public class NameList : ListModel<string>
    {
        private List<string> Names { get; } = new();

        public void Add(string name)
        {
            BeginInsertItems(Names.Count, Names.Count);
            Names.Add(name);
            EndInsertItems();
        }

        public override string Data(int index)
        {
            if (index >= Names.Count)
                return null;
            return Names[index];
        }

        public override int ItemCount() => Names.Count;
    }
}
