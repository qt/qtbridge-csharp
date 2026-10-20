# Linux with .NET CLI

Use this workflow if you are evaluating Qt Bridge for C# on Linux or WSL. Linux does not bundle Qt
with the bridge package, so the build must be pointed at a compatible Qt 6 installation.

## Requirements

* Linux x64 or arm64
* .NET SDK 8+
* CMake and Ninja available on `PATH`
* A C++ toolchain such as `build-essential`
* A compatible Qt 6 installation

<p class="docs-hint">
  CMake, Ninja, and the C++ toolchain are used by the bridge build behind the scenes; you do not
  need to write CMake files or C++ code for a template project.
</p>

The selected Qt prefix must contain `lib/cmake/Qt6/Qt6Config.cmake`. The Qt online installer
installs Qt with this layout, for example in `~/Qt/6.11.0/gcc_64`. Qt from distribution packages
often keeps its CMake files elsewhere, such as `/usr/lib/x86_64-linux-gnu/cmake/Qt6`, and then
cannot be used as `QtDir`.

Every build needs `QtDir`, including the build that `dotnet run` starts. Set it as an environment
variable in your shell, for example in your shell profile, so that you do not have to pass it to
every command:

```bash
export QtDir=/path/to/qt-prefix
```

## Create a project from templates

Install the templates:

```bash
dotnet new install QtGroup.Qt.Bridge.CSharp.Templates
```

Create a project:

```bash
dotnet new qt -n MyQtApp
cd MyQtApp
```

Build and run the project with `QtDir` set:

```bash
dotnet build
dotnet run
```

If you do not set `QtDir` in the environment, pass it to both commands, because `dotnet run`
builds the project again:

```bash
dotnet build -p:QtDir=/path/to/qt-prefix
dotnet run -p:QtDir=/path/to/qt-prefix
```

For template options such as `--Framework` and `--SampleCode`, generated project structure, and QML
item templates, see [Project Templates](templates-and-examples.md).

## Add Qt Bridge for C# to an existing project

For an existing Linux project, add the package that matches your architecture:

```bash
# Linux x64
dotnet add package QtGroup.Qt.Bridge.CSharp.linux-x64 --version 0.4.*-*

# Linux arm64
dotnet add package QtGroup.Qt.Bridge.CSharp.linux-arm64 --version 0.4.*-*
```

The package is platform-specific because the bridge includes native build/runtime pieces for the
target platform. The `0.4.*-*` version range stays on the 0.4 pre-release train.

Then build and run with `QtDir` set, as described above:

```bash
dotnet build
dotnet run
```

For the full migration workflow, including QML files, startup calls, and exposing your existing
C# objects to QML, see [Adding QML to Existing C# Projects](existing-csharp-projects.md).

## Check your setup

After the first build, check that:

* The project builds without missing Qt or toolchain errors
* The application launches
* QML files are present in the project folder
* `QtDir` points to a Qt prefix containing `lib/cmake/Qt6/Qt6Config.cmake`

## Common first issues

If the project does not build or run, check these areas first:

* The .NET SDK is missing or older than .NET 8
* Required Linux build packages are missing
* `QtDir` points to the wrong directory, or is not set for `dotnet run`
* Qt cannot be found by CMake
* The selected package or runtime does not match your Linux architecture (x64 or arm64)
