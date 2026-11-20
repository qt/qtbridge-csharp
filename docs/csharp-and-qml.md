# C# and QML

A Qt Bridge for C# application has two halves with different jobs: C# holds your application
logic, and QML describes the user interface. Understanding how they connect makes the rest of the
bridge predictable.

## Two languages, two jobs

A Qt Bridge for C# application has two halves that each do a different job:

* **C#** holds your application logic: state, data, rules, and the kind of code you already write in
  any .NET application.
* **QML** describes the user interface: what is on screen, how it is laid out, and how it responds
  to input and animation. **Qt Quick** is the engine that renders that interface.

You keep writing application logic the way you already do in C#. QML takes over the part that is
usually the most tedious to hand-code: describing and animating a UI. The bridge is the connective
layer that lets values, objects, and collections in your C# code show up and update inside the QML
side.

## How your C# becomes visible to QML

Qt Bridge for C# exposes selected C# types, objects, properties, methods, and models to QML. By
default, your project's public types are visible; attributes such as
[`[Qt.Ignore]`](api/Qt.IgnoreAttribute.yml) and [`[Qt.Include]`](api/Qt.IncludeAttribute.yml) let you
fine-tune exactly what QML can see. The first build produces the bridge information and native
support needed by the QML side, so this surface is ready by the time your app runs &mdash; you do
not write any of that glue yourself.

By default, the bridge generates C++ wrappers for exported types. See
[Metadata-based Exports (Experimental)](metadata-based-exports.md) to learn how to select the
export mode for a project or individual types and when you can build without C++ compilation.

Once a C# type is exposed, you can use it directly in QML: write its name with curly braces to
create an instance, then read or set its properties and call its methods &mdash; the same way you
would use one of QML's own built-in items, such as
[`Text`](https://doc.qt.io/qt-6/qml-qtquick-text.html) or
[`Button`](https://doc.qt.io/qt-6/qml-qtquick-controls-button.html), in the example below.

```csharp
public class Counter
{
    public int Value { get; set; }

    public void Increment() => Value++;
}
```

```qml
Counter {
    id: counter
}

Text {
    text: "Value: " + counter.value
}

Button {
    text: "Increment"
    onClicked: counter.increment()
}
```

Notice that QML uses `camelCase` for the members that C# exposes as `PascalCase`. This follows QML's
own naming convention, so the type feels native on the QML side.

## Identify the runtime .NET type from QML

Use `getType()` on a bridged .NET object to read its runtime type name as a string. The helper is
available with both source-code and metadata-based exports:

```qml
console.log(viewModel.getType()) // For example: "MyApp.CustomerViewModel"
```

A typical use is choosing a view for a view model. C# creates the view models and passes a root
view model to QML through [`IQmlLoadArgs`](api/Qt.Quick.IQmlLoadArgs.yml) and
[`Qml.Load(...)`](api/Qt.Quick.Qml.yml). These arguments initialize the QML component once.
Subsequent view-model changes reach QML through
[property-change notifications](#keeping-the-ui-in-sync-properties-and-notifications).

When a property exposes different view models during navigation, QML can use their runtime type
names to select a view:

```qml
function viewSource(viewModel) {
    if (!viewModel)
        return ""

    switch (viewModel.getType()) {
    case "MyApp.CustomerViewModel":
        return "CustomerView.qml"
    case "MyApp.OrderViewModel":
        return "OrderView.qml"
    default:
        return ""
    }
}
```

A `Loader` or view factory can use this URL. The application controls the mapping, loads the view
and passes the view model to it. Keep the mapping in sync when C# type names or namespaces change.

## Keeping the UI in sync: properties and notifications

A plain property like `Value` above is read once. If your C# code changes it later and you want the
UI to update automatically, implement
[`INotifyPropertyChanged`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged).
QML treats a notifying property as a binding source: whenever your code raises the change
notification, every binding that depends on that property re-evaluates.

This is the same pattern you would use to drive a WPF or MVVM-style binding &mdash; the difference is
that the binding target is now a QML item instead of a XAML control.

## Exposing collections and data as models

QML views such as [`ListView`](https://doc.qt.io/qt-6/qml-qtquick-listview.html) and
[`TableView`](https://doc.qt.io/qt-6/qml-qtquick-tableview.html), as well as simpler model-driven
items like [`Repeater`](https://doc.qt.io/qt-6/qml-qtquick-repeater.html), all expect a model to
iterate over. You have two ways to provide one:

* **Use an existing .NET collection.** Static collections and observable collections such as
  [`ObservableCollection<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.objectmodel.observablecollection-1)
  can be exposed directly as models. This is often enough when your data is a straightforward list
  that QML mostly needs to display.
* **Derive from a model base type.** When you need full control over how items are counted,
  fetched, edited, or grouped &mdash; for example paging, lazy loading, or hierarchical data &mdash;
  derive from [`ListModel<T>`](api/Qt.Bridge.Models.ListModel-1.yml),
  [`TableModel<T>`](api/Qt.Bridge.Models.TableModel-1.yml), or
  [`Model`](api/Qt.Bridge.Models.Model.yml) and implement the item-count and item-lookup members
  yourself.

A small list model looks like the C# you already write, with a couple of bridge-aware notification
calls around the parts that change:

```csharp
public class NameList : ListModel<string>
{
    private List<string> Names { get; } = new();

    public void Add(string name)
    {
        BeginInsertItems(Names.Count, Names.Count);
        Names.Add(name);
        EndInsertItems();
    }

    public override string Data(int index) => Names[index];
    public override int ItemCount() => Names.Count;
}
```

```qml
NameList {
    id: names
}

ListView {
    model: names
    delegate: Text {
        required property string item
        text: item
    }
}
```

[`BeginInsertItems`](api/Qt.Bridge.Models.ListModel-1.yml)/[`EndInsertItems`](api/Qt.Bridge.Models.ListModel-1.yml)
and the other `Begin`/`End` pairs on the model base types bracket a structural change &mdash; an
insert, removal, move, or reset. Calling them tells QML's views exactly which items are about to
appear, disappear, or move, so the view can update just those items instead of redrawing everything.

## Choosing between built-in support and a custom model

Start with a plain collection or an
[`ObservableCollection<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.objectmodel.observablecollection-1).
Reach for a custom model only once you
need behavior the built-in support does not offer, such as paging, custom sorting, or item editing.
Moving from one to the other later is a localized change: the QML side keeps binding to a model the
same way.

## Where to go from here

Now that you have the mental model, choose the next page based on what you want to do:

* See [Project Templates](templates-and-examples.md) for a hands-on starting point you can run
  and modify.
* See [Adding QML to Existing C# Projects](existing-csharp-projects.md) if you want to apply these
  ideas to a codebase you already have.
* See the [API Reference](api-reference.md) for the exact attributes, model base types, and helpers
  mentioned above.
