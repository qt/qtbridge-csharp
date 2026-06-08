# C# and QML

Implementation notes for this page:

## Scope boundary

Owns:

* The mental model for how C# code and QML UI code relate.
* Conceptual examples that show C# objects, properties, notifications, and models being used from QML.
* Guidance for C# developers who need enough QML/Qt Quick context to understand the bridge.

Does not own:

* Project setup, package installation, or template commands.
* Migration steps for adding Qt Bridge for C# to an existing application.
* Applying these concepts to an already-existing C# codebase; that belongs on Existing C# Projects.
* Detailed API reference material for every attribute, model type, or resource helper.

## Content ideas

* Explain this page as the main learning page for the simple use case.
* Frame QML as the UI language and Qt Quick as the renderer/runtime for that UI.
* Explain that application logic should remain ordinary C# where possible.
* Show how C# objects can be made available to QML.
* Explain at a high level that the bridge uses build-time generation to make selected C# types
visible to QML.
* Cover the basic shape of properties, methods, and notifications/events if applicable.
* Explain how QML binds to values exposed by C#.
* Describe how standard .NET collections can be used as models for QML views.
* Mention when built-in collection/model support is enough.
* Mention when custom data models become useful.
* Keep the tone C#-first and avoid assuming previous Qt knowledge.
* Avoid deep API details; link to the API Reference for exact types.
* Include a small conceptual example later, ideally one C# type and one QML snippet.

