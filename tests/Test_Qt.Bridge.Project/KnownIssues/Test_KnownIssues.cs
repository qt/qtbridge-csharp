// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private const string RootPath = "KnownIssues";
        private const string TestCase = "Test_KnownIssues::KnownIssues";

        // QML files run on their own, since their known issues abort the process.
        private static readonly string[] SeparateRuns =
        [
            "tst_enumerateproperties.qml",
            "tst_nullableproperty.qml",
            "tst_structproperty.qml"
        ];

        // The results of one build: the run of tst_knownissues.qml, and the separate runs.
        private sealed record Results(string StdOut,
            IReadOnlyDictionary<string, (int ExitCode, string StdOut)> Separate);

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
        public async Task NullableProperty_IsValueInQml(string exportAs)
        {
            await CheckSeparateAsync(exportAs, "tst_nullableproperty.qml",
                "NullableProperty::test_nullableProperty",
                "A property of type int? aborts the process with an unhandled .NET exception "
                + "when QML uses it with source code exports. With type metadata exports, numbers "
                + "work, but null reads back as undefined.",
                "Source code exports wrap Nullable<T> as an object type with hasValue and value "
                + "properties, type metadata exports declare the property as QVariant. Convert "
                + "Nullable<T> to T or null at the boundary, like the underlying type.");
        }

        [TestMethod]
        [DataRow("source", KnownIssue.Affected)]
        [DataRow("metadata", KnownIssue.Unaffected)]
        public async Task StructProperty_ReadsInQml(string exportAs, bool affected)
        {
            await CheckSeparateAsync(exportAs, "tst_structproperty.qml",
                "StructProperty::test_read",
                "Reading the members of a property of a struct type aborts the process with an "
                + "unhandled .NET exception with source code exports; type metadata exports "
                + "read them.",
                "Not diagnosed yet. Source code exports wrap the struct as an object type, whose "
                + "getters then run on a boxed copy of it. Exporting structs as Q_GADGET value "
                + "types (QTBRIDGES-231) would replace that wrapper.",
                affected);
        }

        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task StructProperty_WritesBackInQml(string exportAs)
        {
            await CheckSeparateAsync(exportAs, "tst_structproperty.qml",
                "StructProperty::test_writeBack",
                "Setting a member of a struct property in QML, as parcel.size.width = 10, does "
                + "not change the property; QML value types such as point write back.",
                "The getter returns a boxed copy of the struct, and setting its member changes "
                + "only that copy. Export structs as Q_GADGET value types (QTBRIDGES-231): for a "
                + "property of a value type, the QML engine writes the changed value back "
                + "through the property's WRITE accessor itself.");
        }

        [TestMethod]
        [DataRow("source", KnownIssue.Affected)]
        [DataRow("metadata", KnownIssue.Unaffected)]
        public async Task InterfaceProperty_BindingUpdates(string exportAs, bool affected)
        {
            var stdOut = await RunAsync(exportAs, control: "test_controlConcreteBinding");

            Check(stdOut, "test_interfaceBinding",
                "A binding to a property of an object that is typed by an interface, such as "
                + "archive.entry.title with Entry of type IRecord, gets its first value but is "
                + "not updated when the implementing class raises PropertyChanged; it is when "
                + "the property has the class type.",
                "Source code exports wrap the object with the interface's type, whose properties "
                + "have no NOTIFY signal, since the interface does not declare PropertyChanged. "
                + "Wrap it with its runtime type, or let interface properties notify when the "
                + "object implements INotifyPropertyChanged.",
                affected);
        }

        // From a user report; built on its own, since the native build fails. Each row names
        // the identifiers its known compiler errors are about; other build failures are not the
        // known issue. Which of Action and Action<T> loses the shared header varies by run.
        [TestMethod]
        [DataRow("ObservableCollection<ProblemClass>", new[] { "Action", "problemProperty" })]
        [DataRow("ProblemClass[]", new[] { "problemProperty" })]
        public async Task SourceCode_CollectionOfClassWithDelegateProperty_Builds(
            string collection, string[] identifiers)
        {
            using var temp = new TempProject();
            temp.Create(CreateOptions("source"));
            temp.CopyFile("Program.cs", Path.Combine(RootPath, "Program.cs"));
            temp.AddFile("ProblemTypes.cs", $$"""
                using System;
                using System.Collections.ObjectModel;

                namespace Test_QtQuickTest
                {
                    public sealed class ReferencingClass
                    {
                        public {{collection}} Problems { get; set; }
                    }

                    public sealed class ProblemClass
                    {
                        public Action ProblemProperty { get; }
                    }
                }
                """);
            var build = await temp.BuildAsync();
            temp.SaveLog();

            var errors = build.Output.Split('\n').Select(line => line.Trim())
                .Where(line => line.Contains("error") && identifiers.Any(line.Contains))
                .Distinct()
                .Take(5)
                .ToArray();
            if (!build.Ok && errors.Length == 0) {
                Assert.Fail("The build fails, but not with errors about "
                    + $"{string.Join(" or ", identifiers)}.\n{build.Output}");
            }
            KnownIssue.Check(build.Ok,
                "The native build fails for a class that has an Action property and is used "
                + "in an ObservableCollection<T> or an array; the class alone builds.",
                string.Join(" | ", errors),
                "Two causes. Both rows: GenerateListAsModel, used for arrays and for the "
                + "List<T> that ObservableCollection<T> brings in, adds a role for every "
                + "readable property of the item type and reads it through a C++ getter, but a "
                + "delegate property has none, only Q_PROPERTY(QJSValue ...) and an invoke "
                + "method. Build the roles from the properties the wrapper actually exports "
                + "with a getter. ObservableCollection<T> only: Action and Action<T> share one "
                + "header, so one of them is not declared; see the generator known issue "
                + "SourceCode_GenericAndNonGenericDelegate_GetOwnHeaders.");
        }

        // Built on its own, since the code generation stops at these events today, which would
        // fail the build of the other known issues.
        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task Events_WithOtherSignatures_ReachQml(string exportAs)
        {
            const string issue = "Events whose handler is not (object sender, EventArgs-derived "
                + "args), such as Action<int> or EventHandler<int>, do not reach QML as signals "
                + "with their values.";
            const string fix = "EventRelay subscribes with RelayEvent(object, EventArgs), so "
                + "both GenerateEvent rules accept only such handlers. Subscribe through a "
                + "delegate proxy (Adapter.AddDelegateProxy) instead, and declare signals with "
                + "the handler's parameter types: in GenerateEvent for source code exports, and "
                + "in the metadata rules, metadata_loader.cpp and QDotNetDynamicObject for type "
                + "metadata exports.";
            const string qmlFile = "tst_eventsignatures.qml";
            const string testCase = "Test_KnownIssues::EventSignatures";

            using var temp = new TempProject();
            temp.Create(CreateOptions(exportAs));
            foreach (var file in new[] { "Program.cs", "EventSignatures.cs", qmlFile })
                temp.CopyFile(file, Path.Combine(RootPath, file));
            var build = await temp.BuildAsync();
            temp.SaveLog();
            if (!build.Ok) {
                var error = build.Output.Split('\n').Select(line => line.Trim())
                    .FirstOrDefault(line => line.Contains("Qt Bridge: ERROR"));
                Assert.IsNotNull(error, build.Output);
                KnownIssue.Check(false, issue, $"the code generation stops: {error}", fix);
            }

            var stdOut = (await RunQmlTestAsync(temp, qmlFile)).StdOut;
            Assert.Contains($"PASS   : {testCase}::test_controlEventArgs()", stdOut, stdOut);
            var failed = new[] { "test_action", "test_eventHandlerOfValue" }
                .Where(function => !stdOut.Contains($"PASS   : {testCase}::{function}()"))
                .ToArray();
            KnownIssue.Check(failed.Length == 0, issue,
                $"failing QML test functions: {string.Join(", ", failed)}", fix);
        }

        [TestMethod]
        [DataRow("source", KnownIssue.Unaffected)]
        [DataRow("metadata", KnownIssue.Affected)]
        public async Task ObjectKeys_OnQmlElementWithoutIQmlElement_ListsProperties(
            string exportAs, bool affected)
        {
            const string fatal = "QDotNetDynamicObject: ERROR reading nested elements for "
                + "non-element type";
            const string function = "Test_KnownIssues::EnumerateProperties::test_objectKeys()";

            var run = (await GetResultsAsync(exportAs)).Separate["tst_enumerateproperties.qml"];
            var stdOut = run.StdOut;

            var fatalLine = stdOut.Split('\n').FirstOrDefault(line => line.Contains(fatal));
            if (fatalLine == null) {
                // Without the known issue, the run must complete like any other.
                Assert.Contains("Totals: ", stdOut, stdOut);
                Assert.Contains($"PASS   : {function}", stdOut, stdOut);
                Assert.AreEqual(0, run.ExitCode, stdOut);
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
                + "types, or return an empty list instead.",
                affected);
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
            using var temp = new TempProject();
            await InitializeAndBuildAsync(temp, CreateOptions(exportAs), project => {
                foreach (var file in SeparateRuns.Prepend("tst_knownissues.qml")
                    .Prepend("Program.cs")) {
                    project.CopyFile(file, Path.Combine(RootPath, file));
                }
            });

            var run = await RunQmlTestAsync(temp, "tst_knownissues.qml");
            var separate = new Dictionary<string, (int ExitCode, string StdOut)>();
            foreach (var qmlFile in SeparateRuns) {
                var separateRun = await RunQmlTestAsync(temp, qmlFile);
                separate[qmlFile] = (separateRun.ExitCode, separateRun.StdOut);
            }

            // The known-issue functions fail on purpose, so the exit code is not 0 or 1: QtTest
            // exits with the number of failures, at most 127. A crash, or a run that stops
            // early, does not reach the totals or exits with another code.
            var failures = run.StdOut.Split('\n').Count(line => line.StartsWith("FAIL!  : "));
            Assert.Contains("Totals: ", run.StdOut, run.StdOut);
            Assert.AreEqual(Math.Min(failures, 127), run.ExitCode,
                $"{ExitCodeHelper.ToString(run.ExitCode)}\n{run.StdOut}");
            return new Results(run.StdOut, separate);
        }

        private static CreationOptions CreateOptions(string exportAs)
        {
            var options = CreateQtQuickTestOptions(Path.Combine(RootPath, "main.cpp"));
            options.AfterSdkProps = $"""
                <PropertyGroup>
                  <QtExportAs>{exportAs}</QtExportAs>
                </PropertyGroup>
                """;
            return options;
        }

        // For the QML files run on their own: the function either passes, or fails or aborts
        // the process, which ends the run.
        private static async Task CheckSeparateAsync(string exportAs, string qmlFile,
            string function, string issue, string fix, bool affected = KnownIssue.Affected)
        {
            var stdOut = (await GetResultsAsync(exportAs)).Separate[qmlFile].StdOut;
            var name = $"Test_KnownIssues::{function}()";
            if (stdOut.Contains($"PASS   : {name}")) {
                Assert.Contains("Totals: ", stdOut, stdOut);
                return;
            }

            var lines = stdOut.Split('\n').Select(line => line.Trim()).ToArray();
            var fail = Array.FindIndex(lines, line => line.StartsWith($"FAIL!  : {name}")
                || line.StartsWith("A crash occurred"));
            KnownIssue.Check(false, issue,
                fail < 0 ? "no result" : string.Join(" ", lines.Skip(fail).Take(3)), fix,
                affected);
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

        private static void Check(string stdOut, string function, string issue, string fix,
            bool affected = KnownIssue.Affected)
        {
            var lines = stdOut.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
            var fail = Array.FindIndex(lines,
                line => line.StartsWith($"FAIL!  : {TestCase}::{function}()"));
            var actual = fail < 0
                ? "no result"
                : string.Join(" ", lines.Skip(fail).Take(3).Select(line => line.Trim()));

            KnownIssue.Check(stdOut.Contains($"PASS   : {TestCase}::{function}()"),
                issue, actual, fix, affected);
        }
    }
}
