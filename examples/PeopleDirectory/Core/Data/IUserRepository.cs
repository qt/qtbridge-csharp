// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using PeopleDirectory.Core.Models;

namespace PeopleDirectory.Core.Data;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetUsersAsync(
        int count,
        CancellationToken cancellationToken = default
    );
}
