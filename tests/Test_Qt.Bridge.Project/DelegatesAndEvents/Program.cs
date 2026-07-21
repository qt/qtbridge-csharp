// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;

using Qt.DotNet;

[assembly: Qt.Generate(Packages = "Test", Libraries = "Qt6::Test")]

namespace DelegatesAndEvents
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("DelegatesAndEvents managed app ready");
            return 0;
        }
    }

    public static class DelegateExports
    {
        public delegate int Plus42Func(int value);

        public static Plus42Func Plus42 { get; } = static value => value + 42;
    }

    public sealed class PingCompletedEventArgs : EventArgs
    {
        public string Address { get; init; } = "";
        public long RoundtripTime { get; init; }
    }

    public sealed class PingEmitter
    {
        public event EventHandler<PingCompletedEventArgs> PingCompleted;

        public void SendAsync(string hostNameOrAddress)
        {
            PingCompleted?.Invoke(this, new PingCompletedEventArgs {
                Address = hostNameOrAddress,
                RoundtripTime = hostNameOrAddress.Length
            });
        }
    }

    public class Coord2DEventArgs : EventArgs
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class Coord3DEventArgs : Coord2DEventArgs
    {
        public double Z { get; set; }
    }

    public class Apollo11
    {
        public void Land(double x, double y, double z)
        {
            EagleLanded?.Invoke(this, new Coord3DEventArgs { X = x, Y = y, Z = z });
        }

        public event EventHandler<EventArgs> EagleLanded;
    }
}
