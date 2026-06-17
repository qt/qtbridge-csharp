# Change Log

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
