/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Text.Json.Serialization;

namespace UserViewLib
{
    public record User(
        [property: JsonPropertyName("name")] UserName Name,
        [property: JsonPropertyName("dob")] UserDateOfBirth Birth,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("picture")] UserPicture Picture)
    {
        public int Age => Birth.Age;
    }

    public record UserName(
        [property: JsonPropertyName("first")] string First,
        [property: JsonPropertyName("last")] string Last)
    {
        public string Full => $"{Last}, {First}";
    }

    public record UserDateOfBirth(
        [property: JsonPropertyName("date")] DateTime Date,
        [property: JsonPropertyName("age")] int Age);

    public record UserPicture(
        [property: JsonPropertyName("thumbnail")] string Thumbnail,
        [property: JsonPropertyName("large")] string Large);
}
