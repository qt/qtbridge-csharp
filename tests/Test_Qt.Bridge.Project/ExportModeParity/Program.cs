// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_QtQuickTest
{
    // Names that C++ cannot use as they are, acronyms, and single letters.
    public enum Shade { Red = 0, Green = -1, _Hidden = 5, Default = 7 }

    // Ensure the QtQuickTest C++ host is built when the other types use metadata export.
    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class ForceSourceCodeExport { }

    public sealed class LibraryEventArgs : EventArgs { }

    [QmlElement]
    public sealed class Library : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler URLChanged;
        public event EventHandler Delete;

        private string urlPath = "";
        public string URLPath
        {
            get => urlPath;
            set { urlPath = value; Notify(nameof(URLPath)); }
        }

        private int newValue;
        public int New
        {
            get => newValue;
            set { newValue = value; Notify(nameof(New)); }
        }

        private int count;
        public int _Count
        {
            get => count;
            set { count = value; Notify(nameof(_Count)); }
        }

        public int X { get; set; }

        public Shade Shade { get; set; } = Shade.Green;

        public string GetHTTPResponse(string url, int retries) => $"{url}:{retries}";
        public bool IsOK() => true;
        public void Switch() => X = -X;
        public void _Reset() => _Count = 0;
        public int A() => 1;
        public int this[int index] { get => index * 2; set { X = index + value; } }

        public void RaiseEvents()
        {
            URLChanged?.Invoke(this, new LibraryEventArgs());
            Delete?.Invoke(this, new LibraryEventArgs());
        }

        private void Notify(string name) => PropertyChanged?.Invoke(this, new(name));
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
