/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Text.Json.Serialization;

namespace ColorPalette
{
    public sealed class UserResource : ResourceData, IResourcePlugin
    {
        [JsonPropertyName("id")]
        public int UserId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }

        public static List<string> Roles =>
            [nameof(UserId), nameof(Email), nameof(FirstName), nameof(LastName), nameof(Avatar)];

        public override object this[string role] => role switch
        {
            nameof(UserId) => UserId,
            nameof(Email) => Email,
            nameof(FirstName) => FirstName,
            nameof(LastName) => LastName,
            nameof(Avatar) => Avatar,
            _ => null
        };
    }
}
