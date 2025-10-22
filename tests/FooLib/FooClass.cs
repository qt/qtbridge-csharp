// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Qt.DotNet;
using Qt.MetaObject;

namespace FooLib
{
    public interface IBarTransformation
    {
        string Transform(string bar);
        Uri GetUri(int n);
        void SetUri(Uri uri);
        int GetNumber();
    }

    public class BarIdentity : IBarTransformation
    {
        public string Transform(string bar) => bar;
        public Uri GetUri(int n) => null;
        public void SetUri(Uri uri) { }
        public int GetNumber() => 0;
    }

    public class Foo : INotifyPropertyChanged
    {
        public const int FooNumber = 42;
        public const string FooString = "FOO";
        public int FooField = FooNumber;
        public static int FooStaticField = -FooNumber;
        public Foo(IBarTransformation barTransformation)
        {
            BarTransformation = barTransformation;
        }

        public Foo() : this(null)
        { }

        private IBarTransformation BarTransformation { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string bar;
        public string Bar
        {
            get => bar;
            set
            {
                bar = value;
                if (BarTransformation is IBarTransformation) {
                    bar = BarTransformation.Transform(value) ?? bar;
                    BarTransformation.SetUri(new("https://qt.io/developers"));
                    var n = BarTransformation.GetNumber();
                    if (BarTransformation.GetUri(n) is { } uri)
                        bar += $" ({uri})";
                }
                NotifyPropertyChanged();
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public delegate string FormatNumberDelegate(
            [In, MarshalAs(UnmanagedType.LPWStr)] string format, int number);

        public static string FormatNumber(string format, int number)
        {
            return string.Format(format, number);
        }

        public static int EntryPoint(IntPtr arg, int argLength)
        {
            return Convert.ToInt32(Marshal.PtrToStringUni(arg, argLength));
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class Date
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Year = "";
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Month = "";
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Day = "";
        }

        [return: MarshalAs(UnmanagedType.LPWStr)]
        public delegate string FormatDateDelegate(
            [In, MarshalAs(UnmanagedType.LPWStr)] string format, [In] Date date);

        public static string FormatDate(string format, Date date)
        {
            return string.Format(format, date.Year, date.Month, date.Day);
        }

        public delegate int FooFunc(int x);

        public static FooFunc Plus42 { get; } = new FooFunc(x => x + 42);

        public static ModelIndex FindIndex()
        {
            return new(42, 24, 0x12345678);
        }

        public static string DataAt(ModelIndex idx)
        {
            return $"{idx.Row}, {idx.Column}, 0x{idx.Id:X}";
        }

        public static DateTime GetDateTime()
        {
            return new DateTime(1912, 6, 23, 11, 22, 33, 444);
        }

        public static string PrintDateTime(DateTime t)
        {
            return $"{t.Year:0000}-{t.Month:00}-{t.Day:00} {t.Hour:00}:{t.Minute:00}:{t.Second:00}.{t.Millisecond:000}";
        }

        public static Uri GetUri()
        {
            return new Uri("https://www.qt.io/developers#wiki");
        }

        public static string PrintUri(Uri uri)
        {
            return uri.ToString();
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
            EagleLanded?.Invoke(this, new Coord3DEventArgs() { X = x, Y = y, Z = z });
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
            char latNS = args.Y >= 0 ? 'N' : 'S';
            Param1 = $"{lat.Hours}\u00B0 {lat.Minutes}' {lat.Seconds}'' {latNS}";

            var lon = TimeSpan.FromHours(Math.Abs(args.X));
            char lonEW = args.X >= 0 ? 'E' : 'W';
            Param2 = $"{lon.Hours}\u00B0 {lon.Minutes}' {lon.Seconds}'' {lonEW}";

            return true;
        }
    }
}
