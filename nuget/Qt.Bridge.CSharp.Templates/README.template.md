<!--
// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only
-->

# Qt Bridge for C# Templates &ndash; Pre-Release

Qt Bridge for C# is a bridge between C# and QML, designed to write application logic in C# while
using Qt Quick for the UI. The bridging mechanism is based on interoperability between C# and C++.

## Contents

### **Project Template: `Qt.Bridge.CSharp Project`**
A template for creating a new **C# and QML application** project, with:
- QML file `Main.qml` with a simple application UI.
- C# module `Program.cs` with a `Main()` function that loads the QML UI.

### **Item Template: `Qt.Bridge.CSharp QML File (.qml)`**
A template for adding **new QML files** to your project, with:
- Automatic build system integration of newly created `.qml` files
- Streamlined workflow for extending QML-based UIs

## How to use

### Install the templates

From a **NuGet feed**:
```bash
dotnet new install __PACKAGE_ID__
```

From a **local .nupkg**:
```bash
dotnet new install ./PATH/TO/__PACKAGE_ID__.__PACKAGE_VERSION__.nupkg
```

Verify installation:
```bash
dotnet new list
```

Update to the latest version:
```bash
dotnet new update
# or force a specific package/version
dotnet new install __PACKAGE_ID__ --force
```

### Create a project

```bash
dotnet new qt -n MyQtApp
cd MyQtApp
dotnet build
dotnet run
```

The project template defaults to `net8.0`. To target a newer framework supported by your installed
.NET SDK, pass `--Framework`, for example:

```bash
dotnet new qt -n MyQtApp --Framework net9.0
```

To include a small C# and QML counter sample in the generated project, pass `--SampleCode`:

```bash
dotnet new qt -n MyQtApp --SampleCode
```

On Linux, Qt is not bundled with the bridge package. Install Qt 6 for your target system and set
one of the supported Qt prefix selectors before building:

```bash
dotnet build -p:QtDir=/path/to/qt-prefix
```

The Qt prefix must contain `lib/cmake/Qt6/Qt6Config.cmake`.

This generates:
```
MyQtApp/
  Project.csproj
  Program.cs
  Main.qml
```

### Add a QML item to an existing project

```bash
dotnet new qml --FileName=MainPage
```

This creates `MainPage.qml`. The build integrates QML files automatically (they'll be registered
and copied alongside your app).

### Uninstall the templates

```bash
dotnet new uninstall __PACKAGE_ID__
```

## Platforms & requirements

- **Runtime:** .NET 8 or newer.
- **OS:** Windows and Linux only.
- **Qt:** Windows packages include Qt. Linux packages require an external Qt 6 installation selected
  with `QtDir`.
- **Tooling:** `dotnet` SDK 8+, a C++ toolchain for native build steps is required.

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
