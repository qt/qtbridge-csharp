# Qt Bridge for C# &mdash; Templates

Official `dotnet new` templates to build Qt/QML apps with C#/.NET using **QtGroup.Qt.Bridge.CSharp.win-x64**.

## What you get

- **Project template** - `Qt.Bridge.CSharp Project`
  - Minimal C# app wired to Qt Quick / QML.
  - Entry QML (`Main.qml`) + bootstrap `Program.cs`.

- **Item template** - `Qt.Bridge.CSharp QML File (.qml)`
  - Adds a new `.qml` file and lets the build integrate it automatically.

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
dotnet new qtapp -n MyQtApp
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

## Future plans
- TODO: Fill out once we're ready to publish

---

## Versioning & support

Issues/feedback welcome — please file an issue with your OS, .NET, and Qt details.

---

## Licensing

This package bundles components under the open-source license. Review the included license files for details and choose the option that applies to your use case. The Qt Quick runtime subset is provided under the respective open-source terms.

---

## Acknowledgments

© The Qt Company Ltd. and contributors.
Qt is a trademark of The Qt Company Ltd. All other trademarks are the property of their respective owners.
