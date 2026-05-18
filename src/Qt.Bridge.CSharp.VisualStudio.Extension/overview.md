<!--
// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only
-->

# Qt Bridge for C#

Qt Bridge for C# adds QML editor support to Visual Studio for Qt Bridge projects.

Use it to work with C# application logic and Qt Quick user interfaces in the same Visual Studio
workflow. The extension recognizes Qt Bridge projects, enables QML Language Server features for
`.qml` files, and refreshes the editor integration after project builds.

<img src="https://raw.githubusercontent.com/qt/qtbridge-csharp/dev/src/Qt.Bridge.CSharp.VisualStudio.Extension/marketplace/images/qt-bridge-qml-editor.png" width="900" alt="QML editor support in Visual Studio" />

## Features

- QML diagnostics directly in Visual Studio.
- Completion and semantic editor support for `.qml` files.
- Automatic QML Language Server setup for Qt Bridge projects.
- Project-aware QML imports and generated type information after builds.
- Qt Bridge project templates included with the extension.

## How it fits into Qt Bridge for C#

Qt Bridge for C# lets you write application logic in C# while building the user interface with QML
and Qt Quick. The Visual Studio extension focuses on the editor experience: it helps Visual Studio
understand the QML side of a Qt Bridge project so QML files are easier to navigate, edit, and keep
in sync with generated project information.

<img src="https://raw.githubusercontent.com/qt/qtbridge-csharp/dev/src/Qt.Bridge.CSharp.VisualStudio.Extension/marketplace/images/qt-bridge-template.png" width="900" alt="Creating a Qt Bridge for C# project in Visual Studio" />

## Getting Started

1. Install the extension in Visual Studio.
1. Create or open a Qt Bridge for C# project.
1. Build the project once so Qt Bridge can generate the project metadata used by the QML editor
   integration.
1. Open a `.qml` file to use diagnostics, completion, and semantic editor support.

For full setup instructions, see the
[Qt Bridge for C# documentation](https://doc-snapshots.qt.io/qtbridges-dev/qtbridges-csharp-index.html).

## Feedback

Please report issues in the
[Qt Bridge for C# repository](https://github.com/qt/qtbridge-csharp/issues).
