# Change Log

## Version 0.5.0

### Features

* Add Linux arm64 support and extend the platform-specific package and build integration.

* Extend metadata-based exports with collection change events, configurable metadata files,
  checksum validation, and improved type registration and object loading.

* Add configurable resource packaging, including native-host manifest data and validation for
  external resource packages.

* Improve the Visual Studio QML Language Server integration with local build settings, updated
  QML build metadata, and support for passing the configured Qt QML directory before the first
  build.

* Add a tutorial for creating a C# and QML application and improve the profiling tools.

### Fixes and compatibility improvements

* Improve safe-method handling, cached constructors, managed exception handling, and native/managed
  interop reliability.

* Improve cross-platform path handling, redirected process output, test isolation, build
  diagnostics, and incremental native builds.

* Update CI dependencies to Qt 6.12.0 and the current major installed .NET version.

## Version 0.4.0 &ndash; Beta

### Features

* **Metadata-based type exports.** Qt Bridge can now describe exported managed types in a JSON
  metadata file that is loaded by the native bridge at run time. This is an alternative to the
  established source-code generation path, which remains the default.

  - Select the export mode globally with the `QtExportAs` build property, or per assembly/type
    with `Qt.Export` / `Qt.ExportAs`.

  - The metadata schema and exporter currently cover properties, methods, events, indexers,
    singleton types, models, and collection-as-model types.

  - Metadata exports enable load-time type registration and dynamic-object dispatch. This work is
    still under active development.

* Add simplified `TreeModel` support for implementing tree views from C#, and extend the sample
  Models and Views application with list, table, and tree examples.

* Add Windows arm64 support and macOS x64/arm64 package and project-template support. As with
  Linux, these packages use a compatible external Qt installation selected through `QtDir`.

* Improve the build integration:

  - Publish application, QML-resource, and QML build metadata for the Visual Studio QML Language
    Server integration, including Fast Up-to-Date Check support.

  - Add a packaged pre-built application host, more reliable incremental generation/cleanup, and
    architecture-aware native builds.

  - Support debug Qt deployment, with a release-runtime fallback when debug Qt binaries are not
    available.

* Add Visual Studio project/item template integration and a DocFX documentation site covering
  project setup, existing projects, QML editing, resources, templates, and platform support.

### Fixes and compatibility improvements

* Improve cross-platform path, resource-alias, QRC, and QML metadata handling, including
  deterministic handling of duplicate resource identities.

* Improve native/managed interop reliability for string marshalling, null dynamic-object returns,
  event notifications, metadata initialization, and safe-method caching.

### Bug Fixes

|#|Summary|
|-|-|
|[QTBRIDGES-330](https://qt-project.atlassian.net/browse/QTBRIDGES-330)|Fix memory leak of inbound string data buffers|

## Version 0.3.0 &ndash; Beta

### Features

* Build C# + QML applications in Linux.

* [Visual Studio extension](https://marketplace.visualstudio.com/publishers/TheQtCompany)

  - C# + QML project template

  - QML Intellisense via the [QML Language Server](https://doc.qt.io/qt-6/qtqml-tooling-qmlls.html)

* [App resources](HOW-TO%20resources.md)

* [App properties](src/Qt.Bridge.CSharp.Api/Qt/Application.cs)

* Support for C# delegates

  - Pass QML/JS functions to C# as delegates

  - Invoke C# delegates in QML

### Bug Fixes

| #                                                                      | Summary                 |
|------------------------------------------------------------------------|-------------------------|
| [QTBRIDGES-276](https://qt-project.atlassian.net/browse/QTBRIDGES-276) | C# Bridge has hard dependency on .NET 8 |

### Examples

* [Bookshelf](examples/Bookshelf)


## Version 0.2.0 &ndash; Beta / Internal Milestone

### Features

* Extend full [`QAbstractItemModel`](https://doc.qt.io/qt-6/qabstractitemmodel.html) API.

  - Inherit from [`Model`](src/Qt.Bridge.CSharp.Api/Qt/Bridge/Models/Model.cs) to extend
  [`QAbstractItemModel`](https://doc.qt.io/qt-6/qabstractitemmodel.html) in C#.

  - Inherit from [`TableModel`](src/Qt.Bridge.CSharp.Api/Qt/Bridge/Models/TableModel.cs) to extend
  [`QAbstractTableModel`](https://doc.qt.io/qt-6/qabstracttablemodel.html) (QATM) in C#.

  - [`TableModel<T>`](src/Qt.Bridge.CSharp.Api/Qt/Bridge/Models/TableModel.cs) helper type
  simplifies implementation of the QATM API.

  - Instances of .NET types that inherit from `Model`, `ListModel`, `ListModel<T>`, `TableModel` or
  `TableModel<T>` can be used as models in QML views.

* **[EXPERIMENTAL]** Build C# + QML applications in Linux.

### Examples

* [Models and Views](examples/ModelsAndViews)
* [Spreadsheet Sandbox](examples/SpreadsheetSandbox)


## Version 0.1.0 &ndash; Alpha / Public Early Code Access

### Features

* Create C# + QML projects.

* Generate native proxies/wrappers for .NET types.

* Instantiate .NET types in QML.

* .NET object composition with QML nested instances.

* Invoke .NET methods and properties set/get from QML.

* Expose observable properties to QML, according to the
[`INotifyPropertyChanged`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged)
  protocol.

* Handle .NET events in QML.

* Use the `TypeCast` singleton to cast to .NET types in QML.

  - Strongly-typed references to .NET objects from QML.

* Inherit from [`ListModel`](src/Qt.Bridge.CSharp.Api/Qt/Bridge/Models/ListModel.cs) to extend
[`QAbstractListModel`](https://doc.qt.io/qt-6/qabstractlistmodel.html) (QALM) in C#.

  - Instances of .NET types that inherit from `ListModel` can be used as models in QML views.

  - [`ListModel<T>`](src/Qt.Bridge.CSharp.Api/Qt/Bridge/Models/ListModel.cs) helper type simplifies
  implementation of the QALM API.

* Use .NET library collections as static models in QML.

* Use .NET
[`ObservableCollection`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.objectmodel.observablecollection-1)
  objects as dynamic models in QML.

* Create C# + QML projects and QML source files from `dotnet` templates.

* Install from NuGet packages.

### Examples

* [Tutorial](examples/Tutorial)
* [Primes](examples/Primes)
* [City Temperatures](examples/CityTemperatures)
* [Color Palette](examples/ColorPalette)
