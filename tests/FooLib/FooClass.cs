// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
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

        public static void VariantStringToUpper(IQVariant v)
        {
            v.SetValue(v.ToStringValue().ToUpper());
        }

        public static IQVariant GetVariant()
        {
            return Adapter.Static.QVariant_Create();
        }

        public static IQVariant GetVariant(string value)
        {
            return Adapter.QVariant(value);
        }

        public static IQModelIndex GetModelIndex()
        {
            return Adapter.Static.QModelIndex_Create();
        }

        public static int ModelIndexRowColPtr(IQModelIndex idx)
        {
            return idx.Row() * idx.Column() * (int)idx.InternalPointer();
        }

        public static void InitModel()
        {
            if (Model is null)
                Model = new TestListModel();
        }

        public static void CleanupModel()
        {
            if (Model is null)
                return;
            Model = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public static QAbstractListModel Model { get; private set; }

        public class TestListModel : QAbstractListModel
        {
            public override int Flags(IQModelIndex index = null)
            {
                if (index == null)
                    return base.Flags(index);
                return (int)( ItemFlag.ItemIsSelectable
                            | ItemFlag.ItemIsEnabled
                            | ItemFlag.ItemNeverHasChildren);
            }

            public override int RowCount(IQModelIndex parent = null)
            {
                return 2;
            }

            public override IQVariant Data(IQModelIndex index, int role = 0)
            {
                switch (index.Row()) {
                    case 0:
                        return Adapter.QVariant("FOO");
                    default:
                        return Adapter.QVariant("BAR");
                }
            }
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
