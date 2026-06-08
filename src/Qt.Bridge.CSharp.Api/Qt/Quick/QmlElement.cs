// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;

namespace Qt.Quick
{
    /// <summary>
    /// Marks a type as a QML element and configures how Qt Bridge registers it with QML.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Apply this attribute to a class, struct, or interface to make the type visible to the Qt
    /// Bridge QML generation pipeline even when it is declared outside the main source assembly.
    /// Use <see cref="Name"/> to override the default QML type name and
    /// <see cref="Singleton"/> to register the type as a QML singleton.
    /// </para>
    /// <![CDATA[
    /// ```csharp
    /// [QmlElement(Name = "Counter", Singleton = true)]
    /// public class CounterService
    /// {
    ///     public int Clicks { get; set; }
    /// }
    /// ```
    /// ]]>
    /// <para>
    /// In QML, the type is exposed under the name <c>Counter</c>. Because it is registered as a
    /// singleton, it is intended to be shared as one instance rather than created repeatedly.
    /// </para>
    /// <![CDATA[
    /// ```qml
    /// Text {
    ///     text: Counter.clicks
    /// }
    /// ```
    /// ]]>
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
        Inherited = false)]
    public class QmlElementAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the QML name used when the annotated type is registered.
        /// </summary>
        /// <remarks>
        /// When omitted, the generator uses the default element name for the type. If specified,
        /// the name must be a valid QML type name and must start with an uppercase letter and then
        /// contain only letters, digits, or underscores.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the annotated type should be registered as a QML singleton.
        /// </summary>
        /// <remarks>
        /// A QML singleton is a type that QML exposes as one shared instance for the whole QML
        /// engine, similar in intent to an application-wide service. Use this for service or
        /// utility objects that should be shared rather than instantiated repeatedly from QML.
        /// </remarks>
        public bool Singleton { get; set; }
    }

    /// <summary>
    /// Registers a specific runtime type as a QML element at assembly scope.
    /// </summary>
    /// <typeparam name="T">
    /// The runtime type to expose to the Qt Bridge QML generation pipeline.
    /// </typeparam>
    /// <remarks>
    /// This generic assembly-level form is useful when the target type cannot be annotated
    /// directly, for example because it is defined in another assembly. The inherited
    /// <see cref="QmlElementAttribute.Name"/> and <see cref="QmlElementAttribute.Singleton"/>
    /// properties can be used to customize registration metadata.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class QmlElementAttribute<T> : QmlElementAttribute
    {
    }

    /// <summary>
    /// Provides QML component lifecycle callbacks for a generated Qt Bridge object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface when a type needs notification during QML construction or when it
    /// needs access to nested QML child objects after the component has been completed. This is
    /// especially useful for parent-child composition patterns in QML, where a parent element
    /// needs to discover and wire up child elements declared inside it. Qt Bridge maps these
    /// members to the corresponding QQmlParserStatus lifecycle hooks in the generated adapter
    /// layer.
    /// </para>
    /// <para>
    /// A common pattern is to use <see cref="QmlComponentComplete(object[])"/> to discover nested
    /// child objects declared inside the element in QML and wire them to the parent service or
    /// controller object.
    /// </para>
    /// <![CDATA[
    /// ```csharp
    /// public class ApiResource
    /// {
    ///     public string Path { get; set; }
    ///     internal ApiClient Client { get; set; }
    /// }
    ///
    /// public class ApiClient : IQmlElement
    /// {
    ///     private List<ApiResource> Resources { get; } = new();
    ///
    ///     public void QmlClassBegin()
    ///     { }
    ///
    ///     public void QmlComponentComplete(object[] nestedElements)
    ///     {
    ///         foreach (var element in nestedElements) {
    ///             if (element is not ApiResource resource)
    ///                 continue;
    ///
    ///             Resources.Add(resource);
    ///             resource.Client = this;
    ///         }
    ///     }
    /// }
    /// ```
    /// ]]>
    /// <para>
    /// With a C# type like the one above, QML can declare nested child objects inside the parent
    /// element:
    /// </para>
    /// <![CDATA[
    /// ```qml
    /// ApiClient {
    ///     ApiResource {
    ///         path: "users"
    ///     }
    ///
    ///     ApiResource {
    ///         path: "orders"
    ///     }
    /// }
    /// ```
    /// ]]>
    /// <para>
    /// When the QML component is complete, Qt Bridge passes those nested child objects to
    /// <see cref="QmlComponentComplete(object[])"/>, where the implementation can inspect, cast,
    /// and initialize them. In this example, the two <c>ApiResource</c> objects are child
    /// elements composed inside <c>ApiClient</c>.
    /// </para>
    /// </remarks>
    public interface IQmlElement
    {
        /// <summary>
        /// Called when QML begins constructing the object.
        /// </summary>
        /// <remarks>
        /// Use this callback for lightweight initialization that should happen before nested child
        /// objects are fully wired up.
        /// </remarks>
        void QmlClassBegin();

        /// <summary>
        /// Called after the QML component has been fully constructed.
        /// </summary>
        /// <param name="nestedElements">
        /// The nested QML child objects collected by the generated bridge wrapper.
        /// </param>
        /// <remarks>
        /// This is the right place to inspect or wire up nested child objects declared inside the
        /// element in QML, for example service-specific helper or resource objects.
        /// </remarks>
        void QmlComponentComplete(object[] nestedElements);
    }
}
