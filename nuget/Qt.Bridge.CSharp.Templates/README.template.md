# Qt Bridge - C# Templates - Pre Release

> Copyright (C) 2026 The Qt Company Ltd.
> SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only

## Contents

### **Item Template: `Qt.Bridge.CSharp QML File (.qml)`**
A template for adding **new QML files** to your project, with:
- Automatic build system integration of newly created `.qml` files
- Streamlined workflow for extending QML-based UIs

---

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

This creates `MainPage.qml`. The build integrates QML files automatically (they'll be registered and copied alongside your app).

### Uninstall the templates

```bash
dotnet new uninstall __PACKAGE_ID__
```

---

## Platforms & requirements

- **Runtime:** .NET 8 or newer.
- **OS:** Windows only. Platform availability depends on the packaged Qt runtime.
- **Tooling:** `dotnet` SDK 8+, a C++ toolchain for native build steps is required.

---

## Stay in touch

You can reach us on the Qt Forum, specifically in the [Qt Bridges category](https://forum.qt.io/category/78/qt-bridges).

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

No Microsoft code or binaries are redistributed as part of Qt Bridge for C#. .NET and C# are
trademarks of Microsoft Corporation. This project is not affiliated with, or endorsed by Microsoft.

An application built with Qt Bridge for C# will include the following packages licensed under the
MIT license:
  - System.Reflection.MetadataLoadContext
  - System.CommandLine
  - System.IO.Hashing

If you contribute to the code of Qt Bridge for C# you will additionally need the following packages
licensed under the MIT license:
  - Microsoft.NET.Test.Sdk
  - MSTest.TestFramework
  - Microsoft.CodeAnalysis.CSharp
  - coverlet.collector
