// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Test_Qt.Bridge.Project.Shared
{
    /// <summary>
    /// Mirror of the native BridgeExitCode enum in QtTestSetupBase.h. Keep numeric values in
    /// sync with the C++ definition.
    /// </summary>
    enum ExitCode
    {
        Ok = 0,
        QTestFailure = 1,   // QTest will return 1 on test failure

        LocateAssemblyFailed = 101,
        WaitForReadyExitedEarly = 102,
        WaitForReadyTimeout = 103,
        InitAdapterFailed = 104,
        FinalizeNotExited = 105,
        FinalizeFailed = 106
    };

    internal static class ExitCodeHelper
    {
        /// <summary>
        /// Returns a friendly description for bridge-specific exit codes (>= 100).
        /// </summary>
        public static string ToString(int exitCode)
        {
            if (!Enum.IsDefined(typeof(ExitCode), exitCode))
                return $"Failed with unknown error, exit({exitCode})";

            var code = (ExitCode)exitCode;
            if (code is ExitCode.Ok or ExitCode.QTestFailure)
                return ""; // 0 = OK, 1 = QTest failures

            return code switch {
                ExitCode.LocateAssemblyFailed
                    => "Failed to locate the generated .NET test assembly",
                ExitCode.WaitForReadyExitedEarly
                    => "Host process exited before the .NET app signaled readiness",
                ExitCode.WaitForReadyTimeout
                    => "Timeout while waiting for the .NET app to signal readiness",
                ExitCode.InitAdapterFailed
                    => "Failed to initialize the Qt/.NET Adapter",
                ExitCode.FinalizeNotExited
                    => "Finalize requested, but the .NET app did not exit in time",
                ExitCode.FinalizeFailed
                    => "Failed while finalizing the .NET app / adapter",
                _ => code.ToString()
            };
        }
    }

    /// <summary>
    /// Provides shared helpers for building and running temporary Qt Bridge native test projects.
    /// </summary>
    public abstract class ManagedTestBase
    {
        /// <summary>
        /// Creates the temp project, applies the given options, runs a build, saves the log
        /// and asserts that the build succeeded.
        /// </summary>
        protected static async Task InitializeAndBuildAsync(
            TempProject temp,
            CreationOptions options,
            Action<TempProject> configure = null)
        {
            ArgumentNullException.ThrowIfNull(temp);

            options ??= new();
            temp.Create(options);
            configure?.Invoke(temp);

            var build = await temp.BuildAsync();
            temp.SaveLog();
            Assert.IsTrue(build.Ok, build.Output);
        }

        /// <summary>
        /// Default options for a QtQuickTest native project (QtQuickTest harness).
        /// The <paramref name="mainCppTarget"/> is the path (relative to the test bin dir)
        /// where your <c>main.cpp</c> template lives, e.g. <c>QtQuickTest\main.cpp</c>.
        /// </summary>
        protected static CreationOptions CreateQtQuickTestOptions(string mainCppTarget)
        {
            return new CreationOptions
            {
                PackageReferences = [Packages.QtBridge],
                ReplaceGeneratedFiles =
                [
                    // main.cpp from test project -> generated native source
                    (@"source\cpp\main.cpp", mainCppTarget),

                    // shared helper headers/sources
                    (@"source\hpp\QtTestSetupBase.h", @"Shared\QtTestSetupBase.h"),
                    (@"source\hpp\QtQuickTestSetup.h", @"Shared\QtQuickTestSetup.h"),
                    (@"source\cpp\QtQuickTestSetup.cpp",
                        @"Shared\QtQuickTestSetup.cpp"),
                ],
                // Inject extra sources into CMakeLists.txt
                AfterSdkTargets = CMake.InjectQtSourcesTargets(
                    "hpp/QtTestSetupBase.h",
                    "hpp/QtQuickTestSetup.h",
                    "cpp/QtQuickTestSetup.cpp")
            };
        }

        /// <summary>
        /// Default options for a QtTest native project (QtTest harness).
        /// The <paramref name="mainCppTarget"/> is the path (relative to the test bin dir)
        /// where your <c>main.cpp</c> template lives, e.g. <c>QtTest\main.cpp</c>.
        /// </summary>
        protected static CreationOptions CreateQtTestOptions(string mainCppTarget)
        {
            return new CreationOptions
            {
                PackageReferences = [Packages.QtBridge],
                ReplaceGeneratedFiles =
                [
                    (@"source\cpp\main.cpp", mainCppTarget),
                    (@"source\hpp\QtTestSetupBase.h", @"Shared\QtTestSetupBase.h"),
                ],
                AfterSdkTargets = CMake.InjectQtSourcesTargets("hpp/QtTestSetupBase.h")
            };
        }

        protected class Msgs
        {
            public List<string> All { get; } = [];
            public List<string> Fail { get; } = [];
            public List<string> Warning { get; } = [];
            public List<string> Skip { get; } = [];
            public List<string> Pass { get; } = [];
            public List<string> Info { get; } = [];
        }

        private static class TestMsgParser
        {
            private static readonly List<(string Pattern, Func<Msgs, List<string>> Type)> MsgTypes =
            [
                (@"PASS\b", msgs => msgs.Pass),
                ("FAIL!", msgs => msgs.Fail),
                (@"WARNING\b", msgs => msgs.Warning),
                (@"SKIP\b", msgs => msgs.Skip),
                (@"QDEBUG\b", msgs => msgs.Info),
                (@"\*+ Start\b", msgs => msgs.Info),
                (@"Config\b", msgs => msgs.Info),
                (@"Totals\b", msgs => msgs.Info),
                (@"\*+ Finished\b", msgs => msgs.Info)
            ];

            private static readonly Dictionary<string, Func<Msgs, List<string>>> TypeSelectors =
                MsgTypes.Select((t, i) => (Name: $"_{i}", t.Type))
                    .ToDictionary(x => x.Name, x => x.Type);

            private static string Group(string rx) => $"(?:{rx})";

            private static string Group(IEnumerable<string> rxs) => Group(string.Join("", rxs));

            private static string AltGroup(IEnumerable<string> rxs) => Group(string.Join("|", rxs));

            private static string TypeGroup(int i, string rx) => $"(?<_{i}>{rx})";

            private static string NegativeLookAhead(string rx) => $"(?!{rx})";

            private static readonly string StartOfLine = AltGroup(["^", @"(?<=\n)"]);

            private static readonly string AnyChar = AltGroup([".", @"[\r\n]"]);

            private static readonly string NewLine = @"\r?\n";

            private static readonly string MessageStart
                = $"{StartOfLine}{AltGroup(MsgTypes.Select((t, i) => TypeGroup(i, t.Pattern)))}";

            private static readonly string MessageGuard
                = NegativeLookAhead(AltGroup(MsgTypes.Select(t => Group([NewLine, t.Pattern]))));

            private static readonly string MessageChar = Group([MessageGuard, AnyChar]);

            private static readonly string MessagePattern = $"{MessageStart}{MessageChar}*";

            private static readonly Regex MessageRegex = new(MessagePattern);

            public static Msgs Parse(string stdOut)
            {
                var msgs = new Msgs();
                foreach (Match match in MessageRegex.Matches(stdOut)) {
                    msgs.All.Add(match.Value);
                    foreach (var (name, selector) in TypeSelectors) {
                        if (match.Groups[name].Success) {
                            selector(msgs).Add(match.Value);
                            break;
                        }
                    }
                }
                return msgs;
            }
        }

        protected static Msgs ParseQtTestMessages(string stdOut)
        {
            return TestMsgParser.Parse(stdOut);
        }
    }
}
