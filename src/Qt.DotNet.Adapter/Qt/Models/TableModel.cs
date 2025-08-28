/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Qt.DotNet;

namespace Qt.DotNet
{
    public abstract class TableModel : Model
    {
        public sealed override ModelIndex Index(int row, int column, ModelIndex parent)
        {
            throw new NotImplementedException();
        }

        public sealed override ModelIndex Sibling(int row, int column, ModelIndex index)
        {
            throw new NotImplementedException();
        }

        public sealed override ModelIndex Parent(ModelIndex index)
        {
            throw new NotImplementedException();
        }

        public sealed override bool HasChildren(ModelIndex parent)
        {
            return base.HasChildren(parent);
        }
    }
}
