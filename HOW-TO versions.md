# Version management

> Copyright (C) 2026 The Qt Company Ltd.
> SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only

The file `qt_version.props` implements a centralized version management that can be shared between
all relevant projects.

## Define a version

Different versions can be defined and mantained in `qt_version.props`. To define a version, the
following properties need to be set, corresponding to the various parts of the version number:

* `Qt_Major`: Major version number
* `Qt_Minor`: Minor version number
* `Qt_Patch`: Patch version number
* (optional) `Qt_Build`: Build number
* (optional) `Qt_BuildEpoch`: Automated build number epoch (see below)
* (optional) `Qt_BuildResolution`: Automated build number resolution (see below)
* (optional) `Qt_Tag`: Pre-release tag

The property `SelectVersion` will indicate which version is relevant for the project being built.
The above properties can be set in property groups based on the value of `SelectVersion`. For
example:

```xml
<PropertyGroup Condition="'$(SelectVersion)' == 'foo'">
  <Qt_Major>1</Qt_Major>
  <Qt_Minor>2</Qt_Minor>
  <Qt_Patch>3</Qt_Patch>
</PropertyGroup>

<PropertyGroup Condition="'$(SelectVersion)' == 'bar'">
  <Qt_Major>2</Qt_Major>
  <Qt_Minor>4</Qt_Minor>
  <Qt_Patch>8</Qt_Patch>
  <Qt_Build>10</Qt_Build>
</PropertyGroup>
```

In this example, selecting version `foo` will render a version number of `1.2.3`, and selecting
version `bar` will render version number `2.4.8.10`.

### Automating build numbers

It's possible to generate an automatic build number based on a timestamp. To use this option,
instead of setting `Qt_Build`, the aforementioned properties `Qt_BuildEpoch` and
`Qt_BuildResolution` must instead be defined, according to the following:

* `Qt_BuildEpoch`: Timestamp base date
* `Qt_BuildResolution`: Timestamp resolution, in 100-nanosecond units.

Timestamps are calculated from 00:00 UTC of the base date. For compatibility with .NET assembly
version numbers, the timestamp should be within the range of `[0..65534]`. If omitted in the version
definition, the timestamp resolution will default to intervals of 10 minutes, allowing timestamps
up to 455 days after the base date.

## Consume a version

Projects that opt into the centralized version management just need to import the `qt_version.props`
file. Best practice is to place this file at the topmost level of the project folder hierarchy and
then instruct MSBuild to look for it upwards from the directory of each project. For example:

```xml
<Import Project="$([MSBuild]::GetPathOfFileAbove(qt_version.props))" />
```

Importing the `qt_version.props` file should be the first action in each project.

### Selecting a version

As mentioned above, the particular version to be used in a project is selected using the property
`SelectVersion`. This needs to be defined before the import of `qt_version.props` for the selection
to take effect.

Best practice for when this property is omitted is to have a default value set at the beginning of
the `qt_version.props` file.

### Reading the selected version number

After the import of `qt_version.props`, the calculated value of the version number is stored in the
property `SelectedVersion`.

The value of this property is also available in compile-time in an `[AssemblyMetadata]` attribute.
This can be accessed in code as follows:

```csharp
var selectedVersion = Assembly.GetExecutingAssembly()
    .GetCustomAttributes<AssemblyMetadataAttribute>()
    .FirstOrDefault(m => m.Key == "SelectedVersion")
    ?.Value;
```
