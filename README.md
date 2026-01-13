# Qt Bridge - C#

> Copyright (C) 2025 The Qt Company Ltd.
> SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only

This is a pre-release implementation of Qt Bridge for C#.
By installing this package, you agree to the terms and conditions stated at https://www.qt.io/terms-conditions.
These terms and conditions also apply to the Qt Framework, which is used as a major dependency in this package.

The Qt Bridge for C# is built using the .NET SDK and Runtime, which are developed and maintained by Microsoft and the .NET Foundation.

No Microsoft code or binaries are redistributed as part of Qt Bridge for C#.
.NET and C# are trademarks of Microsoft Corporation.
This project is not affiliated with or endorsed by Microsoft.

An application built with Qt Bridge for C# includes the following packages licensed under the MIT License:
  - System.Reflection.MetadataLoadContext
  - System.CommandLine
  - System.IO.Hashing

If you contribute to the Qt Bridge for C# codebase, you will also need the following packages licensed under the MIT License:
  - Microsoft.NET.Test.Sdk
  - MSTest.TestFramework
  - Microsoft.CodeAnalysis.CSharp
  - coverlet.collector

## Contents

1. [Introduction](#introduction)
2. [Supported platforms](#supported-platforms)
3. [Requirements](#requirements)
4. [Installing Qt Bridge](#installing-qt-bridge)
    1. [Importing Qt Bridge as a package reference](#importing-qt-bridge-as-a-package-reference)
    2. [Importing Qt Bridge as a local package reference](#importing-qt-bridge-as-a-local-package-reference)
        1. [Building Qt 6 from source](#building-qt-6-from-source)
        2. [Using an existing Qt installation](#using-an-existing-qt-installation)
5. [Running examples](#running-examples)
6. [Using dotnet CLI templates](#using-dotnet-cli-templates)
7. [Stay in touch](#stay-in-touch)

## Introduction

Qt Bridge for C# is a bridge between C# and QML, designed to write application logic in
C# while using Qt Quick for the UI. The bridging mechanism is based on interoperability between C# and C++.

Qt Bridge for C# is intended for C# developers who want to experiment with Qt and/or QML
without committing to a full C++ application. The repository includes example applications
and dotnet CLI templates that demonstrate the recommended project structure, how to model data
and logic in C#, and how to connect those models to QML views.

Detailed documentation can be found [here](https://doc-snapshots.qt.io/qtbridges-dev/qtbridges-csharp-index.html).

## Supported platforms

Currently, only **Windows x64 and .NET 8+** are supported, with plans to extend support in the future.

## Requirements

- **Visual Studio 2022** (Desktop development with C++) with the **x64 Native Tools Command Prompt** (required because Qt Bridge generates C++ code)
- **.NET SDK 8+** (`dotnet --version`)
- **Git**
- **CMake** & **Ninja** (installed with VS; available in the native tools prompt)
- **Python** & **Perl** (required only if you build Qt from source); see Qt's system requirements
- Sufficient disk space (Qt build can require tens of GB)

> System requirements reference: https://wiki.qt.io/Building_Qt_6_from_Git#System_Requirements

## Installing Qt Bridge

Qt Bridge is distributed as a NuGet package. You can add it to your project as a package dependency using a package reference.

### Importing Qt Bridge as a package reference

#### Add via Visual Studio

1. In Visual Studio, select *Project* | *Manage NuGet Packages...*.
2. Select the *Browse* tab and enter *QtGroup.Qt.Bridge.CSharp* in the search field.
3. Select the latest version from the *Version* dropdown and click *Install*.

#### Add via dotnet CLI

```bat
# Project folder
dotnet add package QtGroup.Qt.Bridge.CSharp.win-x64 --version 0.1.0-alpha
```

### Importing Qt Bridge as a local package reference

#### Using an existing Qt installation

If you already have a Qt 6 installation that includes `qtbase`, `qtsvg`, `qtshadertools`, `qtdeclarative`, `qtquick3d`,
`qtquick3dphysics`, and `qtquicktimeline`, you can skip building Qt from source. In the **x64 Native Tools Command Prompt
for VS 2022**, set `QtInstallRoot` to the Qt installation prefix (the folder that contains `bin`, `lib`, and `include`),
then continue with **Build the Qt Bridge for C#**.

```bat
set QtInstallRoot=D:\Qt\6.6.0\msvc2019_64
```

#### Building Qt 6 from source

> The paths below use `D:\work` for demonstration. Adjust as needed. All commands are meant to run from the **x64 Native Tools Command Prompt for VS 2022**.

1. **Build Qt 6 (subset) from source**

    Open **x64 Native Tools Command Prompt for VS 2022** and run:

    ```bat
    # Choose a working directory
    pushd D:\work

    # Clone the Qt Bridge for C# repository
    git clone https://codereview.qt-project.org/qt/qtbridge-csharp

    # Create Qt source/build/install folders
    mkdir qt6-source
    mkdir qt6-build
    mkdir qt6-install

    # Clone Qt meta-repo (Qt 6 uses the qt5 meta-repo name)
    git clone https://code.qt.io/qt/qt5.git qt6-source

    # Initialize only the modules we need
    cd qt6-source
    init-repository --module-subset=qtbase,qtsvg,qtshadertools,qtdeclarative,qtquick3d,qtquick3dphysics,qtquicktimeline

    # Configure out-of-source build
    cd ..\qt6-build
    ..\qt6-source\configure -prefix ..\qt6-install -release -opensource -confirm-license -submodules qtbase,qtsvg,qtshadertools,qtdeclarative,qtquick3d,qtquick3dphysics,qtquicktimeline -- -DQT_BUILD_TESTS=OFF -DQT_BUILD_EXAMPLES=OFF

    # Build and install
    cmake --build .
    cmake --install .
    ```

    > You can add `-DCMAKE_BUILD_PARALLEL_LEVEL=N` to speed up builds (or use `cmake --build . --parallel`).
    > If you run into generator issues, you can specify `-G "Ninja"` and install Ninja.
    > The above turns off Qt tests/examples to keep the build lean.

2. **Set the Qt environment for the session**

    Use the **same** native tools prompt you used to build Qt:

    ```bat
    set QtInstallRoot=D:\work\qt6-install
    ```

    If you used an existing Qt installation, set `QtInstallRoot` to that path instead.

3. **Build the Qt Bridge for C#**

    From the **same prompt**:

    ```bat
    # Go to your Qt Bridge for C# source checkout
    pushd D:\work\qtbridge-csharp

    # Restore and build (Release)
    dotnet build -c Release
    ```

4. **Add the local NuGet Package Sources directory to Visual Studio**
    - In Visual Studio, select *Tools* | *Options*.
    - In the Options dialog, select *NuGet Package Manager*.
    - In the NuGet Package Manager settings, select *Package Sources*.
    - In *Package Sources*, add a new item, give it a *Name* like *Local Package Source*, and set the *Source* path to *D:\work\qtbridge-csharp\nuget\local*.

At this point, the Qt Bridge and Qt packages are ready, and any projects referencing Qt Bridge for C# can be built successfully.

## Running examples

The **examples** directory contains simple projects implemented with Qt Bridge for C#. For instance, to build and run the *Primes* test application:

```bat
# Build the test app (adjust the path if different)
dotnet build -c Release examples\Primes\Primes.csproj
```

## Using dotnet CLI templates

These templates are installed and used via the dotnet CLI (the dotnet command-line tool).

### Install the templates

From a **NuGet feed**:
```bat
dotnet new install QtGroup.Qt.Bridge.CSharp.Templates
```

From a **local .nupkg**:
```bat
dotnet new install D:\work\qtbridge-csharp\nuget\local\QtGroup.Qt.Bridge.CSharp.Templates\0.1.0-alpha\QtGroup.Qt.Bridge.CSharp.Templates.0.1.0-alpha.nupkg
```

Verify installation:
```bat
dotnet new list
```

### Create a project

```bat
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

```bat
dotnet new qml --FileName=MainPage
```

This creates `MainPage.qml`. The build integrates QML files automatically (they are registered and copied alongside your app).

### Uninstall the templates

```bat
dotnet new uninstall QtGroup.Qt.Bridge.CSharp.Templates
```

## Stay in touch

You can reach us on the Qt Forum, specifically in the [Qt Bridges category](https://forum.qt.io/category/78/qt-bridges).
