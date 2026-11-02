// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using Qt.Quick;

namespace Test_QtQuickTest
{
    public sealed class SignallerEventArgs : EventArgs { }

    // Built without the other known-issue types, since the events with other signatures stop
    // the code generation.
    [QmlElement]
    public sealed class Signaller
    {
        public event EventHandler<SignallerEventArgs> Signalled;
        public event Action<int> Counted;
        public event EventHandler<int> Moved;

        public void RaiseSignalled() => Signalled?.Invoke(this, new SignallerEventArgs());
        public void RaiseCounted(int value) => Counted?.Invoke(value);
        public void RaiseMoved(int value) => Moved?.Invoke(this, value);
    }
}
