// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;
using System.Reflection;
using Qt.Bridge.Utils.Text;
using Qt.DotNet;

namespace Qt
{
    namespace Quick
    {
        using static Adapter;

        /// <summary>
        /// Provides the static entry point for loading QML components and running the Qt
        /// application event loop from C#.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="Qml"/> is the primary API for bootstrapping a Qt Bridge application. A
        /// typical application loads one or more QML components by URI and type name, then blocks
        /// on <see cref="WaitForExit(int)"/> until the user closes the window.
        /// </para>
        /// <code language="csharp"><![CDATA[
        /// static void Main(string[] args)
        /// {
        ///     Qml.LoadFromRootModule("MainWindow");
        ///     Qml.WaitForExit();
        /// }
        /// ]]></code>
        /// </remarks>
        public static class Qml
        {
            private static readonly Lazy<IQQmlEngine> LazyInstance =
                new(Static.QQmlEngine_Get, isThreadSafe: true);
            internal static IQQmlEngine Instance => LazyInstance.Value;

            /// <summary>
            /// Gets the URI of the root QML module defined in the entry assembly.
            /// </summary>
            /// <remarks>
            /// The root module is identified by the <see cref="QmlModuleAttribute"/> with
            /// <see cref="QmlModuleAttribute.IsRoot"/> set to <see langword="true"/>. This
            /// attribute is generated automatically for the application's primary QML module.
            /// </remarks>
            /// <exception cref="InvalidOperationException">
            /// No root module was found in the entry assembly.
            /// </exception>
            public static string RootModule
            {
                get
                {
                    var root = Assembly.GetEntryAssembly()
                        ?.GetCustomAttributes<QmlModuleAttribute>()
                        .FirstOrDefault(a => a.IsRoot)
                        ?.Uri;
                    if (string.IsNullOrEmpty(root))
                        throw new InvalidOperationException("QML root module not found.");
                    return root;
                }
            }

            /// <summary>
            /// Loads a QML component by type name from the application's root module.
            /// </summary>
            /// <param name="typeName">
            /// The QML type name to load. The type name is normalized to PascalCase to match Qt's
            /// naming convention for QML types.  For example, <c>"mainWindow"</c> becomes
            /// <c>"MainWindow"</c>.
            /// </param>
            /// <remarks>
            /// Convenience wrapper around <see cref="LoadFromModule"/> that uses
            /// <see cref="RootModule"/> as the module URI. Most applications have a single root
            /// module and call this method once with the top-level window component.
            /// </remarks>
            /// <seealso cref="LoadFromModule"/>
            public static void LoadFromRootModule(string typeName)
            {
                LoadFromModule(RootModule, typeName);
            }

            /// <summary>
            /// Loads a QML component by module URI and type name.
            /// </summary>
            /// <param name="uri">The URI of the QML module that contains the component.</param>
            /// <param name="typeName">
            /// The QML type name to load. The type name is normalized to PascalCase to match Qt's
            /// naming convention for QML types. For example, <c>"mainWindow"</c> becomes
            /// <c>"MainWindow"</c>.
            /// </param>
            /// <remarks>
            /// Multiple components can be loaded before calling <see cref="WaitForExit(int)"/>:
            /// <code language="csharp"><![CDATA[
            /// static void Main(string[] args)
            /// {
            ///     Qml.LoadFromModule("MyApp.Views", "ListView");
            ///     Qml.LoadFromModule("MyApp.Views", "TableView");
            ///     Qml.WaitForExit();
            /// }
            /// ]]></code>
            /// </remarks>
            public static void LoadFromModule(string uri, string typeName)
            {
                Instance.LoadFromModule(uri, typeName.ConvertCase(CaseStyle.Camel, CaseStyle.Pascal));
            }

            /// <summary>
            /// Blocks the calling thread until the QML engine exits or the optional timeout
            /// elapses.
            /// </summary>
            /// <param name="timeout">
            /// Maximum time to wait, in milliseconds. The default value of <c>-1</c> waits
            /// indefinitely until the QML engine exits.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if the QML engine has exited; <see langword="false"/> if
            /// the timeout elapsed before the engine exited.
            /// </returns>
            /// <remarks>
            /// A finite timeout makes it possible to interleave C# work with the event loop. A
            /// common pattern is a polling loop that modifies model data between short waits:
            /// <code language="csharp"><![CDATA[
            /// static void Main(string[] args)
            /// {
            ///     Qml.LoadFromRootModule("Main");
            ///     while (!Qml.WaitForExit(100)) {
            ///         // update model data while the UI remains responsive
            ///     }
            /// }
            /// ]]></code>
            /// </remarks>
            public static bool WaitForExit(int timeout = -1)
            {
                return Instance.WaitForExit(timeout);
            }

            /// <summary>
            /// Pumps the Qt event loop from the main thread, keeping the UI responsive during
            /// long-running C# operations.
            /// </summary>
            /// <remarks>
            /// <para>
            /// This method must be called from the main thread.
            /// </para>
            /// <para>
            /// The intended use is inside a C# method called by QML that takes a noticeable
            /// amount of time. Calling <see cref="ProcessEvents"/> periodically prevents the UI
            /// from freezing while the operation completes:
            /// </para>
            /// <code language="csharp"><![CDATA[
            /// public void ImportData(string path)
            /// {
            ///     foreach (var record in ReadRecords(path)) {
            ///         ProcessRecord(record);
            ///         Qml.ProcessEvents(); // keep the UI alive
            ///     }
            /// }
            /// ]]></code>
            /// </remarks>
            public static void ProcessEvents()
            {
                Debug.Assert(IsMainThread, "Qml.ProcessEvents() must be called from main thread.");
                if (Adapter.IsMainThread)
                    Instance.ProcessEvents();
            }
        }
    }

    /// <summary>
    /// Exposes global Qt runtime properties.
    /// </summary>
    public static class Globals
    {
        /// <summary> Gets the version of the Qt runtime used by the application. </summary>
        public static Version Version => new(Quick.Qml.Instance.Version());
    }
}
