// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Threading;
using Qt.Quick;

namespace Test_QmlLoad
{
    [QmlElement(Singleton = true)]
    public class QmlLoad
    {
        private static string _TestName { get; set; } = "";
        public string TestName => _TestName;

        private static bool? _Result { get; set; } = null;
        public bool Result
        {
            get => _Result ?? false;
            set => _Result = value;
        }
        internal static bool Loaded => _Result != null;
        internal static bool Ok => _Result == true;

        internal static string Test { get; set; } = "";
        public string TestExpr => string.IsNullOrEmpty(Test) ? "true" : Test;

        internal static void Reset(string testName)
        {
            _TestName = testName;
            _Result = null;
            Test = "";
        }

        internal static void Assert(string expr, string msg)
        {
            if (!string.IsNullOrEmpty(Test))
                Test += " && ";
            Test += $"assert({expr}, \"{msg}\")";
        }
    }

    public class AutoLoadArgs : IQmlLoadArgs
    {
        public static string Module { get; set; } = null;
        public static string Name { get; set; } = "Main";
        public int Foo { get; set; }
        public string[] Bar { get; set; }
    }

    public class ManualLoadArgs : IQmlLoadArgs
    {
        public static string Module { get; set; } = null;
        public static string Name { get; set; } = "Main";
        public Dictionary<string, object> InitialProperties { get; set; }
    }

    public class SetterOnlyLoadArgs : IQmlLoadArgs
    {
        public static string Module { get; set; } = null;
        public static string Name { get; set; } = "Main";
        public int Foo { private get; set; } = 42;
        public string[] Bar { private get; set; } = ["b", "a", "r"];
    }

    internal class HiddenBase
    {
        public double Foo => 4.2;
    }

    internal sealed class HiddenPropertyArgs : HiddenBase, IQmlLoadArgs
    {
        public static string Name => "Main";
        public new int Foo { get; set; }
        public string[] Bar { get; set; }
    }

    internal class Program
    {
        private static void Reset(string testName, bool defaultTest = true)
        {
            AutoLoadArgs.Module = null;
            AutoLoadArgs.Name = "Main";
            ManualLoadArgs.Module = null;
            ManualLoadArgs.Name = "Main";
            SetterOnlyLoadArgs.Module = null;
            SetterOnlyLoadArgs.Name = "Main";
            QmlLoad.Reset(testName);
            if (defaultTest) {
                Assert("foo === 42", "'foo' value");
                Assert("bar.length === 3", "'bar' length");
                Assert("bar.get(0) === 'b'", "'bar[0]' value");
                Assert("bar.get(1) === 'a'", "'bar[1]' value");
                Assert("bar.get(2) === 'r'", "'bar[2]' value");
            }
        }

        private static void Assert(string expr, string msg) => QmlLoad.Assert(expr, msg);

        internal static int Main(string[] args)
        {
            try {

                /////////////////////////////////////////////////////////////////////////
                Reset("AutoArgs");
                Qml.Load<AutoLoadArgs>(new()
                {
                    Foo = 42,
                    Bar = ["b", "a", "r"]
                });
                SpinWait.SpinUntil(() => QmlLoad.Loaded, 5000);
                if (!QmlLoad.Ok)
                    return (int)Result.Fail_AutoArgs;

                /////////////////////////////////////////////////////////////////////////
                Reset("ManualArgs");
                Qml.Load<ManualLoadArgs>(new()
                {
                    InitialProperties = new()
                    {
                        ["Foo"] = 42,
                        ["Bar"] = new[] { "b", "a", "r" }
                    }
                });
                SpinWait.SpinUntil(() => QmlLoad.Loaded, 5000);
                if (!QmlLoad.Ok)
                    return (int)Result.Fail_ManualArgs;

                /////////////////////////////////////////////////////////////////////////
                Reset("NullRefArg", false);
                Assert("false", "Expected load failure");
                try {
                    Qml.Load<AutoLoadArgs>(null);
                    return (int)Result.Fail_NullRefArg;
                } catch (ArgumentNullException) {
                } catch (Exception) {
                    return (int)Result.Fail_Exception_NullRefArg;
                }

                /////////////////////////////////////////////////////////////////////////
                Reset("DuplicateKeys", false);
                Assert("false", "Expected load failure");
                try {
                    Qml.Load<ManualLoadArgs>(new()
                    {
                        InitialProperties = new()
                        {
                            ["FOO"] = 42,
                            ["Foo"] = 43,
                            ["Bar"] = new[] { "b", "a", "r" }
                        }
                    });
                    return (int)Result.Fail_DuplicateKeys;
                } catch (ArgumentException) {
                } catch (Exception) {
                    return (int)Result.Fail_Exception_DuplicateKeys;
                }

                /////////////////////////////////////////////////////////////////////////
                Reset("HiddenProperty", false);
                Assert("false", "Expected load failure");
                try {
                    Qml.Load<HiddenPropertyArgs>(new()
                    {
                        Foo = 42,
                        Bar = ["b", "a", "r"]
                    });
                    return (int)Result.Fail_HiddenProperty;
                } catch (ArgumentException) {
                } catch (Exception) {
                    return (int)Result.Fail_Exception_HiddenProperty;
                }

                /////////////////////////////////////////////////////////////////////////
                Reset("SetterOnly", false);
                Assert("bar === undefined", "'bar' not undefined");
                Qml.Load<SetterOnlyLoadArgs>(new());
                SpinWait.SpinUntil(() => QmlLoad.Loaded, 5000);
                if (!QmlLoad.Ok)
                    return (int)Result.Fail_SetterOnly;

                /////////////////////////////////////////////////////////////////////////
                Reset("EmptyModule");
                AutoLoadArgs.Module = "";
                Qml.Load<AutoLoadArgs>(new()
                {
                    Foo = 42,
                    Bar = ["b", "a", "r"]
                });
                SpinWait.SpinUntil(() => QmlLoad.Loaded, 5000);
                if (!QmlLoad.Ok)
                    return (int)Result.Fail_EmptyModule;

                /////////////////////////////////////////////////////////////////////////
                Reset("AutoNullValue", false);
                Assert("bar === null", "'bar' not null");
                Qml.Load<AutoLoadArgs>(new()
                {
                    Foo = 42,
                    Bar = null
                });
                SpinWait.SpinUntil(() => QmlLoad.Loaded, 5000);
                if (!QmlLoad.Ok)
                    return (int)Result.Fail_AutoNullValue;

                /////////////////////////////////////////////////////////////////////////
                Reset("ManualNullValue", false);
                Assert("bar === null", "'bar' not null");
                Qml.Load<ManualLoadArgs>(new()
                {
                    InitialProperties = new()
                    {
                        ["Foo"] = 42,
                        ["Bar"] = null
                    }
                });
                SpinWait.SpinUntil(() => QmlLoad.Loaded, 5000);
                if (!QmlLoad.Ok)
                    return (int)Result.Fail_ManualNullValue;

            } catch (Exception) {
                return (int)Result.Fail_Exception;
            }

            return (int)Result.Ok;
        }
    }
}
