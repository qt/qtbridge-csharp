// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using System.Reflection;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_Exceptions
{
    internal class Program
    {
        static int Main(string[] args)
        {
            {
                Qml.WaitForExit();
                return 0;
            }
        }
    }

    [Qt.Export(Options = Qt.ExportAs.Metadata)]
    public class MetadataExceptionGenerator
    {
        public void ExceptionMethod() => throw new Exception("[[METADATA::Method]]");
        public int ExceptionProperty
        {
            get => throw new Exception("[[METADATA::Property::get]]");
            set => throw new Exception("[[METADATA::Property::set]]");
        }
    }

    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public class SourceCodeExceptionGenerator
    {
        public void ExceptionMethod() => throw new Exception("[[SOURCE::Method]]");
        public int ExceptionProperty
        {
            get => throw new Exception("[[SOURCE::Property::get]]");
            set => throw new Exception("[[SOURCE::Property::set]]");
        }
    }
}
