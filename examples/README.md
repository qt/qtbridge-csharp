<!--
// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only
-->

# Qt Bridge for C# &ndash; Examples

Each example is contained in its own directory. Some examples also include a `README.md` file
with additional notes.

## Getting the examples

The examples are part of the Qt Bridge for C# repository. Clone it to get a local copy:

```bash
git clone https://github.com/qt/qtbridge-csharp.git
cd qtbridge-csharp/examples
```

Open `examples.sln` in Visual Studio, or build and run any example directly with the
`dotnet` CLI (replace `Primes` with the example you want to run):

```bash
dotnet run --project Primes/Primes.csproj -c Release
```

> The examples require the Qt Bridge NuGet package to be resolvable. The easiest way is to
> install the package from the NuGet feed — see the
> root [README](../README.md#importing-qt-bridge-as-a-package-reference) for details.

## Examples

| Example | Description |
|---------|-------------|
| [Bookshelf](Bookshelf) | Demonstrates all three Qt Bridge resource access modes (`Native`, `ManagedAndNative` and `ManagedOnly`) using a book library application that reads cover art, synopsis text, and app metadata from `.resx` and Qt resources. |
| [CityTemperatures](CityTemperatures) | Shows how to model and display tabular data from multiple cities using a C# data class exposed to a QML view. |
| [ColorPalette](ColorPalette) | A full-featured example demonstrating REST API integration, user authentication, and CRUD operations on a remote color palette service. Includes pagination, custom QML styles, and SVG icons. |
| [ModelsAndViews](ModelsAndViews) | Shows how to expose C# data to QML `ListView`, `TableView`, and `TreeView` using the corresponding Qt Bridge model types. |
| [Primes](Primes) | Demonstrates prime number generation exposed to QML through multiple model patterns: static value lists, observable collections, item models, and event-driven models. |
| [SpreadsheetSandbox](SpreadsheetSandbox) | Shows how to build a two-dimensional spreadsheet-like data model in C# and expose it to a QML table view. |
| [Tutorial](Tutorial) | A minimal introductory example that demonstrates the recommended project structure and the basic pattern for connecting a C# model to a QML view. |
| [UserView](UserView) | A multi-project example split into a shared library (`UserViewLib`), a console front-end (`UserViewCli`), and a QML front-end (`UserViewQml`), showing how to reuse a C# model across different application types. |

## Contributing

Contributions to Qt Bridge for C# are welcome. Before submitting a contribution, please review the
[Qt Contribution Guidelines](https://wiki.qt.io/Qt_Contribution_Guidelines).

If you contribute to the code of Qt Bridge for C#, note that the following packages (licensed under
the MIT license) are required for development and testing:

- `Microsoft.NET.Test.Sdk`
- `MSTest.TestFramework`
- `Microsoft.CodeAnalysis.CSharp`
- `coverlet.collector`

## Stay in touch

You can reach us on the Qt Forum in the
[Qt Bridges category](https://forum.qt.io/category/78/qt-bridges).

## License

    Copyright (C) 2026 The Qt Company Ltd.
    SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

Qt Bridge for C# is available under the Qt Commercial License or the GNU Lesser General Public
License v3.0-only (`LGPL-3.0-only`).

For commercial licensing, see https://www.qt.io/licensing/.

## Terms and Conditions

If you, your employer, or the legal entity you act on behalf of hold commercial license(s) with a
Qt Group entity, Qt Bridges constitutes Pre-Release Code under the Qt License/Frame Agreement
governing those licenses, and that agreement's terms and conditions relating to Pre-Release Code
apply to your use of Qt Bridges as found in this repo.

.NET and C# are trademarks of Microsoft Corporation. This project is not affiliated with, or
endorsed by Microsoft.
