// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Collections.Generic;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_QtQuickTest
{
    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class Person(string name, int age)
    {
        public string Name { get; } = name;
        public int Age { get; } = age;
    }

    [QmlElement]
    public sealed class CollectionModels : IQmlElement
    {
        public int[] Numbers { get; } = [2, 3, 5];

        public List<Person> People { get; } =
        [
            new("Ada", 36),
            new("Grace", 85)
        ];

        public void QmlClassBegin()
        { }

        public void QmlComponentComplete(object[] nestedElements)
        { }
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            Qml.WaitForExit();
            return 0;
        }
    }
}
