# Troubleshooting

A Qt Bridge for C# application can fail silently: the window opens, but nothing responds, values
stay at their defaults, and no errors appear. This page covers how to make diagnostics visible at
runtime and build time.

## See application output on Windows

On Windows, a bridge application prints nothing by default, for two independent reasons:

* The project templates set `<OutputType>WinExe</OutputType>`. That produces a GUI-subsystem
  executable with no console attached, so `Console.Write` and `Console.Error.Write` from your C#
  code go nowhere.
* Without a console, Qt's default message handler writes to `OutputDebugString` instead of
  `stderr`. QML warnings and type errors are therefore invisible too, even if you redirect `stderr`
  to a file.

Set both Qt logging variables and redirect the standard handles for applications launched from the
current PowerShell session:

```powershell
$env:QT_FORCE_STDERR_LOGGING = '1'
$env:QT_ASSUME_STDERR_HAS_CONSOLE = '1'
Start-Process .\MyApp.exe -RedirectStandardOutput out.txt -RedirectStandardError err.txt
```

Your own `Console.Error` output and Qt's messages both land in `err.txt`.

On Linux and macOS neither problem applies: run the executable from a terminal and both streams
appear as usual.

## The application exits immediately without a window

If the process ends straight away - under a debugger, as *"The target process exited without raising
a CoreCLR started event"* - the generated native host started but could not load the .NET runtime.
Nothing managed ever ran.

Make the output visible first, using the section above. The host reports the exact reason:

| Message | Cause |
|---|---|
| `Error calling dotnet` | `dotnet` is not on `PATH` for this process |
| `No .NET runtime was found.` | no runtime installed where `dotnet --list-runtimes` reports |
| `Only prerelease .NET runtimes were found; prerelease runtimes are not supported.` | only a preview or RC runtime is installed |
| `No stable .NET runtime compatible with 8.0.0 or later was found.` | installed runtime is too old |
| `A compatible stable .NET runtime was found, but its host library (hostfxr) could not be located.` | incomplete or unusual install layout |
| `Error loading library: hostfxr` | **architecture mismatch** - see below |

Two properties of the discovery are worth knowing:

* The host locates the runtime by running `dotnet --list-runtimes` and resolving `dotnet` through
  `PATH`. Environments where a GUI process has a different `PATH` than your shell will fail here.
* `DOTNET_ROOT` is **not** consulted. Pointing it at a runtime does not help; the `dotnet` that
  `PATH` resolves to is what decides.
* In Visual Studio, check **Project Properties -> Debug -> Environment** when the launched process
  uses a custom `PATH`.

### Architecture mismatch

`Error loading library: hostfxr` means a runtime was found and its `hostfxr` exists, but could not
be loaded into the host process - almost always because the two have different architectures.

Architecture mismatches can occur on any platform. A common Windows case is an x64 bridge
application on **Windows on ARM**, including a Windows VM on Apple Silicon. Choose one architecture
and use it for the bridge, Qt, and .NET runtime:

* **ARM64 natively (recommended):** Reference `QtGroup.Qt.Bridge.CSharp.win-arm64`, use an ARM64 Qt
  installation, and set `QtDir` to its prefix. The normal ARM64 .NET runtime then supplies the
  matching `hostfxr.dll`.
* **x64 under emulation:** Reference `QtGroup.Qt.Bridge.CSharp.win-x64` and install the x64 .NET
  runtime. It is installed beside the ARM64 runtime in `C:\Program Files\dotnet\x64`; put that
  directory ahead of `C:\Program Files\dotnet` on `PATH` for the process. In Visual Studio, set it
  under **Project Properties -> Debug -> Environment**.

Do not combine the x64 bridge with the ARM64 runtime, or the ARM64 bridge with the x64 runtime.

### `hostfxr_initialize_for_dotnet_command_line` fails

If the native host reports:

```text
Error calling function: hostfxr_initialize_for_dotnet_command_line
```

then `hostfxr` was loaded successfully, but rejected the application argument it received. That
argument must be the managed application DLL, for example `MyApplication.dll`; it must not be the
native executable or its output directory.

Make the output visible first, then enable the .NET host's CoreHost tracing. The trace reports the
exact application argument and commonly includes a message such as *"Application
'.../bin/Debug/net8.0' is not a managed executable."* when a directory was passed instead of the
DLL. Run the smallest reproducer with tracing enabled:

```powershell
$env:COREHOST_TRACE = '1'
$env:COREHOST_TRACEFILE = (Join-Path $PWD 'corehost.trace')
dotnet test <project> --filter "FullyQualifiedName~<test-name>"
```

On Linux or macOS, use the equivalent environment variables before the command:

```sh
COREHOST_TRACE=1 COREHOST_TRACEFILE="$PWD/corehost.trace" \
  dotnet test <project> --filter 'FullyQualifiedName~<test-name>'
```

Set `COREHOST_TRACE=2` for more detail. Remove the variables after the investigation.

## Linux build errors with distribution Qt

These errors appear when building against Qt from Debian or Ubuntu packages. The last two occur
only when types are exported as source code, which starts a native CMake build; with
`<QtExportAs>metadata</QtExportAs>` they do not occur.

| Message | Cause and fix |
|---|---|
| `Qt Bridge for C# found Qt6Config.cmake in '...', but could not derive the Qt installation prefix from it` | `QtDir` names a Qt CMake package directory in a layout other than `lib/cmake/Qt6`, `lib64/cmake/Qt6`, or `lib/<multiarch triplet>/cmake/Qt6`. Set `QtDir` to the Qt prefix, such as `/usr`. |
| `Imported target "Qt6::CorePrivate" includes non-existent path "/usr/include/<triplet>/qt6/QtCore/6.x.y"` | Qt private headers are missing: `sudo apt install qt6-base-private-dev`. If another `*Private` module is reported next, install its package, such as `qt6-declarative-private-dev`. |
| `file RPATH_SET could not write new RPATH ... No valid ELF RPATH or RUNPATH entry exists in the file` | Distribution Qt binaries have no RPATH, and CMake can only replace an existing one. Install `patchelf`: `sudo apt install patchelf`. |

If `QtDir` still has an old value after you change it in the project, check for a `-p:QtDir=...`
on the command line, which overrides the project. If the project sets `QtDir` only when it is empty
(`Condition="'$(QtDir)' == ''"`), an exported environment variable wins; check `echo $QtDir`.

During the native build, CMake also prints many warnings of the form *"The qml plugin '...' is a
dependency of '...', but the link target ... does not exist"*. With a shared Qt such as the
distribution packages they are harmless: the plugins are loaded at run time instead of being
linked.

## The native build fails after switching to a different Qt

The native CMake build keeps its configuration, including the Qt it found, in a CMake cache under
`obj/<configuration>/<framework>/qt/native/build`. When `QtDir` changes, the build resets that
cache automatically and reports *"Qt Bridge: Qt installation changed, resetting the CMake cache."*

If the native build still fails with CMake errors and *"Configuring incomplete, errors occurred!"*
after a Qt change, for example because the Qt installation was replaced in place under the same
path, run `dotnet clean` once, then build again:

```bash
dotnet clean
dotnet build
```

## Smart App Control blocks a build

If `dotnet build` reports *"An Application Control policy has blocked this file"* followed by
*"Error generating native code!"*, Windows Smart App Control, a Windows 11 feature, may have
blocked an unsigned or unrecognized bridge build-time assembly. The bridge code generator loads
these assemblies while it generates native code, so the blocked file can be `qbgen.dll`,
`Qt.DotNet.Adapter.dll`, or a `Qt.Bridge.Utils.*.dll`.

Open **Windows Security -> App & browser control -> Smart App Control**. If it is **On**, Smart App
Control is enforcing its policy and may be the cause. **Evaluation** only observes activity; it
does not block files.

To build local unsigned binaries, turn Smart App Control off from that page. This reduces the
protection Windows applies to unrecognized code. It cannot simply be turned back on later; doing so
requires resetting or cleanly reinstalling Windows.

## Trace QML type and import resolution

When a QML type does not resolve, or a module loads from an unexpected place, turn on the import
category:

```powershell
$env:QT_LOGGING_RULES = 'qt.qml.import.debug=true'
```

The output names every import path that was added and every `qmldir` that was loaded, with its full
path. That is the quickest way to tell whether a module came from the resource system (`qrc:/…`) or
from the loose copy next to the executable.

Other categories worth knowing:

| Rule | Shows |
|---|---|
| `qt.qml.import.debug=true` | import paths, `qmldir` resolution, plugin loading |
| `qt.qml.typeresolution.cycle=true` | possible cyclic dependencies between QML files |
| `qt.qml.binding.removal=true` | bindings overwritten by an imperative assignment |

Set several at once by separating them with `;`.

## Check what QML actually sees of a C# type

A successful build does not prove that a member reached QML. The C# compiler has no knowledge of
your QML, so a member that is no longer exported still compiles, and the failure appears only at
runtime as:

```
TypeError: Property 'myMethod' of object MyModule/MyType is not a function
```

When source-code generation is enabled, read the generated bridge source for the type:

```
obj/<configuration>/<framework>/qt/native/source/cpp/<module>/<type>.cpp
```

Search it for the camelCase name QML uses. If the name is absent, QML cannot see the member.

Metadata mode records type information in JSON rather than generating a separate C++ wrapper for
each type. Inspect `obj/<configuration>/<framework>/qt/native/qt_bridge_metadata.json` instead.
That is the default name; if the project sets `QtBridgeMetadataFileName`, inspect the file using
that configured name.

Members disappear from either generated output when they stop being instance members of the exported
type - for example when a method is made `static` or its visibility is reduced below `public`.
Analyzer suggestions such as **CA1822** ("member does not access instance data, can be marked
static") fire readily on pure mapping functions, which are exactly the kind of method QML tends to
call. Applying such a suggestion to a QML-visible member removes it from the bridge while leaving
the build successful.

Before changing the visibility or staticness of a member on a `[QmlElement]` type, search your QML
for its camelCase name, and check the generated file afterwards.
