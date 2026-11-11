// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

namespace PeopleDirectory.Core.Models;

public sealed record User(
    Guid Id,
    string FullName,
    string Initials,
    int AvatarColorIndex,
    string Email,
    string Phone,
    string Cell,
    string Gender,
    DateTimeOffset DateOfBirth,
    int Age,
    string Street,
    string City,
    string State,
    string Country,
    string Postcode,
    string Nationality
);
