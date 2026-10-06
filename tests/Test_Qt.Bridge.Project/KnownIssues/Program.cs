// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_QtQuickTest
{
    // Ensure the QtQuickTest C++ host is built when the other types use metadata export.
    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class ForceSourceCodeExport { }

    public sealed class NotifierEventArgs : EventArgs { }

    [QmlElement]
    public sealed class Notifier
    {
        public event EventHandler Plain;
        public event EventHandler Custom;

        public void RaisePlain() => Plain?.Invoke(this, EventArgs.Empty);
        public void RaiseCustom() => Custom?.Invoke(this, new NotifierEventArgs());
    }

    [QmlElement]
    public sealed class Clock
    {
        private const string IsoFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public DateTime Utc { get; set; } =
            new(2026, 3, 29, 12, 34, 56, 789, DateTimeKind.Utc);
        public DateTime Local { get; set; } =
            new(2026, 3, 29, 12, 34, 56, 789, DateTimeKind.Local);

        // The instant Local stands for, as JavaScript's Date.toISOString() writes it.
        public string LocalAsIso => Local.ToUniversalTime().ToString(IsoFormat);

        // The instant a DateTime from QML stands for, or "Unspecified" if it has no Kind.
        public string Describe(DateTime value) => value.Kind == DateTimeKind.Unspecified
            ? "Unspecified"
            : value.ToUniversalTime().ToString(IsoFormat);
    }

    [QmlElement]
    public sealed class Customer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; private set; } = "Before";
        public int Revision { get; private set; }

        // A null property name means that all properties changed.
        public void ReloadAll()
        {
            Name = "After";
            Revision++;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public void ReloadEach()
        {
            Name = "After";
            Revision++;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Revision)));
        }
    }

    // A QML element that does not implement IQmlElement.
    [QmlElement]
    public sealed class Inspected
    {
        public int Value { get; set; } = 42;
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
