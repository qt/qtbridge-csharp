// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_QmlToManagedDelegates
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Qml.WaitForExit();
            return 0;
        }
    }

    public delegate int IntTransform(int value);
    public delegate void VoidConsumer(int value);
    public delegate int Combiner(int a, int b);
    public delegate int Provider();
    public delegate int PointInspector(Point p);
    public delegate Point PointTransform(Point p);

    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Callback
    {
        // single invocation, int -> int
        public int InvokeSingle(IntTransform callback)
        {
            return callback?.Invoke(42) ?? -1;
        }

        // void delegate: invokes callback with a fixed value, returns 0 on success
        public int InvokeVoid(VoidConsumer callback)
        {
            if (callback == null)
                return -1;
            callback.Invoke(42);
            return 0;
        }

        // Multiple invocations: calls the same delegate three times, returns sum
        public int InvokeThrice(IntTransform callback)
        {
            if (callback == null)
                return -1;
            return callback.Invoke(1) + callback.Invoke(10) + callback.Invoke(100);
        }

        // Multiple parameters
        public int InvokeCombiner(Combiner callback)
        {
            return callback?.Invoke(6, 7) ?? -1;
        }

        // No parameters
        public int InvokeProvider(Provider callback)
        {
            return callback?.Invoke() ?? -1;
        }

        // Object as delegate argument: C# creates a Point and passes it to the lambda;
        // QML reads its properties and returns a computed int value.
        public int InvokeWithPoint(PointInspector callback)
        {
            if (callback == null)
                return -1;
            return callback.Invoke(new Point { X = 3, Y = 4 });
        }

        // Object as delegate return value: C# passes a Point to the lambda and gets
        // one back; the returned object's properties are read on the C# side.
        public int InvokeWithPointRoundtrip(PointTransform callback)
        {
            if (callback == null)
                return -1;
            var result = callback.Invoke(new Point { X = 7, Y = 8 });
            return result == null ? -1 : result.X + result.Y;
        }

        // JS exception: catches the propagated InvalidOperationException,
        // returns -2 to signal the exception was received
        public int InvokeThrowing(IntTransform callback)
        {
            try {
                return callback?.Invoke(1) ?? -1;
            } catch (InvalidOperationException) {
                return -2;
            }
        }

        // BCL Action<int>: void delegate, captures side-effect via closure on QML side
        public int InvokeAction(Action<int> callback)
        {
            if (callback == null)
                return -1;
            callback.Invoke(42);
            return 0;
        }

        // BCL Func<int,int>: int -> int delegate
        public int InvokeFunc(Func<int, int> callback)
        {
            return callback?.Invoke(42) ?? -1;
        }
    }

    public class Sink
    {
        // Delegate-typed property: QML assigns a JS function, C# calls it via Fire().
        // Named ValueHandler (not OnHandler) to avoid QML's on<Signal> binding syntax.
        public Action<int> ValueHandler { get; set; }

        public void Fire(int value) => ValueHandler?.Invoke(value);
    }
}
