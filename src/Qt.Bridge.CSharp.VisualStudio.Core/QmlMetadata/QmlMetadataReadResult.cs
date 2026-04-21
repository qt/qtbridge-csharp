// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata
{
    /// <summary> Classifies why a <c>qtbridge-qml.ide.json</c> read operation failed. </summary>
    public enum QmlMetadataReadError
    {
        /// <summary> No error; the read succeeded. </summary>
        None,
        /// <summary> The file path was empty or the file did not exist on disk. </summary>
        NotFound,
        /// <summary>
        /// The file existed but could not be deserialized as valid metadata JSON, or the
        /// deserialized content failed the required-field checks in the DTO mapping.
        /// </summary>
        ParseError,
        /// <summary> The file existed but an I/O error occurred while reading it. </summary>
        IoError
    }

    /// <summary>
    /// The result of a <see cref="IQmlMetadataReader.TryRead"/> call. On success,
    /// <see cref="Success"/> is <see langword="true"/> and <see cref="Metadata"/> is non-null.
    /// On failure, <see cref="Error"/> identifies the cause and <see cref="Exception"/> carries
    /// the underlying exception when one was raised.
    /// </summary>
    public sealed class QmlMetadataReadResult
    {
        /// <summary>
        /// The deserialized metadata, or <see langword="null"/> when <see cref="Success"/> is
        /// <see langword="false"/>.
        /// </summary>
        public QmlMetadata? Metadata { get; }

        /// <summary>
        /// The failure kind, or <see cref="QmlMetadataReadError.None"/> on success.
        /// </summary>
        public QmlMetadataReadError Error { get; }

        /// <summary>
        /// The underlying exception for <see cref="QmlMetadataReadError.IoError"/> and
        /// <see cref="QmlMetadataReadError.ParseError"/> failures, or <see langword="null"/>
        /// when no exception was raised.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary> The file path that was attempted. </summary>
        public string Path { get; }

        /// <summary>
        /// <see langword="true"/> when the read succeeded and <see cref="Metadata"/> is non-null.
        /// </summary>
        public bool Success => Error == QmlMetadataReadError.None;

        private QmlMetadataReadResult(
            QmlMetadata? metadata,
            QmlMetadataReadError error,
            string path,
            Exception? exception)
        {
            Metadata = metadata;
            Error = error;
            Path = path;
            Exception = exception;
        }

        /// <summary> Creates a successful result carrying the deserialized metadata. </summary>
        public static QmlMetadataReadResult Ok(QmlMetadata metadata, string path) =>
            new(metadata, QmlMetadataReadError.None, path, null);

        /// <summary>
        /// Creates a failure result with the given error kind and optional exception.
        /// </summary>
        public static QmlMetadataReadResult Fail(
            QmlMetadataReadError error,
            string path,
            Exception? exception = null) => new(null, error, path, exception);
    }
}
