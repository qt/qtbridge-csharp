// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata
{
    /// <summary>
    /// Watches the file system for creation or modification of the <c>qtbridge-qml.ide.json</c>
    /// metadata file and notifies callers so the extension can re-evaluate server startup.
    /// </summary>
    public interface IQmlMetadataWatcher
    {
        /// <summary>
        /// Watches for creation or modification of the metadata file under the project's obj
        /// directory whose immediate parent matches the given configuration name.
        /// <para>
        /// The callback is invoked on a background thread with a short delay applied. Dispose
        /// the returned handle to stop watching.
        /// </para>
        /// </summary>
        IDisposable Watch(string projectDirectory, string configuration, Action metadataAction);
    }
}
