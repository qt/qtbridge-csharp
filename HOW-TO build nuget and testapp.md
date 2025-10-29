# Building the NuGet Package and Test Application (Windows, .NET 8)

This guide documents the steps used to:

1) **Build a minimal Qt 6** from source (subset of modules)
2) **Build** the **Qt.DotNet** solution (adapter, generator, rules)
3) **Pack** a **NuGet** package
4) **Build/Run** the **test application** against the local package

> The paths below use `D:\work` for demonstration. Adjust as needed. All commands are meant to run from **"x64 Native Tools Command Prompt for VS 2022."**

---

## Prerequisites

- **Visual Studio 2022** (Desktop development with C++) with the **x64 Native Tools Command Prompt**
- **.NET SDK 8+** (`dotnet --version`)
- **Git**
- **CMake** & **ninja** (installed with VS; available in the native tools prompt)
- **Python** & **Perl** (required by Qt build tooling) - see Qt's system requirements
- Sufficient disk space (Qt build can require tens of GB)

> System requirements reference: https://wiki.qt.io/Building_Qt_6_from_Git#System_Requirements

---

## 1) Build Qt 6 (subset) from source

Open **x64 Native Tools Command Prompt for VS 2022** and run:

```bat
:: Choose a working directory
pushd D:\work

:: Create source/build/install folders
mkdir qt6-source
mkdir qt6-build
mkdir qt6-install

:: Clone Qt meta-repo (Qt6 uses the qt5 meta-repo name)
git clone git://code.qt.io/qt/qt5.git qt6-source

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

**Notes**
- You can add `-D CMAKE_BUILD_PARALLEL_LEVEL=N` to speed up builds (or use `cmake --build . --parallel`).
- If you run into generator issues, you can specify `-G "Ninja"` and install Ninja.
- The above turns off Qt tests/examples to keep the build lean.

---

## 2) Set Qt environment for the session

Use the **same** native tools prompt you used to build Qt:

```bat
set QtInstallRoot=D:\work\qt6-install
```

---

## 3) Build the Qt.DotNet solution

From the **same prompt**:

```bat
:: Go to your Qt.DotNet source checkout
pushd D:\work\qtdotnet

:: Restore and build (Release)
dotnet build -c Release
```

Build the test app as a smoke test:

```bat
:: Build the test app (adjust the path if different)
dotnet build -c Release tests\PackageTestApp\PackageTestApp.csproj
```

---

## 4) Pack the NuGet

From the **same prompt**:

```bat
:: Generates .nupkg into .\nuget\local
dotnet pack -c Release nuget\Qt.Bridge.DotNet.Package\Qt.Bridge.DotNet.Package.csproj -p:PackageVersion=0.1.0
```

---

## 5) Test the package locally with the sample app

From the **same prompt**:

```bat
:: Build & run the sample app
dotnet run --project tests\PackageTestApp\PackageTestApp.csproj -c Release
```

---

## 6) Publish to nuget.org (optional)

```bat
:: Push the package
set NUGET_API_KEY=***your key***
dotnet nuget push nuget\local\*.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
```

---

## Troubleshooting

- **MSVC/CMake not detected**: Make sure you're using the *x64 Native Tools* prompt.
- **Missing Python/Perl**: Install them and ensure they're on `PATH` before running `init-repository`/`configure`.
- **Long paths or spaces**: Prefer short, space-free paths like `D:\work`.
- **Rebuild from scratch**: Delete the entire content of `qt6-build` & `qt6-install` directory  and configure again.

---

## What gets packaged

The NuGet contains:

- **.NET adapter** (host Qt/QML engine from C#)
- **Generator** (discovers your types and emits interop glue)
- **Filtering rules** (Include/Ignore/Exclude attributes)
- **C++ include headers** for the native bridge
- A **minimal open-source Qt Quick runtime subset** sufficient to run QML

All parts are versioned together to ensure compatibility.

---

## Clean up

To revert environment changes, close the prompt or manually unset variables:
```bat
set QtInstallRoot=
```

That's it-after completing the steps above, you'll have:
- A locally built Qt install at `%QtInstallRoot%`
- A **Release** build of **Qt.DotNet**
- A **NuGet** package in `nuget\local`
- A test application built against that package

