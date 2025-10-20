/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Text.Json.Serialization;

namespace ColorPalette
{
    public sealed class ColorResourceFactory
    {
        public ColorResource Create() => new ColorResource();
    }

    public sealed class ColorResource : ResourceData, IResourcePlugin
    {
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int ColorId { get; set; }

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Name { get; set; }

        [JsonPropertyName("year")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Year { get; set; }

        [JsonPropertyName("color")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Color { get; set; }

        [JsonPropertyName("pantone_value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Pantone { get; set; }

        public static List<string> Roles =>
            [nameof(ColorId), nameof(Name), nameof(Year), nameof(Color), nameof(Pantone)];

        public override object this[string role] => role switch
        {
            nameof(ColorId) => ColorId,
            nameof(Name) => Name,
            nameof(Year) => Year,
            nameof(Color) => Color,
            nameof(Pantone) => Pantone,
            _ => null
        };

        public override void Add(PaginatedResource resPg)
        {
            resPg.Add(this);
        }

        public override void Update(PaginatedResource resPg)
        {
            resPg.Update(ColorId, new ColorResource()
            {
                Name = Name, Year = Year, Color = Color, Pantone = Pantone
            });
        }
    }
}
