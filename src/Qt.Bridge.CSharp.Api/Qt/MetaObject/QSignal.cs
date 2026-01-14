// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;

namespace Qt.MetaObject
{
    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute : Attribute
    {
        public string Name { get; set; }
        public virtual Signal Signal { get; } = new();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TSignal>
        : QSignalAttribute where TSignal : Signal, new()
    {
        public override Signal Signal { get; } = new TSignal();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent, T1>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6, T7>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6, T7>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6, T7, T8>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6, T7, T8, T9>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>();
    }

    [AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
    public class QSignalAttribute
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
        : QSignalAttribute where TEvent : EventArgs
    {
        public override Signal Signal { get; } = new AutoSignal<TEvent,
            T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>();
    }
}
