<!--
// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only
-->

# Qt Bridge for C# &ndash; Pre-Release

Qt Bridge for C# is a bridge between C# and QML, designed to write application logic in C# while
using Qt Quick for the UI. The bridging mechanism is based on interoperability between C# and C++.

## Contents

Bring **QML/Qt Quick** to **C#/.NET** with a single package. This package ships:

- A custom **.NET host** that also functions as **QML engine bootstrap**.
- A **Qt/.NET adapter** module that handles interoperability between C# and QML.
- **Source code generator** that discovers your C# types and exposes them to QML.
- **Filtering rules** (`[Qt.Include]`, `[Qt.Ignore]`, `[Qt.IgnoreType]`) to control what becomes
  visible to QML.
- **C++ include headers** used by the native side of the bridge.
- On Windows packages, a **minimal, open-source Qt Quick runtime subset** sufficient to load and
  run QML.

> Target framework: **.NET 8+**. Windows packages include Qt. Linux packages require an external
  Qt installation selected with `QtDir`.

## Why use this package?

- **Ship Qt Quick UIs with C# backends.** Keep your application logic in .NET while using QML for
  fast UI iteration.
- **Zero-glue plumbing.** The generator inspects your assemblies and wires up eligible types
  automatically.
- **Precise surface control.** Attribute-based filters let you decide exactly which types/members
  are exposed to QML.
- **Windows runtime included.** Comes with the native headers and a minimal Qt Quick runtime so you
  can start quickly.

## Install

```bash
# Project folder
dotnet add package __PACKAGE_ID__ --version __PACKAGE_VERSION__
```

## Quick start

1. **Reference the package** in your .NET 8+ project.
1. **On Linux, select Qt** before building:

   ```bash
   dotnet build -p:QtDir=/path/to/qt-prefix
   ```

   The Qt prefix must contain `lib/cmake/Qt6/Qt6Config.cmake`.
1. Manage the **visibility of C# types and type members in QML**:
   - By default, the set of types and type members exposed to QML includes all C# `public` types
     defined in the project.
   - Use the `[Qt.Ignore]` attribute to remove a type or member.
   - Use the `[assembly: Qt.IgnoreType(...)]` attribute to remove one or more types, or entire type
      hierarchies by specifying `Inherited = true`.
   - Use the `[Qt.Include]` attribute to include a type or member that would otherwise be excluded.
1. **Add your QML** (for example, `Main.qml`) to the project.
1. **Build & run.** The generator creates the interop surface; the adapter boots the QML engine
    and loads your entry file.

## Platforms & requirements

- **Runtime:** .NET 8 or newer.
- **OS:** Windows x64 and Linux x64.
- **Qt:** Windows packages include Qt. Linux packages require a Qt 6 installation from the target
  system or a Qt installation compatible with that system.
- **Tooling:** `dotnet` SDK 8+, a C++ toolchain for native build steps is required.

## Future plans

- Expand interoperability between C# and Qt/QML.
   - For example, allow C# types to be used in QML as models for `TableView` and `TreeView`.
- Remove the requirement of a C++ compiler.

## Stay in touch

You can reach us on the Qt Forum, specifically in the
[Qt Bridges category](https://forum.qt.io/category/78/qt-bridges).

## License

    Copyright (C) 2026 The Qt Company Ltd.
    SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

Qt Bridge for C# is available under the Qt Commercial License
or the GNU Lesser General Public License v3.0-only (`LGPL-3.0-only`).

For commercial licensing, see:
  - https://www.qt.io/terms-conditions
  - https://www.qt.io/licensing/

For `LGPL-3.0-only`, see:
  - https://www.gnu.org/licenses/lgpl-3.0.html

This information does not replace the full license terms. Use is subject to the applicable license.

## Terms and Conditions

If you, your employer, or the legal entity you act on behalf of hold commercial license(s) with a Qt
Group entity, Qt Bridges constitutes Pre-Release Code under the Qt License/Frame Agreement governing
those licenses, and that agreement's terms and conditions relating to Pre-Release Code apply to your
use of Qt Bridges as found in this repo.
This Qt Bridges repo may provide links or access to third-party libraries or code (collectively
"Third-Party Software") to implement various functions. Use or distribution of Third-Party Software
is discretionary and in all respects subject to applicable license terms of applicable third-party
right holders.

### Additional Terms and Conditions

The Qt Bridge for C# is built using the .NET SDK and Runtime, which are developed and maintained by
Microsoft and .NET Foundation

.NET and C# are trademarks of Microsoft Corporation. This project is not affiliated with, or
endorsed by Microsoft.

The Qt Bridge for C# package includes the following modules in binary form, licensed under the
MIT license:
  - System.Reflection.MetadataLoadContext
  - System.CommandLine
  - System.IO.Hashing

If you contribute to the code of Qt Bridge for C#, you will additionally need the following packages
licensed under the MIT license:
  - Microsoft.NET.Test.Sdk
  - MSTest.TestFramework
  - Microsoft.CodeAnalysis.CSharp
  - coverlet.collector
