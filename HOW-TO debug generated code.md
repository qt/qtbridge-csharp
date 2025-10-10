<!--************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
*************************************************************************************************-->

# Debugging generated code

The following applies only to example / demo / test projects in this solution. Let ***FooBar*** be
one such project.

## Build ***FooBar*** and debug generated code

To debug ***FooBar***'s generated code:

1. Build project ***FooBar*** in the `Debug` configuration.

    * The generated native solution will include a custom build step to copy the target binary
      `foo_bar.exe` to the `bin` directory, and also to write debug configuration properties
      pointing to that copied binary.

2. Open the generated native solution in another instance of Visual Studio.

    * For example: right-click on ***FooBar***, select *"Open in Terminal"* and run the following:

          devenv .\obj\Debug\net8.0\qtdotnet\native\build\foo_bar.sln

    * We should now have two Visual Studio instances running side-by-side:
        - **VS#1** with the *qtdotnet.sln* solution
        - **VS#2** with the *foo_bar.sln* solution

3. Switch to **VS#2** and press F5.

    * The ***FooBar*** app starts in debug mode.
    * It should be possible to break anywhere in the app's code.

**NOTE**: By default, the "mixed mode" debugger is selected. This allows breaking and stepping
through both the native and managed code of the app. There is however, a limitation to using this
debugger mode, namely that it will only be possible to step into/through native code once the
*managed* debug session is running. In practice this means that it will not be possible to break at
the `main` function of the application before the `loadApp` function of the .NET host is called.
Debugging native code executed before this call can only be done in a native-only debug session.

## Changes to ***FooBar***'s generated code in **VS#2**

It's possible to make changes to the generated native code inside **VS#2** and debug those changes
right away.

1. In **VS#2**, make changes to the generated native code.

    * These changes will, of course, not be reflected in the code generation rules. Rebuilding
      ***FooBar*** in **VS#1** will overwrite any changes made to the generated code in **VS#2**.

2. Press F5.

    * The generated *foo_bar.sln* solution is built, taking into account the changes made.
    * The `foo_bar.exe` is copied to the `bin` directory.
    * The ***FooBar*** app starts in debug mode.

## Changes to ***FooBar*** C# code with both **VS#1** and **VS#2** opened

1. In **VS#1**, make changes to the C# code of ***FooBar***.

2. Build ***FooBar*** in **VS#1** in the `Debug` configuration.

    * This will overwrite any changes made to the generated code in **VS#2**.

3. Switch to **VS#2** and press F5.

    * The ***FooBar*** app starts in debug mode.
    * The changes to the C# code should be in effect.

## Changes to generation rules with both **VS#1** and **VS#2** opened

This requires a rebuild of ***FooBar*** because the incremental build up-to-date check for the
project will not include the generation rules (or the generator tool).

1. In **VS#1**, make changes to the generation rules (or the generator tool).

2. Clean the ***FooBar*** project.

    * For example: right-click on ***FooBar*** and select *"Clean"*.

    * If **VS#2** is opened, this will result in an error due to the output dir of the generated
      code being in use. Ignore this error.

3. Build the ***FooBar*** project in the `Debug` configuration.

    * Assuming project dependencies are correctly set up, changes to the generation rules (or the
      generator tool) will be included in the build and be in effect when generating the code for
      the ***FooBar*** project.

    * This will overwrite any changes made to the generated code in **VS#2**.

4. Switch to **VS#2** and press F5.

    * The ***FooBar*** app starts in debug mode.
    * The changes to the generation rules (or the generator tool) should be in effect.
