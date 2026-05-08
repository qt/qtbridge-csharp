// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.DotNet;

namespace Qt
{
    /// <summary>
    /// Sets app-wide metadata such as the app name, version, publisher details, and default
    /// window icon.
    /// </summary>
    /// <remarks>
    /// Qt Bridge for C# forwards these values to the native application object as soon
    /// as you assign them. In practice, set them near the start of <c>Main</c>, before
    /// <see cref="Quick.Qml.LoadFromRootModule"/>, so the UI sees them from the beginning:
    /// <code language="csharp"><![CDATA[
    /// static void Main(string[] args)
    /// {
    ///     Qt.Application.Name               = "MyApp";
    ///     Qt.Application.Version            = "1.0";
    ///     Qt.Application.OrganizationName   = "Acme Corp";
    ///     Qt.Application.OrganizationDomain = "acme.com";
    ///     Qt.Application.DisplayName        = "My Application";
    ///     Qt.Application.SetWindowIcon("qrc:/assemblies/MyApp/icons/app.svg");
    ///
    ///     Qml.LoadFromRootModule("Main");
    ///     Qml.WaitForExit();
    /// }
    /// ]]></code>
    /// </remarks>
    public static class Application
    {
        private static readonly Lazy<IQtApplication> LazyInstance =
            new(Adapter.QtApplication, isThreadSafe: true);
        private static IQtApplication _instanceOverride;
        internal static IQtApplication InstanceOverride { set => _instanceOverride = value; }
        private static IQtApplication Instance => _instanceOverride ?? LazyInstance.Value;

        /// <summary>Sets the internal application name.</summary>
        /// <remarks>
        /// Use this as the stable programmatic name of your app. If not set, the executable name
        /// will be used as the default.
        /// </remarks>
        public static string Name
        {
            set => Instance.SetName(value ?? "");
        }

        /// <summary>Sets the application version.</summary>
        /// <remarks>
        /// Use the same version string you would show in an About dialog or diagnostic output.
        /// Qt Bridge for C# passes it through to Qt, which can surface it in places such as
        /// version reporting and settings metadata.
        /// </remarks>
        public static string Version
        {
            set => Instance.SetVersion(value ?? "");
        }

        /// <summary>Sets the publisher or company name for the application.</summary>
        /// <remarks>
        /// This helps Qt Bridge for C# provide publisher information to Qt, which uses it when
        /// choosing where app settings are stored on the current platform. A company or product
        /// owner name is usually the right value here.
        /// </remarks>
        public static string OrganizationName
        {
            set => Instance.SetOrganizationName(value ?? "");
        }

        /// <summary>Sets the publisher domain for the application.</summary>
        /// <remarks>
        /// This is mainly useful on platforms like macOS, where the domain is part of the app
        /// identity. Example: <c>acme.com</c>. Some platforms may ignore this value.
        /// </remarks>
        public static string OrganizationDomain
        {
            set => Instance.SetOrganizationDomain(value ?? "");
        }

        /// <summary>Sets the user-facing application name.</summary>
        /// <remarks>
        /// Use this when the name shown to users should differ from <see cref="Name"/>. If not
        /// set, the <see cref="Name"/> value will be used.
        /// </remarks>
        public static string DisplayName
        {
            set => Instance.SetDisplayName(value ?? "");
        }

        /// <summary>Sets the default window icon from a packaged app resource.</summary>
        /// <param name="qrcUrl">
        /// The resource URL. Use the Qt resource form, for example:
        /// <c>qrc:/assemblies/MyApp/icons/app.svg</c>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="qrcUrl"/> does not start with <c>qrc:/</c>.
        /// </exception>
        /// <remarks>
        /// Sets the default icon for all windows that do not specify their own. Provide the icon
        /// as a resource (e.g., <c>qrc:/assemblies/MyApp/icons/app.png</c>). PNG is recommended
        /// for broad compatibility; SVG is supported if the environment allows it.
        /// </remarks>
        public static void SetWindowIcon(string qrcUrl)
        {
            Resources.ValidateQrcUri(qrcUrl, nameof(qrcUrl));
            Instance.SetWindowIcon(qrcUrl);
        }
    }
}
