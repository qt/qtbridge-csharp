/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections;
using System.Reflection;

namespace Qt.MetaObject
{
    public class Signal : IEnumerable<object>
    {
        public string Name { get; protected set; }
        public virtual object this[int index] { get => null; protected set { } }
        public virtual int Count => 0;

        public static Signal[] FromEvent(string name, object sender)
        {
            var attribs = sender.GetType().GetEvent(name)
                .GetCustomAttributes(false)
                .Where(x => x is QSignalAttribute)
                .Cast<QSignalAttribute>()
                .ToArray();
            foreach (var attrib in attribs) {
                if (attrib.Signal is { } signal && string.IsNullOrEmpty(attrib.Signal.Name))
                    signal.Name = attrib.Name ?? name;
            }
            return attribs
                .Select(x => x.Signal)
                .Where(x => x != null)
                .ToArray();
        }

        public static bool Convert(Signal signal, object sender, EventArgs args)
        {
            if (signal.GetType() == typeof(Signal))
                return true;
            var convert = signal.GetType()
                .GetMethod("Convert", BindingFlags.Public | BindingFlags.Instance);
            if (convert == null || convert.ReturnType != typeof(bool))
                return false;
            if (convert.GetParameters() is not { Length: 2 } convertParams)
                return false;
            if (!convertParams[1].ParameterType.IsAssignableFrom(args.GetType()))
                return false;
            try {
                var ok = convert?.Invoke(signal, new object[] { sender, args });
                return (bool)ok;
            } catch (Exception) {
                return false;
            }
        }

        internal bool AutoConvert<TEvent>(Signal signal, object sender, TEvent args)
            where TEvent : EventArgs
        {
            var props = typeof(TEvent)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < int.Min(props.Length, signal.Count); ++i)
                signal[i] = props[i].GetValue(args);
            return true;
        }

        protected static TArg ArgValue<TArg>(object param)
        {
            return (TArg)ValueConverter.ConvertValue(param, typeof(TArg));
        }

        private IEnumerable<object> AsEnumerable =>
            Enumerable.Range(0, Count).Select(idx => this[idx]);

        public IEnumerator<object> GetEnumerator() => AsEnumerable.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)AsEnumerable).GetEnumerator();
    }

    public abstract class Signal<TEvent> : Signal
        where TEvent : EventArgs
    {
        public abstract bool Convert(object sender, TEvent args);
    }

    public abstract class Signal
        <TEvent, T1>
        : Signal<TEvent>
        where TEvent : EventArgs
    {
        public Type Type1 => typeof(T1);
        public virtual T1 Arg1 { get; private set; }
        protected object Param1 { set { Arg1 = ArgValue<T1>(value); } }
        public override int Count => 1;
        public override object this[int index]
        {
            get => (index == 0) ? Arg1 : base[index];
            protected set
            {
                if (index == 0)
                    Param1 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2>
        : Signal<TEvent, T1>
        where TEvent : EventArgs
    {
        public Type Type2 => typeof(T2);
        public virtual T2 Arg2 { get; private set; }
        protected object Param2 { set { Arg2 = ArgValue<T2>(value); } }
        public override int Count => 2;
        public override object this[int index]
        {
            get => index switch { < 1 => base[index], 1 => Arg2, _ => null };
            protected set
            {
                if (index < 1)
                    base[index] = value;
                else if (index == 1)
                    Param2 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3>
        : Signal<TEvent, T1, T2>
        where TEvent : EventArgs
    {
        public Type Type3 => typeof(T3);
        public virtual T3 Arg3 { get; private set; }
        protected object Param3 { set { Arg3 = ArgValue<T3>(value); } }
        public override int Count => 3;
        public override object this[int index]
        {
            get => index switch { < 2 => base[index], 2 => Arg3, _ => null };
            protected set
            {
                if (index < 2)
                    base[index] = value;
                else if (index == 2)
                    Param3 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4>
        : Signal<TEvent, T1, T2, T3>
        where TEvent : EventArgs
    {
        public Type Type4 => typeof(T4);
        public virtual T4 Arg4 { get; private set; }
        protected object Param4 { set { Arg4 = ArgValue<T4>(value); } }
        public override int Count => 4;
        public override object this[int index]
        {
            get => index switch { < 3 => base[index], 3 => Arg4, _ => null };
            protected set
            {
                if (index < 3)
                    base[index] = value;
                else if (index == 3)
                    Param4 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5>
        : Signal<TEvent, T1, T2, T3, T4>
        where TEvent : EventArgs
    {
        public Type Type5 => typeof(T5);
        public virtual T5 Arg5 { get; private set; }
        protected object Param5 { set { Arg5 = ArgValue<T5>(value); } }
        public override int Count => 5;
        public override object this[int index]
        {
            get => index switch { < 4 => base[index], 4 => Arg5, _ => null };
            protected set
            {
                if (index < 4)
                    base[index] = value;
                else if (index == 4)
                    Param5 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6>
        : Signal<TEvent, T1, T2, T3, T4, T5>
        where TEvent : EventArgs
    {
        public Type Type6 => typeof(T6);
        public virtual T6 Arg6 { get; private set; }
        protected object Param6 { set { Arg6 = ArgValue<T6>(value); } }
        public override int Count => 6;
        public override object this[int index]
        {
            get => index switch { < 5 => base[index], 5 => Arg6, _ => null };
            protected set
            {
                if (index < 5)
                    base[index] = value;
                else if (index == 5)
                    Param6 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6, T7>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6>
        where TEvent : EventArgs
    {
        public Type Type7 => typeof(T7);
        public virtual T7 Arg7 { get; private set; }
        protected object Param7 { set { Arg7 = ArgValue<T7>(value); } }
        public override int Count => 7;
        public override object this[int index]
        {
            get => index switch { < 6 => base[index], 6 => Arg7, _ => null };
            protected set
            {
                if (index < 6)
                    base[index] = value;
                else if (index == 6)
                    Param7 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7>
        where TEvent : EventArgs
    {
        public Type Type8 => typeof(T8);
        public virtual T8 Arg8 { get; private set; }
        protected object Param8 { set { Arg8 = ArgValue<T8>(value); } }
        public override int Count => 8;
        public override object this[int index]
        {
            get => index switch { < 7 => base[index], 7 => Arg8, _ => null };
            protected set
            {
                if (index < 7)
                    base[index] = value;
                else if (index == 7)
                    Param8 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8>
        where TEvent : EventArgs
    {
        public Type Type9 => typeof(T9);
        public virtual T9 Arg9 { get; private set; }
        protected object Param9 { set { Arg9 = ArgValue<T9>(value); } }
        public override int Count => 9;
        public override object this[int index]
        {
            get => index switch { < 8 => base[index], 8 => Arg9, _ => null };
            protected set
            {
                if (index < 8)
                    base[index] = value;
                else if (index == 8)
                    Param9 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        where TEvent : EventArgs
    {
        public Type Type10 => typeof(T10);
        public virtual T10 Arg10 { get; private set; }
        protected object Param10 { set { Arg10 = ArgValue<T10>(value); } }
        public override int Count => 10;
        public override object this[int index]
        {
            get => index switch { < 9 => base[index], 9 => Arg10, _ => null };
            protected set
            {
                if (index < 9)
                    base[index] = value;
                else if (index == 9)
                    Param10 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        where TEvent : EventArgs
    {
        public Type Type11 => typeof(T11);
        public virtual T11 Arg11 { get; private set; }
        protected object Param11 { set { Arg11 = ArgValue<T11>(value); } }
        public override int Count => 11;
        public override object this[int index]
        {
            get => index switch { < 10 => base[index], 10 => Arg11, _ => null };
            protected set
            {
                if (index < 10)
                    base[index] = value;
                else if (index == 10)
                    Param11 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        where TEvent : EventArgs
    {
        public Type Type12 => typeof(T12);
        public virtual T12 Arg12 { get; private set; }
        protected object Param12 { set { Arg12 = ArgValue<T12>(value); } }
        public override int Count => 12;
        public override object this[int index]
        {
            get => index switch { < 11 => base[index], 11 => Arg12, _ => null };
            protected set
            {
                if (index < 11)
                    base[index] = value;
                else if (index == 11)
                    Param12 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
        where TEvent : EventArgs
    {
        public Type Type13 => typeof(T13);
        public virtual T13 Arg13 { get; private set; }
        protected object Param13 { set { Arg13 = ArgValue<T13>(value); } }
        public override int Count => 13;
        public override object this[int index]
        {
            get => index switch { < 12 => base[index], 12 => Arg13, _ => null };
            protected set
            {
                if (index < 12)
                    base[index] = value;
                else if (index == 12)
                    Param13 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
        where TEvent : EventArgs
    {
        public Type Type14 => typeof(T14);
        public virtual T14 Arg14 { get; private set; }
        protected object Param14 { set { Arg14 = ArgValue<T14>(value); } }
        public override int Count => 14;
        public override object this[int index]
        {
            get => index switch { < 13 => base[index], 13 => Arg14, _ => null };
            protected set
            {
                if (index < 13)
                    base[index] = value;
                else if (index == 13)
                    Param14 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
        where TEvent : EventArgs
    {
        public Type Type15 => typeof(T15);
        public virtual T15 Arg15 { get; private set; }
        protected object Param15 { set { Arg15 = ArgValue<T15>(value); } }
        public override int Count => 15;
        public override object this[int index]
        {
            get => index switch { < 14 => base[index], 14 => Arg15, _ => null };
            protected set
            {
                if (index < 14)
                    base[index] = value;
                else if (index == 14)
                    Param15 = value;
            }
        }
    }

    public abstract class Signal
        <TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
        where TEvent : EventArgs
    {
        public Type Type16 => typeof(T16);
        public virtual T16 Arg16 { get; private set; }
        protected object Param16 { set { Arg16 = ArgValue<T16>(value); } }
        public override int Count => 16;
        public override object this[int index]
        {
            get => index switch { < 15 => base[index], 15 => Arg16, _ => null };
            protected set
            {
                if (index < 15)
                    base[index] = value;
                else if (index == 15)
                    Param16 = value;
            }
        }
    }

    public class
        AutoSignal<TEvent, T1>
        : Signal<TEvent, T1>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2>
        : Signal<TEvent, T1, T2>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3>
        : Signal<TEvent, T1, T2, T3>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4>
        : Signal<TEvent, T1, T2, T3, T4>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5>
        : Signal<TEvent, T1, T2, T3, T4, T5>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6, T7>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }

    public class
        AutoSignal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
        : Signal<TEvent, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
        where TEvent : EventArgs
    {
        public override bool Convert(object sender, TEvent args)
        {
            return AutoConvert(this, sender, args);
        }
    }
}
