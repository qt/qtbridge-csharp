// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_NestedTypes
    {
        private const string Source = """
            using System;
            using System.Collections.Generic;

            namespace MyApp
            {
                public class Outer
                {
                    // nested EventArgs
                    public class ChangedEventArgs : EventArgs
                    {
                        public string Name { get; }
                        public ChangedEventArgs(string name) => Name = name;
                    }

                    // referenced only via EventHandler<T>
                    public event EventHandler<ChangedEventArgs> Changed;

                    public void Fire(string name)
                        => Changed?.Invoke(this, new ChangedEventArgs(name));

                    // put it on the surface to force codegen to need it
                    public ChangedEventArgs Latest() => new ChangedEventArgs("last");
                }

                public class Player
                {
                    // nested enum used on public surface
                    public enum State { Idle, Running, Paused }
                    public State Current { get; set; } = State.Idle;
                    public List<State> History { get; } = new();
                    public State Echo(State s) => s;
                }
            }
            """;

        [TestMethod]
        public async Task NestedTypes()
        {
            var result = await TestCodeGenerator.GenerateAsync([Source]);

            Assert.IsTrue(result.Sink.Files.TryGetValue(
                "source/hpp/myapp/outer_changedeventargs.h", out var hpp));
            Assert.MatchesRegex(value: hpp, pattern:
                @"Outer_ChangedEventArgs\s*,\s*""MyApp.Outer\+ChangedEventArgs");
        }
    }
}
