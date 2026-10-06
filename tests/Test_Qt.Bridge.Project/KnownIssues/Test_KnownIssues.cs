// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.KnownIssues
{
    /// <summary>
    /// Known problems that only show at run time; see <see cref="KnownIssue"/>. The QML test
    /// application is built and run once per export mode, and each test evaluates its own QML
    /// test function.
    /// </summary>
    [TestClass]
    [TestCategory(KnownIssue.Category)]
    public class Test_KnownIssues : ManagedTestBase
    {
        private const string TestCase = "Test_KnownIssues::KnownIssues";

        // The results of one build: the run of tst_knownissues.qml, and the separate run of
        // tst_enumerateproperties.qml, whose known issue aborts the process.
        private sealed record Results(string StdOut, int EnumerateExitCode, string EnumerateStdOut);

        private static readonly ConcurrentDictionary<string, Lazy<Task<Results>>> Runs = new();

        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task Events_WithEmptyEventArgs_ReachQml(string exportAs)
        {
            var stdOut = await RunAsync(exportAs, control: "test_controlCustomEventArgs");

            Check(stdOut, "test_emptyEventArgs",
                "An event raised with EventArgs.Empty, or any other EventArgs instance that is "
                + "not of an exported type, does not emit its signal in QML; events with "
                + "arguments of an exported EventArgs subclass do.",
                "Not diagnosed yet. The native host logs 'QDotNetDynamicObject: Unrecognized "
                + "type: \"System.EventArgs, System.Private.CoreLib\"', so the event handler "
                + "likely fails to wrap arguments of a type that is not exported. Start in the "
                + "event handling of qdotnetobject.h / qdotnetdynamicobject.h.");
        }

        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task DateTime_KeepsMilliseconds(string exportAs)
        {
            var stdOut = await RunAsync(exportAs);

            Check(stdOut, "test_dateTimeMilliseconds",
                "A DateTime read in QML can lose a millisecond.",
                "DateTime crosses as an OLE automation date (a double), which cannot hold every "
                + "millisecond exactly. Marshal it exactly, for example as ticks or as "
                + "milliseconds since the epoch.");
        }

        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task DateTime_RespectsKind(string exportAs)
        {
            var stdOut = await RunAsync(exportAs);

            Check(stdOut, "test_dateTimeLocalKind",
                "A DateTime of Kind Local reaches QML as if it were UTC, shifted by the UTC "
                + "offset of the machine. (On a machine set to UTC, this test passes "
                + "regardless.)",
                "Respect DateTime.Kind when converting to QDateTime: a Local value is local "
                + "time, a Utc value is UTC.");
        }

        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task DateTime_FromQml_HasKind(string exportAs)
        {
            var stdOut = await RunAsync(exportAs);

            Check(stdOut, "test_dateTimeFromQml",
                "A date passed from QML arrives in C# as a DateTime of Kind Unspecified, so C# "
                + "cannot tell which instant it stands for.",
                "Convert a QDateTime to a DateTime of Kind Utc (or Local), according to its "
                + "time spec, instead of Unspecified.");
        }

        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task PropertyChanged_WithNullName_UpdatesAllProperties(string exportAs)
        {
            var stdOut = await RunAsync(exportAs, control: "test_controlPropertyChangedEach");

            Check(stdOut, "test_propertyChangedNullName",
                "PropertyChanged with a null or empty property name, which .NET defines as "
                + "\"all properties changed\", updates nothing in QML; bindings keep their "
                + "old values.",
                "Emit the notify signals of all properties of the object when the property name "
                + "of a PropertyChanged event is null or empty; source code exports match the "
                + "name in the generated property change handler, type metadata exports in "
                + "QDotNetDynamicObject.");
        }

        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task ObjectKeys_OnQmlElementWithoutIQmlElement_ListsProperties(
            string exportAs)
        {
            const string fatal = "QDotNetDynamicObject: ERROR reading nested elements for "
                + "non-element type";
            const string function = "Test_KnownIssues::EnumerateProperties::test_objectKeys()";

            var results = await GetResultsAsync(exportAs);
            var stdOut = results.EnumerateStdOut;

            var fatalLine = stdOut.Split('\n').FirstOrDefault(line => line.Contains(fatal));
            if (fatalLine == null) {
                // Without the known issue, the run must complete like any other.
                Assert.Contains("Totals: ", stdOut, stdOut);
                Assert.Contains($"PASS   : {function}", stdOut, stdOut);
                Assert.AreEqual(0, results.EnumerateExitCode, stdOut);
                return;
            }

            KnownIssue.Check(false,
                "Enumerating the properties of a type metadata QML element that does not "
                + "implement IQmlElement, for example with Object.keys(), aborts the process.",
                fatalLine.Trim(),
                "QDotNetDynamicObject::defineType() adds the nestedQmlElements property to every "
                + "QML element, but initQmlElement() only creates the nested elements for types "
                + "implementing IQmlElement, and reading the property without them calls "
                + "qFatal (qdotnetdynamicobject.h). Add the property only for IQmlElement "
                + "types, or return an empty list instead.");
        }

        private static Task<Results> GetResultsAsync(string exportAs)
        {
            return Runs.GetOrAdd(exportAs,
                mode => new Lazy<Task<Results>>(() => BuildAndRunAsync(mode))).Value;
        }

        private async Task<string> RunAsync(string exportAs, string control = null)
        {
            var stdOut = (await GetResultsAsync(exportAs)).StdOut;

            Assert.Contains($"PASS   : {TestCase}::initTestCase()", stdOut, stdOut);
            if (control != null)
                Assert.Contains($"PASS   : {TestCase}::{control}()", stdOut, stdOut);
            return stdOut;
        }

        private static async Task<Results> BuildAndRunAsync(string exportAs)
        {
            const string rootPath = "KnownIssues";

            using var temp = new TempProject();
            var options = CreateQtQuickTestOptions(Path.Combine(rootPath, "main.cpp"));
            options.AfterSdkProps = $"""
                <PropertyGroup>
                  <QtExportAs>{exportAs}</QtExportAs>
                </PropertyGroup>
                """;
            await InitializeAndBuildAsync(temp, options, project => {
                project.CopyFile("Program.cs", Path.Combine(rootPath, "Program.cs"));
                project.CopyFile("tst_knownissues.qml",
                    Path.Combine(rootPath, "tst_knownissues.qml"));
                project.CopyFile("tst_enumerateproperties.qml",
                    Path.Combine(rootPath, "tst_enumerateproperties.qml"));
            });

            var run = await RunQmlTestAsync(temp, "tst_knownissues.qml");
            var enumerateRun = await RunQmlTestAsync(temp, "tst_enumerateproperties.qml");

            // The known-issue functions fail on purpose, so the exit code is not 0 or 1: QtTest
            // exits with the number of failures, at most 127. A crash, or a run that stops
            // early, does not reach the totals or exits with another code.
            var failures = run.StdOut.Split('\n').Count(line => line.StartsWith("FAIL!  : "));
            Assert.Contains("Totals: ", run.StdOut, run.StdOut);
            Assert.AreEqual(Math.Min(failures, 127), run.ExitCode,
                $"{ExitCodeHelper.ToString(run.ExitCode)}\n{run.StdOut}");
            return new Results(run.StdOut, enumerateRun.ExitCode, enumerateRun.StdOut);
        }

        private static Task<(int ExitCode, string StdOut, string StdErr)> RunQmlTestAsync(
            TempProject temp, string qmlFile)
        {
            return temp.RunAsync(new() {
                Args = ["-input", Path.Combine(temp.ExeDir, "Application", qmlFile)],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });
        }

        private static void Check(string stdOut, string function, string issue, string fix)
        {
            var lines = stdOut.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
            var fail = Array.FindIndex(lines,
                line => line.StartsWith($"FAIL!  : {TestCase}::{function}()"));
            var actual = fail < 0
                ? "no result"
                : string.Join(" ", lines.Skip(fail).Take(3).Select(line => line.Trim()));

            KnownIssue.Check(stdOut.Contains($"PASS   : {TestCase}::{function}()"),
                issue, actual, fix);
        }
    }
}
