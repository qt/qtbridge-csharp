# Debugging hostfxr startup failures

> Copyright (C) 2026 The Qt Company Ltd.
> SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only

Use CoreHost tracing when a native Qt Bridge host reports a failure calling
`hostfxr_initialize_for_dotnet_command_line`, or when a managed application
does not start.

## Enable tracing

Set `COREHOST_TRACE` and `COREHOST_TRACEFILE` before running the smallest
reproducer. A focused test is usually preferable to the full suite.

### PowerShell (Windows, Linux, or macOS with PowerShell)

```powershell
$env:COREHOST_TRACE = '1'
$env:COREHOST_TRACEFILE = (Join-Path $PWD 'corehost.trace')

dotnet test tests/Test_Qt.Bridge.Project/Test_Qt.Bridge.Project.csproj `
  --no-build --no-restore `
  --filter "FullyQualifiedName~QtQuickTest" `
  --logger "console;verbosity=normal"
```

### Bash or Zsh (Linux or macOS)

```sh
export COREHOST_TRACE=1
export COREHOST_TRACEFILE="$PWD/corehost.trace"

dotnet test tests/Test_Qt.Bridge.Project/Test_Qt.Bridge.Project.csproj \
  --no-build --no-restore \
  --filter 'FullyQualifiedName~QtQuickTest' \
  --logger 'console;verbosity=normal'
```

Use `COREHOST_TRACE=2` when the normal trace is not sufficiently detailed.

## What to inspect

Open `corehost.trace` in an editor and search for these terms:

- `hostfxr_initialize_for_dotnet_command_line`
- `Application`
- `managed executable`
- `error`
- `runtimeconfig`
- `hostfxr`

Verify that the application argument passed to hostfxr is the managed DLL, for
example `MyApplication.dll`, rather than the native executable or its output
directory.

For example, this message means hostfxr was passed a directory instead of a
managed assembly:

```text
Application '.../bin/Debug/net8.0' is not a managed executable.
```

For Qt Bridge hosts, trace this value back through `QDotNetHost::loadApp()`.
In particular, test harnesses obtain the DLL name from `QT_DOTNET_HOST` via
`QtTestSetupBase::locateAssembly()`.

## Disable tracing

Remove the environment variables after the investigation so unrelated runs do
not produce diagnostic files.

### PowerShell

```powershell
Remove-Item Env:COREHOST_TRACE -ErrorAction SilentlyContinue
Remove-Item Env:COREHOST_TRACEFILE -ErrorAction SilentlyContinue
```

### Bash or Zsh

```sh
unset COREHOST_TRACE
unset COREHOST_TRACEFILE
```
