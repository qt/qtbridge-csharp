#if ( CopyrightHeader )
// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only
#endif
#if ( SampleCode )
using Qt.MetaObject;
#endif
using Qt.Quick;
#if ( SampleCode )

using System.ComponentModel;
using System.Runtime.CompilerServices;
#endif

namespace NewProject;
#if ( SampleCode )

[QObject]
[QmlElement(Name = "Counter", Singleton = true)]
public class CounterService : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private int _clicks = 0;
    public int Clicks
    {
        get => _clicks;
        set
        {
            if (_clicks == value)
                return;
            _clicks = value;
            OnPropertyChanged();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
#endif

public class Program
{
    internal static void Main(string[] args)
    {
        Qml.LoadFromRootModule("Main");
        Qml.WaitForExit();
    }
}
