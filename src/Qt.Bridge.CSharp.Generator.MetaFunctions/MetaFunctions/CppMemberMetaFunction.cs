// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using static Traits;

    public abstract class CppMemberMetaFunction : CppMetaFunction
    {
        protected override bool IsSanitized(Enum traits) => !traits.HasTraits(Src);
    }
}
