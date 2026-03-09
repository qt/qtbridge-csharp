// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.Mime
{
    /// <summary>
    /// Interface of types mapping to
    /// <see href="https://doc.qt.io/qt-6/qmimedata.html">QMimeData</see>.
    /// </summary>
    /// <remarks>
    /// Currently only serving as a placeholder type for unsupported overrides of
    /// <see cref="Qt.Bridge.Models.Model"/> that deal with drag/drop actions.
    /// </remarks>
    internal interface IMimeData
    {
        bool HasText { get; }
        string Text { get; set; }

        bool HasHtml { get; }
        string Html { get; set; }

        bool HasUrls { get; }
        Uri[] Urls { get; set; }

        bool HasImage { get;}
        object ImageData { get; set; }

        bool HasColor { get; }
        object ColorData { get; set; }

        string[] Formats { get; }
        bool HasFormat(string mimeType);
        void RemoveFormat(string mimeType);
        byte[] this[string mimeType] { get; set; }

        void Clear();
    }
}
