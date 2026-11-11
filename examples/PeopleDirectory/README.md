# People Directory

## `Core`

Framework-neutral library containing models, data access, view models, navigation, DI,
HostApplicationBuilder creation, and IHost ownership.

## `App.Wpf`

WPF app containing only UI-bound application lifetime, windows, views, XAML, styles, resources, and
converters.

## `App.Qml`

Qt Quick frontend for the same `Core`. C# passes the root view model to QML using `IQmlLoadArgs`
and `Qml.Load`. The window uses `getType()` to select the view for the current view model.
