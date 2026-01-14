// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt.Bridge.Models;

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
