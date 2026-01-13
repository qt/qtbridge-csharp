# Qt Bridge - C#

> Copyright (C) 2026 The Qt Company Ltd.
> SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only

This is a pre-release implementation of Qt Bridge for C#.
By installing this package, you agree to the terms and conditions stated in https://www.qt.io/terms-conditions.
These terms and conditions also apply to the Qt Framework, which is used as a major dependency in this package.

The Qt Bridge for C# is built using the .NET SDK and Runtime, which are developed and maintained by Microsoft and the .NET Foundation.

No Microsoft code or binaries are redistributed as part of Qt Bridge for C#.
.NET and C# are trademarks of Microsoft Corporation.
This project is not affiliated with or endorsed by Microsoft.

An application built with Qt Bridge for C# will include the following packages licensed under the MIT license:
  - System.Reflection.MetadataLoadContext
  - System.CommandLine
  - System.IO.Hashing

If you contribute to the code of Qt Bridge for C# you will additionally need the following packages licensed under the MIT license:
  - Microsoft.NET.Test.Sdk
  - MSTest.TestFramework
  - Microsoft.CodeAnalysis.CSharp
  - coverlet.collector

## Pre-requisites

* [**Microsoft Visual Studio 2022**](https://visualstudio.microsoft.com/vs/older-downloads/#visual-studio-2022-and-other-products)
    * [Workloads](https://learn.microsoft.com/en-us/visualstudio/install/modify-visual-studio?view=visualstudio)
        * [.NET desktop development](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-professional?view=visualstudio#net-desktop-development)
        * [Desktop development with C++](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-professional?view=visualstudio#desktop-development-with-c)
* [**Qt 6.10**](https://code.qt.io/cgit/qt/qt5.git/log/?h=6.10) built from source
    * See instructions in: _[Build Qt 6 (subset) from source](HOW-TO%20build%20nuget%20and%20testapp.md#1-build-qt-6-subset-from-source)_

## Visual Studio IDE

### Tools and installation package

1. Open [`qtbridge-csharp.sln`](qtbridge-csharp.sln) solution in Visual Studio
2. Build tools and installation package
    * Select menu option: ___Build___ > ___Build Solution___
    * Or press `Ctrl`+`Shift`+`B`

#### Auto-tests

* View available tests
    * Select menu option: ___View___ > ___Test Explorer___
    * Or press: `Ctrl`+`E`, `T`
* Run tests
    * Select menu option: ___Test___ > ___Run All Tests___
    * Or press: _`Ctrl`+`R`, `A`_

### Examples

1. Open [`examples.sln`](examples/examples.sln) solution in Visual Studio
2. Build example projects
    * Select menu option: ___Build___ > ___Build Solution___
    * Or press _`Ctrl`+`Shift`+`B`_

#### Run example app

1. In the Solution Explorer, right-click an example project
2. Select context menu option: ___Debug___ > ___Start New Instance___

## Command Prompt

### Tools and installation package

1. Open a Command Prompt
    * Windows ___Start___ menu > ___Command Prompt___
    * Or press _<code>&#x229E; Win</code>+`R`_, then type **`cmd`** and press _`Enter`_
2. `cd` to [solution dir](./)
3. Build tools and installation package
    * **`> dotnet build`**

#### Auto-tests

1. Stay in the [solution dir](./)
2. Run tests
    * **`> dotnet test`**

### Examples

1. `cd` to the [examples sub-dir](./examples/)
    * **`> cd examples`**
2. Build example projects
    * **`> dotnet build`**

#### Run example app

1. `cd` to [example project sub-dir](./examples/Primes/)
    * E.g.: **`> cd Primes`**
2. Run example app
    * **`> dotnet run`**
