// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Runtime.InteropServices;
using System.Text;
using Qt.DotNet;

namespace Qt
{
    /// <summary> Provides access to app resources packaged with Qt Bridge for C#. </summary>
    /// <remarks>
    /// Use this class when C# code needs to read a non-code file that ships with the app, such as
    /// an image, icon, font, text file, or JSON file. Qt Bridge packages these files into the app
    /// and gives each one a stable <c>qrc:/</c> URL. QML can use the same URL directly, and C# can
    /// read the file content through this class.
    /// <para>
    /// The default URL shape is <c>qrc:/assemblies/&lt;AssemblyId&gt;/&lt;relative-path&gt;</c>.
    /// For example, a project named <c>MyApp</c> that includes <c>icons/app.svg</c> usually reads
    /// it as <c>qrc:/assemblies/MyApp/icons/app.svg</c>.
    /// </para>
    /// <para>
    /// Add packaged files with <c>&lt;QtResource Include="..." /&gt;</c>. Projects that already use
    /// <c>.resx</c> files can enable <c>QtBridgeResourceLibrary</c> to package file-reference
    /// entries the same way. Standard .NET <c>ResourceManager</c> access is separate and is only
    /// used when a resource is explicitly marked with <c>QtResourceAccess</c>.
    /// </para>
    /// <para>
    /// All methods in this class validate that the URL starts with <c>qrc:/</c>.
    /// </para>
    /// </remarks>
    public static class Resources
    {
        private static readonly Lazy<IQtResources> LazyInstance =
            new(Adapter.QtResources, isThreadSafe: true);
        private static IQtResources _instanceOverride;
        internal static IQtResources InstanceOverride { set => _instanceOverride = value; }
        private static IQtResources Instance => _instanceOverride ?? LazyInstance.Value;

        /// <summary>
        /// Returns <see langword="true"/> if a resource exists at the given <c>qrc:/</c> URL.
        /// </summary>
        /// <param name="qrcUrl">The resource URL. Must start with <c>qrc:/</c>.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="qrcUrl"/> does not start with <c>qrc:/</c>.
        /// </exception>
        /// <remarks>
        /// Use this method before optional resource reads:
        /// <code language="csharp"><![CDATA[
        /// if (Resources.Exists("qrc:/assemblies/MyApp/help/welcome.html")) {
        ///     string html = Resources.ReadAllText("qrc:/assemblies/MyApp/help/welcome.html");
        /// }
        /// ]]></code>
        /// </remarks>
        public static bool Exists(string qrcUrl)
        {
            ValidateQrcUri(qrcUrl, nameof(qrcUrl));
            return Instance.Exists(qrcUrl);
        }

        /// <summary>
        /// Returns the byte length of the resource, or <c>-1</c> if it does not exist.
        /// </summary>
        /// <param name="qrcUrl">The resource URL. Must start with <c>qrc:/</c>.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="qrcUrl"/> does not start with <c>qrc:/</c>.
        /// </exception>
        /// <remarks>
        /// A return value of <c>-1</c> means the resource could not be opened:
        /// <code language="csharp"><![CDATA[
        /// long bytes = Resources.Size("qrc:/assemblies/MyApp/data/catalog.bin");
        /// if (bytes >= 0) {
        ///     Console.WriteLine($"Catalog size: {bytes} bytes");
        /// }
        /// ]]></code>
        /// </remarks>
        public static long Size(string qrcUrl)
        {
            ValidateQrcUri(qrcUrl, nameof(qrcUrl));
            return Instance.Size(qrcUrl);
        }

        /// <summary>
        /// Reads the full content of a <c>qrc:/</c> resource into a new byte array.
        /// </summary>
        /// <param name="qrcUrl">The resource URL. Must start with <c>qrc:/</c>.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="qrcUrl"/> does not start with <c>qrc:/</c>.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// The resource was not found in the packaged app resources.
        /// </exception>
        /// <exception cref="IOException">
        /// The native read returned fewer bytes than expected.
        /// </exception>
        /// <remarks>
        /// Use this method for binary resources such as images, fonts, and data files when the
        /// consuming C# API expects bytes or streams:
        /// <code language="csharp"><![CDATA[
        /// byte[] fontData = Resources.ReadAllBytes(
        ///     "qrc:/assemblies/MyApp/fonts/Inter-Regular.ttf");
        /// using var stream = new MemoryStream(fontData);
        /// ]]></code>
        /// </remarks>
        public static byte[] ReadAllBytes(string qrcUrl)
        {
            ValidateQrcUri(qrcUrl, nameof(qrcUrl));
            var size = Instance.Size(qrcUrl);
            if (size < 0)
                throw new FileNotFoundException("Packaged resource not found.", qrcUrl);
            if (size == 0)
                return [];

            var buffer = new byte[size];
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try {
                var read = Instance.Read(qrcUrl, handle.AddrOfPinnedObject(), size);
                if (read != size)
                    throw new IOException($"Read returned {read} bytes; expected {size}.");
                return buffer;
            } finally {
                handle.Free();
            }
        }

        /// <summary>
        /// Reads the full content of a <c>qrc:/</c> resource and decodes it as text.
        /// </summary>
        /// <param name="qrcUrl">The resource URL. Must start with <c>qrc:/</c>.</param>
        /// <param name="encoding">
        /// The text encoding to use. Defaults to <see cref="Encoding.UTF8"/> when
        /// <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="qrcUrl"/> does not start with <c>qrc:/</c>.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// The resource was not found in the packaged app resources.
        /// </exception>
        /// <remarks>
        /// The default encoding is UTF-8. Pass an explicit encoding when the resource uses a
        /// different text encoding:
        /// <code language="csharp"><![CDATA[
        /// string json = Resources.ReadAllText("qrc:/assemblies/MyApp/config/appsettings.json");
        /// string legacyText = Resources.ReadAllText(
        ///     "qrc:/assemblies/MyApp/text/legacy.txt",
        ///     Encoding.GetEncoding("windows-1252"));
        /// ]]></code>
        /// </remarks>
        public static string ReadAllText(string qrcUrl, Encoding encoding = null)
        {
            var bytes = ReadAllBytes(qrcUrl);
            return (encoding ?? Encoding.UTF8).GetString(bytes);
        }

        internal static void ValidateQrcUri(string url, string paramName)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("qrc:/", StringComparison.Ordinal))
                throw new ArgumentException("URL must start with 'qrc:/'.", paramName);
        }
    }
}
