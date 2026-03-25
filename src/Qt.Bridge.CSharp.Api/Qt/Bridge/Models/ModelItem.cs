// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.DotNet;

namespace Qt.Bridge.Models
{
    public interface IModelItem
    {
        bool IsEnabled { get; }
        bool IsSelectable { get; }
    }

    public interface IDisplayable
    {
        object DisplayValue { get; }
    }

    public interface IEditable
    {
        bool IsEditable { get; }
        object EditValue { get; set; }
    }
}
