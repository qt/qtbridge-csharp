# Qt Bridge - C#

> Copyright (C) 2025 The Qt Company Ltd.
> SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only

This is a pre-release implementation of Qt Bridges for C#. By
installing this package, you agree to the terms and conditions stated in
https://www.qt.io/terms-conditions. These terms and conditions also apply to
the Qt Framework, which is used as a major dependency in this package.

The Qt Bridge for C# is built using the .NET SDK and Runtime, which are developed and maintained by Microsoft and .NET Foundation

No Microsoft code or binaries are redistributed as part of Qt Bridge for C#.
.NET and C# are trademarks of Microsoft Corporation.
This project is not affiliated with or endorsed by Microsoft.

An application built with Qt Bridge for C# will include the following packages licensed under the MIT license:
  - System.Reflection.MetadataLoadContext
  - System.CommandLine
  - System.IO.Hashing

If you contribute to the code of Qt Bridge for C# you will additionally need the following packages licensed under the MIT license:
  - Microsoft.NET.Test.Sdk
  - MSTest.TestFramework
  - Microsoft.CodeAnalysis.CSharp
  - coverlet.collector

---

Bring **QML/Qt Quick** to **C#/.NET** with a single package. This bundle ships:

- **.NET adapter** to host the Qt/QML engine from managed code.
- **Source/code generator** that discovers your C# types and exposes them to QML.
- **Filtering rules** (`[Qt.Include]`, `[Qt.Ignore]`, `[Qt.IgnoreType]`) to control what becomes visible to QML.
- **C++ include headers** used by the native side of the bridge.
- A **minimal, open-source Qt Quick runtime subset** sufficient to load and run QML (no full or commercial Qt installation).

> Target framework: **.NET 8+**. Works currently on Windows x64 only, subject to the available Qt runtime in this package.

---

## Why use this package?

- **Ship Qt Quick UIs with C# backends.** Keep your application logic in .NET while using QML for fast UI iteration.
- **Zero-glue plumbing.** The generator inspects your assemblies and wires up eligible types automatically.
- **Precise surface control.** Attribute-based filters let you decide exactly which types/members are exposed to QML.
- **Batteries included.** Comes with the native headers and a minimal Qt Quick runtime so you can start quickly.

---

## Install

```bash
# Project folder
dotnet add package __PACKAGE_ID__ --version __PACKAGE_VERSION__
```

---

## Quick start

1. **Reference the package** in your .NET 8 project.
2. **Mark types/members** you want to expose to QML using attributes:
   - `[Qt.Include]` – opt–in a type or a specific member.
   - `[Qt.Ignore]` – remove a type or member.
   - `[assembly: Qt.IgnoreType(...)]` – remove one or more types, or entire type hierarchies by rule; supports `Inherited = true`.
3. **Add your QML** (e.g., `Main.qml`) to the project.
4. **Build & run.** The generator creates the interop surface; the adapter boots the QML engine and loads your entry file.

---

## Platforms & requirements

- **Runtime:** .NET 8 or newer.
- **OS:** Windows only. Platform availability depends on the packaged Qt runtime.
- **Tooling:** `dotnet` SDK 8+, a C++ toolchain for native build steps is required.

---

## Future plans
- TODO: Fill out once we're ready to publish

---

## Versioning & support

- Semantic-ish versioning aligned with the bundled Qt/adapter compatibility.
- CI-tested on the supported runtimes/OSes; see release notes for specifics.

Issues/feedback welcome - please file an issue with your OS, .NET, and Qt details.
