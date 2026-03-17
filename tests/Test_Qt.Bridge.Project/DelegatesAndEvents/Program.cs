// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;

using Qt.DotNet;
using Qt.MetaObject;

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

    [QObject(Name = "ApolloXI")]
    public class Apollo11
    {
        [QSlot(Name = "GoForEagleLanding")]
        public void Land(double x, double y, double z)
        {
            EagleLanded?.Invoke(this, new Coord3DEventArgs { X = x, Y = y, Z = z });
        }

        [QSignal]
        [QSignal(Name = "TheEagleHasLanded")]
        [QSignal<Coord3DEventArgs, string, string>(Name = "TheEagleHasLanded_WRONG_PARAMS")]
        [QSignal<Coord2DEventArgs, string, string>(Name = "TheEagleHasLanded_WRONG_ORDER")]
        [QSignal<UnhandledExceptionEventArgs, string, bool>(Name = "TheEagleHasLanded_WRONG_EVENT")]
        [QSignal<EagleLandedSignal>(Name = "TheEagleHasLanded_OK")]
        public event EventHandler<EventArgs> EagleLanded;
    }

    public class EagleLandedSignal : Signal<Coord3DEventArgs, string, string>
    {
        public override bool Convert(object sender, Coord3DEventArgs args)
        {
            var lat = TimeSpan.FromHours(Math.Abs(args.Y));
            char latNs = args.Y >= 0 ? 'N' : 'S';
            Param1 = $"{lat.Hours}° {lat.Minutes}' {lat.Seconds}'' {latNs}";

            var lon = TimeSpan.FromHours(Math.Abs(args.X));
            char lonEw = args.X >= 0 ? 'E' : 'W';
            Param2 = $"{lon.Hours}° {lon.Minutes}' {lon.Seconds}'' {lonEw}";

            return true;
        }
    }
}
