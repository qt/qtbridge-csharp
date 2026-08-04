// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include <builtin_types.h>

namespace QtDotNet {
    QObject *objectDispatch(QDotNetRef &args, const QObject *context = nullptr);
}
