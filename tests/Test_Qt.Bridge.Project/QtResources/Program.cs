// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

[assembly: Qt.Generate(Packages = "Test", Libraries = "Qt6::Test")]

namespace QtResources
{
    internal class Program
    {
        static int Main(string[] args) => 0;
    }

    public static class ResourceValidator
    {
        private const string Url = "qrc:/assemblies/QtResources/sample.txt";

        public static bool CheckExists() => Qt.Resources.Exists(Url);

        public static bool CheckContent()
            => Qt.Resources.ReadAllText(Url).TrimEnd() == "Hello from Qt Resources!";
    }
}
