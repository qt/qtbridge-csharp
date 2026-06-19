<!--
// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only
-->

# Qt Bridge for C# &ndash; Pre-Release

Qt Bridge for C# is a bridge between C# and QML, designed to write application logic in C# while
using Qt Quick for the UI. The bridging mechanism is based on interoperability between C# and C++.

## Contents

1. [Introduction](#introduction)
1. [Supported platforms](#supported-platforms)
1. [Requirements](#requirements)
1. [Installing Qt Bridge](#installing-qt-bridge)
    1. [Importing Qt Bridge as a package reference](#importing-qt-bridge-as-a-package-reference)
    1. [Using the Visual Studio extension](#using-the-visual-studio-extension)
    1. [Importing Qt Bridge as a local package reference](#importing-qt-bridge-as-a-local-package-reference)
        1. [Using an existing Qt installation](#using-an-existing-qt-installation)
        1. [Building Qt 6 from source on Windows](#building-qt-6-from-source-on-windows)
        1. [Building Qt 6 from source on Linux (Ubuntu / WSL)](#building-qt-6-from-source-on-linux-ubuntu--wsl)
1. [Running examples](#running-examples)
1. [Using dotnet CLI templates](#using-dotnet-cli-templates)
1. [Using resources](#using-resources)
1. [Troubleshooting](#troubleshooting)
1. [What gets packaged](#what-gets-packaged)
1. [Clean up](#clean-up)
1. [Stay in touch](#stay-in-touch)

## Introduction

Qt Bridge for C# is intended for C# developers who want to experiment with Qt and/or QML
without committing to a full C++ application. The repository includes example applications
and dotnet CLI templates that demonstrate the recommended project structure, how to model data
and logic in C#, and how to connect those models to QML views.

Detailed documentation can be found
[here](https://doc-snapshots.qt.io/qtbridges-dev/qtbridges-csharp-index.html).

## Supported platforms

The currently supported workflow is:

- **Windows x64 with .NET 8+** using the bundled Qt runtime or an external Qt installation.
- **Linux x64 with .NET 8+** using an external Qt installation from or compatible with the target
  Linux distribution.

## Requirements

- Windows 11 (`x64`) or Ubuntu/WSL (`x64`)
- **.NET SDK 8+** (`dotnet --version`)
- **Git**
- **CMake** & **Ninja**
- A C++ toolchain:
  - Windows: **Visual Studio 2022** (Desktop development with C++) and
    **x64 Native Tools Command Prompt**
  - Ubuntu/WSL: `build-essential` (or equivalent)
- **Python** & **Perl** (required only if you build Qt from source); see Qt's system requirements
- Sufficient disk space (Qt build can require tens of GB)

> System requirements reference: https://wiki.qt.io/Building_Qt_6_from_Git#System_Requirements

## Installing Qt Bridge

Qt Bridge is distributed as a NuGet package. You can add it to your project directly with a
package reference, use the Visual Studio extension to create a preconfigured project, or build and
consume local packages from this repository.

### Importing Qt Bridge as a package reference

#### Add via Visual Studio

1. In Visual Studio, select *Project* | *Manage NuGet Packages...*.
1. Select the *Browse* tab and enter *QtGroup.Qt.Bridge.CSharp* in the search field.
1. Select the latest version from the *Version* dropdown and click *Install*.

#### Add via dotnet CLI

Choose the package that matches your RID (runtime identifier). The Windows package includes a
minimal Qt runtime. The Linux package does not include Qt; set `QtDir` to a Qt 6 installation
prefix before building.

```bash
# Windows x64
dotnet add package QtGroup.Qt.Bridge.CSharp.win-x64 --version 0.3.*-*

# Linux x64 (Ubuntu / WSL)
dotnet add package QtGroup.Qt.Bridge.CSharp.linux-x64 --version 0.3.*-*
```

Linux example:

```bash
dotnet build -p:QtDir=/usr/lib/qt6
```

The selected Qt prefix must contain `lib/cmake/Qt6/Qt6Config.cmake`.

### Using the Visual Studio extension

The Qt Bridge for C# Visual Studio extension is the recommended entry point for Visual Studio
users. Installing the VSIX adds Qt Bridge project and item templates to Visual Studio, so you can
create a Qt Bridge application without installing the dotnet templates separately.

The extension packages the Qt Bridge template package inside the VSIX. Projects created from the
extension template include the appropriate `PackageReference` to the Qt Bridge for C# NuGet package,
which gives you another way to get started with the bridge package besides adding the package
reference manually.

The extension also activates QML Language Server support for Qt Bridge projects. After a project is
built, Visual Studio can use the generated Qt Bridge metadata to provide QML language features for
the QML files in the project.

To use the extension:

1. Install the `Qt.Bridge.CSharp.vsix` package.
1. Restart Visual Studio if prompted.
1. Create a new project using the Qt Bridge for C# project template, or add a QML file using the
   Qt Bridge item template.
1. Build the project so the Qt Bridge package can restore and generate the metadata used by the
   QML Language Server integration.

The extension is Windows/Visual Studio-only. The dotnet CLI template workflow remains available for
CLI users and for Linux/WSL development.

### Importing Qt Bridge as a local package reference

#### Using an existing Qt installation

If you already have a Qt 6 installation that includes `qtbase`, `qtsvg`, `qtshadertools`,
`qtdeclarative`, `qtquick3d`, `qtquick3dphysics`, and `qtquicktimeline`, you can skip building Qt
from source. Set `QtDir` to the Qt installation prefix (the folder that contains `bin`, `lib`, and
`include`), then continue with **Build the Qt Bridge for C#**.

Windows (`cmd` / Native Tools Prompt):
```bat
set QtDir=D:\Qt\6.11.0\msvc2022_64
```

Linux / WSL (`bash`):
```bash
export QtDir=~/work/qt6-install
```

If you already use the Qt `QTDIR` environment variable, MSBuild will also honor it because
environment variables are available as build properties.

#### Building Qt 6 from source on Windows

> The paths below use `D:\work` for demonstration. Adjust as needed. All commands are meant to run
from the **x64 Native Tools Command Prompt for VS 2022**.

```bat
:: Choose a working directory
set WORKDIR=D:\work
pushd %WORKDIR%

:: Create Qt source/build/install folders
mkdir qt6-source
mkdir qt6-build
mkdir qt6-install

:: Clone Qt meta-repo (Qt 6 uses the qt5 meta-repo name)
git clone https://code.qt.io/qt/qt5.git qt6-source

:: Initialize only the modules we need
cd qt6-source
init-repository --module-subset=qtbase,qtsvg,qtshadertools,qtdeclarative,qtquick3d,qtquick3dphysics,qtquicktimeline

:: Configure out-of-source build
cd ..\qt6-build
..\qt6-source\configure -prefix ..\qt6-install -release -opensource -confirm-license -submodules qtbase,qtsvg,qtshadertools,qtdeclarative,qtquick3d,qtquick3dphysics,qtquicktimeline -- -DQT_BUILD_TESTS=OFF -DQT_BUILD_EXAMPLES=OFF

:: Build and install
cmake --build .
cmake --install .
```

> You can add `-DCMAKE_BUILD_PARALLEL_LEVEL=N` to speed up builds
> (or use `cmake --build . --parallel`). If you run into generator issues, you can specify
> `-G "Ninja"` and install Ninja. The above turns off Qt tests/examples to keep the build lean.

#### Building Qt 6 from source on Linux (Ubuntu / WSL)

If you build on Ubuntu / WSL, install the .NET SDK first:

```bash
sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
```

1. **Install required build dependencies**

    ```bash
    sudo apt update
    sudo apt install -y \
      cmake ninja-build build-essential python3 pkg-config \
      libegl-dev libgl-dev libglu1-mesa-dev mesa-common-dev \
      libopengl-dev libglx-dev \
      libx11-dev libx11-xcb-dev libxext-dev libxrender-dev libxi-dev \
      libxcb1-dev libxcb-cursor-dev libxcb-glx0-dev libxcb-keysyms1-dev \
      libxcb-image0-dev libxcb-shm0-dev libxcb-icccm4-dev libxcb-sync-dev \
      libxcb-xfixes0-dev libxcb-shape0-dev libxcb-randr0-dev \
      libxcb-render-util0-dev libxcb-util-dev libxcb-xkb-dev \
      libxkbcommon-dev libxkbcommon-x11-dev
    ```

2. **Clone and initialize the required Qt modules**

    ```bash
    mkdir -p ~/work
    cd ~/work

    git clone https://code.qt.io/qt/qt5.git qt6-source
    cd qt6-source
    ./init-repository --module-subset=qtbase,qtsvg,qtshadertools,qtdeclarative,qtquick3d,qtquick3dphysics,qtquicktimeline
    ```

3. **Configure an out-of-source build**

    ```bash
    cd ~/work
    mkdir -p qt6-build qt6-install
    cd qt6-build

    ../qt6-source/configure \
      -prefix ../qt6-install \
      -release \
      -opensource \
      -confirm-license \
      -submodules qtbase,qtsvg,qtshadertools,qtdeclarative,qtquick3d,qtquick3dphysics,qtquicktimeline \
      -- -DQT_BUILD_TESTS=OFF -DQT_BUILD_EXAMPLES=OFF
    ```

4. **Build and install**

    ```bash
    cmake --build . --parallel
    cmake --install .
    ```

The resulting Qt installation is placed in `~/work/qt6-install` and contains the usual `bin`,
`lib`, `include`, `plugins`, and `qml` directories.

> If `configure` fails with an OpenGL-related error, make sure the OpenGL and X11 / XCB
> development packages listed above are installed, then remove `CMakeCache.txt` and `CMakeFiles/`
> and run `configure` again.

#### Build Qt Bridge for C#

After Qt is available, run from this repository root:

```bash
dotnet build -c Release
```

On Windows, setting `QtInstallRoot` before the build lets the local `win-x64` package include the
Qt payload.

On Linux, the local `linux-x64` package does not include Qt by default, even when `QtInstallRoot`
points to a valid Qt installation. Projects that consume the package must select a compatible
system Qt by setting `QtDir`.

To build a Linux package with a bundled Qt payload for local testing, opt in explicitly:

```bash
dotnet build -c Release \
  -p:QtBridgePackBundledQt=true \
  -p:QtInstallRoot=/path/to/qt
```

#### Add the local NuGet package source

- In Visual Studio, select *Tools* | *Options* | *NuGet Package Manager* | *Package Sources*.
- Add a source named, for example, *Local Package Source* with path `<repo-root>/nuget/local`.

CLI alternative:

```bash
dotnet nuget add source ./nuget/local --name QtBridgeLocal
```

At this point, the Qt Bridge and Qt packages are ready, and any projects referencing Qt Bridge for
C# can be built successfully.

## Running examples

The **examples** directory contains simple projects implemented with Qt Bridge for C#. For instance,
to build and run the *Primes* test application:

```bash
# Build and run the test app (adjust the path if different)
dotnet build -c Release examples/Primes/Primes.csproj
dotnet run --project examples/Primes/Primes.csproj -c Release
```

### Linux / WSL runtime notes (Qt platform plugins)

- `wayland` is optional. If your Qt build/package does not include the Wayland platform plugin,
  startup can still work with `xcb`.
- On Ubuntu/WSL, install runtime dependency:

```bash
sudo apt update
sudo apt install -y libxcb-cursor0
```

- In WSL2, make sure GUI forwarding is available (WSLg / X server), then force `xcb`:

```bash
QT_QPA_PLATFORM=xcb ./examples/Primes/bin/Release/net8.0/Primes
```

- If startup reports `QFontDatabase: Cannot find font directory .../lib/fonts`, the selected Qt
  installation does not provide bundled fonts for that system. Point Qt to a system font directory
  before launching the app, for example:

```bash
export QT_QPA_FONTDIR=/usr/share/fonts/truetype/ubuntu
QT_QPA_PLATFORM=xcb ./examples/Primes/bin/Release/net8.0/Primes
```

- For headless validation only (no GUI):

```bash
QT_QPA_PLATFORM=offscreen ./examples/Primes/bin/Release/net8.0/Primes
```

## Using dotnet CLI templates

These templates are installed and used via the dotnet CLI (the dotnet command-line tool).

### Install the templates

If you installed the Visual Studio extension, the Qt Bridge templates are already available in
Visual Studio. Install the dotnet templates separately only when you want to create Qt Bridge
projects or QML files from the `dotnet` CLI.

From a **NuGet feed**:
```bash
dotnet new install QtGroup.Qt.Bridge.CSharp.Templates
```

From a **local .nupkg**:
```bash
dotnet new install ./nuget/local/QtGroup.Qt.Bridge.CSharp.Templates/<version>/QtGroup.Qt.Bridge.CSharp.Templates.<version>.nupkg
```

Verify installation:
```bash
dotnet new list
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

This generates:
```
MyQtApp/
  Project.csproj
  Program.cs
  Main.qml
```

### Add a QML item to an existing project

```bash
 dotnet new qml -n MainPage
```

This creates `MainPage.qml`. The build integrates QML files automatically (they are registered and
copied alongside your app).

### Uninstall the templates

```bash
dotnet new uninstall QtGroup.Qt.Bridge.CSharp.Templates
```

## Using resources

Qt Bridge for C# packages app resources into the Qt Resource System. QML uses `qrc:/` URLs
directly, and C# uses `Qt.Resources` when it needs to read the same packaged files.

See [Resources in Qt Bridge for C# apps](HOW-TO%20resources.md) for the resource authoring model,
`.resx` integration, access modes, aliases, and cross-project resource usage.

## Troubleshooting

- **C++ toolchain not detected**:
  - Windows: use the *x64 Native Tools* prompt.
  - Ubuntu/WSL: ensure `build-essential`, `cmake`, and `ninja-build` are installed.
- **Missing Python/Perl**: Install them and ensure they are on `PATH` before running
  `init-repository`/`configure`.
- **Rebuild Qt from scratch**: Delete `qt6-build` and `qt6-install`, then run `configure` again.
- **Linux/WSL runtime plugin errors**:
  - Install runtime dependency: `sudo apt install -y libxcb-cursor0`
  - Force `xcb`: `QT_QPA_PLATFORM=xcb ./examples/Primes/bin/Release/net8.0/Primes`
  - Headless startup check: `QT_QPA_PLATFORM=offscreen ./examples/Primes/bin/Release/net8.0/Primes`
- **Tests fail because the temp path contains spaces**:
  - The test harness requires a temp root without spaces.
  - Set `QTBRIDGE_TEST_ROOT` to a writable directory without spaces before running tests.
  - Windows: `set QTBRIDGE_TEST_ROOT=C:\temp`
  - Ubuntu / WSL: `export QTBRIDGE_TEST_ROOT=/tmp`
- **WSL GUI**: Make sure GUI forwarding is available (WSLg or X server).

## What gets packaged

The NuGet contains:

- **.NET adapter** (host Qt/QML engine from C#)
- **Generator** (discovers your types and emits interop glue)
- **Filtering rules** (Include/Ignore/Exclude attributes)
- **C++ include headers** for the native bridge
- On Windows packages, a **minimal open-source Qt Quick runtime subset** sufficient to run QML

Linux packages do not contain Qt. They use the Qt installation selected with `QtDir`.

## Clean up

To revert environment changes:

Windows:

```bat
set QtInstallRoot=
set QtDir=
```

Ubuntu / WSL:

```bash
unset QtInstallRoot
unset QtDir
```

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
