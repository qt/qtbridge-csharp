/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;

using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_StructuredData
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Qml.WaitForExit();
            return 0;
        }
    }

    public sealed class Person
    {
        public int Age { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class Team
    {
        public string Name { get; set; } = "";
        public List<Person> Members { get; set; } = new List<Person>();
    }

    public class StructuredData
    {
        public Person LastPerson { get; private set; } =
            new() { Name = "Alice", Age = 42 };

        public Team LastTeam { get; private set; } =
            new()
            {
                Name = "Awesome Team",
                Members = new List<Person>
                {
                    new Person { Name = "Alice", Age = 42 },
                    new Person { Name = "Bob", Age = 21 },
                }
            };

        // Host -> QML
        public Person CreateSamplePerson() =>
            new() { Name = "Alice", Age = 42 };

        public Team CreateSampleTeam()
        {
            var t = new Team
            {
                Name = "Awesome Team",
                Members = new List<Person>
                {
                    new Person { Name = "Alice", Age = 42 },
                    new Person { Name = "Bob", Age = 21 }
                }
            };
            return t;
        }

        // QML -> Host
        public void AcceptTeam(Team team) => LastTeam = team;
        public void AcceptPerson(Person person) => LastPerson = person;
    }
}

