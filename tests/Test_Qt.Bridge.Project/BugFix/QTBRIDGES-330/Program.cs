// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

[assembly: Qt.Generate(Packages = "Test", Libraries = "Qt6::Test")]

namespace Test_InboundMemLeak
{
    public static class Functions
    {
        private static Stopwatch SampleTimer { get; } = Stopwatch.StartNew();
        private static readonly TimeSpan SampleDuration = TimeSpan.FromSeconds(1);
        private static List<double> SampleCalls { get; } = new(100);
        private static List<double> SampleBytes { get; } = new(100);

        private static int CallCount { get; set; } = 0;

        private static long AllocatedBytes()
        {
            using var process = Process.GetCurrentProcess();
            return process.PrivateMemorySize64;
        }

        public static double Correlation()
        {
            var r = MathNet.Numerics.Statistics.Correlation.Pearson(SampleCalls, SampleBytes);
            return r;
        }

        public static void InboundVoid()
        {
            Stopwatch delayTimer = Stopwatch.StartNew();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            while (delayTimer.Elapsed.TotalNanoseconds < 400000) ;

            CallCount++;
            if (SampleTimer.Elapsed < SampleDuration)
                return;
            SampleTimer.Restart();

            SampleCalls.Add(CallCount);
            SampleBytes.Add(AllocatedBytes());
        }

        private static T Inbound<T>(T value)
        {
            InboundVoid();
            return value;
        }

        public static int InboundInt32() => Inbound(valueInt32);

        public static char InboundChar() => Inbound(valueChar);

        public static string InboundString() => Inbound(valueString);

        public static DateTime InboundDateTime() => Inbound(valueDateTime);

        public static Uri InboundUri() => Inbound(valueUri);

        public static object InboundObject() => Inbound(valueObject);

        private static readonly int valueInt32 = Environment.ProcessorCount;
        private static readonly char valueChar = Path.DirectorySeparatorChar;
        private static readonly string valueString = Environment.CommandLine;
        private static readonly DateTime valueDateTime = DateTime.Now;
        private static readonly Uri valueUri = new("https://qt.io");
        private static readonly object valueObject = new();
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            return 0;
        }
    }
}
