# Introduction

Qt Bridge for C# connects a C# backend with a QML frontend. It is designed for .NET developers who
want to keep application logic, models, and domain code in familiar C# while using Qt Quick for the
user interface.

QML is Qt's declarative UI language: concise, binding-driven, and purpose-built for animated, fluid
interfaces. Qt Quick is the rendering and UI framework behind it. The bridge lets those two worlds
meet without requiring you to start from a full C++ Qt application.

## Why use it?

Qt Quick is useful for building fluid, modern user interfaces, but the traditional Qt way can feel
unfamiliar to a C# team. Qt Bridge for C# lowers that initial barrier by letting you:

* Keep business logic and application state in C#
* Describe the visual layer with QML and Qt Quick
* Expose C# types, models, and resources to the UI layer
* Evaluate Qt UI technology without committing to a full C++ application

## Who is it for?

Use Qt Bridge for C# if your application logic belongs naturally in C#, but you want to explore Qt
Quick as the UI layer. You do not need to know Qt or QML before you start; the early guides explain
the Qt concepts through the lens of a .NET project.

The current release is especially relevant if you are:

* Looking for a cross-platform way to build desktop UIs for C# applications
* Evaluating Qt Bridge for C# as an early adopter
* Building desktop applications with .NET
* Exploring QML for the first time
* Using Visual Studio and want QML editor support in the same workflow
* Looking for examples that show how C# models connect to QML views

## How it fits together

<div class="docs-flow-diagram" aria-label="Qt Bridge for C# application architecture">
  <div class="docs-flow-card">
    <strong>C#</strong>
    <span>Application logic, models, resources</span>
  </div>
  <div class="docs-flow-arrow" aria-hidden="true"></div>
  <div class="docs-flow-card docs-flow-card-bridge">
    <strong>Qt Bridge for C#</strong>
    <span>Exposes selected C# types to QML</span>
  </div>
  <div class="docs-flow-arrow" aria-hidden="true"></div>
  <div class="docs-flow-card">
    <strong>QML</strong>
    <span>Declarative interface layer</span>
  </div>
  <div class="docs-flow-arrow" aria-hidden="true"></div>
  <div class="docs-flow-card">
    <strong>Qt Quick</strong>
    <span>Renders the user interface</span>
  </div>
</div>

A Qt Bridge for C# application has two main sides:

* QML describes the user interface that Qt Quick renders.
* C# contains the application logic, data models, bridge attributes, and resources.

When building your application, Qt Bridge for C# generates the interop pieces needed for the QML
side to understand the C# types you expose. The Visual Studio extension can provide QML
diagnostics, completion, semantic editor support, and project-aware imports.

## What is included?

The repository and packages provide the main pieces needed to evaluate and build Qt Bridge for C#
applications:

* NuGet packages for consuming the bridge from C# projects
* Bridge attributes and APIs for controlling what is exposed to QML
* Model helpers for list, table, and item-based data
* Resource helpers for packaging application assets
* .NET CLI templates and example applications
* A Visual Studio extension with project/item templates and QML Language Server integration
* Generated API reference pages for namespaces, types, properties, methods, and attributes

## Current product status

Qt Bridge for C# is currently in beta. APIs, workflows, and documentation may evolve as feedback
arrives.

By installing this package, you agree to the [Qt terms and conditions](https://www.qt.io/terms-conditions).
Those terms also apply to the Qt Framework, which is a major dependency of the package.

Qt Bridge for C# is built using the .NET SDK and Runtime, developed and maintained by Microsoft and
the .NET Foundation. No Microsoft code or binaries are redistributed as part of Qt Bridge for C#.
.NET and C# are trademarks of Microsoft Corporation. This project is not affiliated with or endorsed
by Microsoft.

