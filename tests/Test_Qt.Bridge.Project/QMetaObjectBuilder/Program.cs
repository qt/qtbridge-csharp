// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace QMetaObjectBuilderFixture
{
    // Inherits methods from another assembly. Metadata tokens are only unique per module, so
    // the first methods of this assembly have the same tokens as some of the inherited ones,
    // e.g. ConsoleTraceListener.Close(). Declared first, so its own methods get the first
    // tokens of this assembly.
    [QmlElement]
    public class InheritedMethodsObject : ConsoleTraceListener
    {
        public int Own1() => Count(1);
        public int Own2() => Count(2);
        public int Own3() => Count(3);
        public int Own4() => Count(4);
        public int Own5() => Count(5);
        public int Own6() => Count(6);
        public int Own7() => Count(7);
        public int Own8() => Count(8);
        public int Own9() => Count(9);
        public int Own10() => Count(10);

        private int ownCalls;
        private int Count(int value)
        {
            ++ownCalls;
            return value;
        }

        // Number of calls of the own methods, to detect calls that reach the wrong method.
        public int OwnCalls() => ownCalls;

        // Pairs "Own:Inherited" of an own method and a parameterless inherited method from
        // another module that have the same metadata token. The test needs at least one.
        public string CollidingMethods()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var inherited = GetType().GetMethods(flags)
                .Where(m => m.Module != GetType().Module && m.GetParameters().Length == 0)
                .ToLookup(m => m.MetadataToken, m => m.Name);
            return string.Join(",", GetType().GetMethods(flags | BindingFlags.DeclaredOnly)
                .SelectMany(m => inherited[m.MetadataToken].Select(name => $"{m.Name}:{name}")));
        }
    }

    // Arrays of metadata types are metadata types themselves. Their Get(), Set() and Address()
    // are provided by the runtime and have no metadata token of their own.
    public sealed class MetadataItem(int value)
    {
        public int Value { get; } = value;
    }

    public sealed class MetadataEventArgs(int value) : EventArgs
    {
        public int Value { get; } = value;
    }

    // Ensure the QtQuickTest C++ host is built when the other types use metadata export.
    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class SourceCodeDependency
    {
        public int Id { get; } = 1;
    }

    [QmlElement]
    public sealed class MetadataObject
    {
        public SourceCodeDependency Dependency { get; } = new();

        public event EventHandler<MetadataEventArgs> Changed;
        public event EventHandler<MetadataEventArgs> Updated;

        public int Value() => 42;
        public MetadataItem[] Items() => [new(1), new(2)];
        public int Add(int left, int right) => left + right;

        public void RaiseEvents()
        {
            Changed?.Invoke(this, new(1));
            Updated?.Invoke(this, new(2));
        }
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
