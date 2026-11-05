// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Quick
{
    /// <summary>
    /// Interface of arguments to <see cref="Qml.Load{T}(T)"/>.
    /// </summary>
    /// <remarks>
    /// Instances of types that implement this interface can be used to specify arguments when
    /// loading corresponding QML components. This includes the module and type name, as well as
    /// initial properties. By default, <see cref="Qml.Load{T}(T)"/> will use all public instance
    /// properties of <c>T</c> as initial properties. To override this default behavior, implement
    /// <see cref="InitialProperties"/> in the definition of <c>T</c>.
    /// <para/>
    /// <b>Note:</b> instances that implement <see cref="IQmlLoadArgs"/> are only used to initialize
    /// QML components during <see cref="Qml.Load{T}(T)"/>. Subsequent changes to the properties of
    /// these instances have no effect on the loaded QML components.
    /// <para/>
    /// Example:
    /// <example><code><![CDATA[
    /// ```csharp
    /// public sealed class MainPageArgs : IQmlLoadArgs
    /// {
    ///     public static string Name => "Main";
    ///     public int Foo { get; init; }
    ///     public string[] Bar { get; init; }
    /// }
    ///
    /// public sealed class SplashScreenArgs : IQmlLoadArgs
    /// {
    ///     public static string Module => "Utils";
    ///     public static string Name => "Splash";
    ///     public int Foo { get; init; }
    /// }
    ///
    /// public static void Main(string[] args)
    /// {
    ///     Qml.Load<MainPageArgs>(new()
    ///     {
    ///         Foo = 42,
    ///         Bar = ["b", "a", "r"]
    ///     });
    ///     Qml.Load<SplashScreenArgs>(new()
    ///     {
    ///         Foo = 99,
    ///     });
    ///     Qml.WaitForExit();
    /// }
    /// ```
    /// ]]></code></example>
    /// </remarks>
    public interface IQmlLoadArgs
    {
        /// <summary>
        /// URI of the module containing the QML type. Defaults to <see cref="Qml.RootModule"/>.
        /// </summary>
        static virtual string Module { get; } = null;

        /// <summary>
        /// Name of the QML type.
        /// </summary>
        static abstract string Name { get; }

        /// <summary>
        /// Initial properties of the newly created QML component.
        /// </summary>
        /// <remarks>
        /// By default, <see cref="Qml.Load{T}(T)"/> will use all public instance properties of
        /// <c>T</c> as initial properties. To override this default behavior, implement
        /// <see cref="InitialProperties"/> in the definition of <c>T</c>.
        /// <code><![CDATA[
        /// ```csharp
        /// internal sealed class MainPageArgs : IQmlLoadArgs
        /// {
        ///     public static string Name => "Main";
        ///     public Dictionary<string, object> InitialProperties { get; init; }
        /// }
        ///
        /// public static void Main(string[] args)
        /// {
        ///     Qml.Load<MainPageArgs>(new()
        ///     {
        ///         InitialProperties = new()
        ///         {
        ///             ["Foo"] = 42,
        ///             ["Bar"] = new[] { "b", "a", "r" },
        ///         }
        ///     });
        /// }
        /// ```
        /// ]]></code>
        /// </remarks>
        virtual Dictionary<string, object> InitialProperties => null;
    }
}
