// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using System.Reflection;
using Qt;
using Qt.Quick;

[assembly: Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_GetType
{
    [Export(Options = ExportAs.Metadata)]
    public class Foo { }

    [Export(Options = ExportAs.SourceCode)]
    public class Bar { }

    [Export(Options = ExportAs.Metadata)]
    public class MetadataCustomType
    {
        public int getType() => 123;
        public int getType(int value) => value;
    }

    [Export(Options = ExportAs.SourceCode)]
    public class SourceCustomType
    {
        public int getType() => 123;
        public int getType(int value) => value;
    }

    [Export(Options = ExportAs.Metadata)]
    public class MetadataTypeProperty { public new string GetType => "custom"; }

    [Export(Options = ExportAs.SourceCode)]
    public class SourceTypeProperty { public new string GetType => "custom"; }

    [Export(Options = ExportAs.SourceCode)]
    public class SourceTypeField { public new string GetType = "custom"; }

    [QmlElement(Singleton = true)]
    public class TypeToStringOf
    {
        public string Foo => typeof(Foo).ToString();
        public string Bar => typeof(Bar).ToString();
        public string MetadataCustomType => typeof(MetadataCustomType).ToString();
        public string SourceCustomType => typeof(SourceCustomType).ToString();
        public string MetadataTypeProperty => typeof(MetadataTypeProperty).ToString();
        public string SourceTypeProperty => typeof(SourceTypeProperty).ToString();
        public string SourceTypeField => typeof(SourceTypeField).ToString();
    }

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
}
