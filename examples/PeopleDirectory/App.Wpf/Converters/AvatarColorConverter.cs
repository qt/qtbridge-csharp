// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PeopleDirectory.Converters;

public sealed class AvatarColorConverter : IValueConverter
{
    private static readonly Brush[] Palette =
    [
        CreateBrush(37, 99, 235),
        CreateBrush(124, 58, 237),
        CreateBrush(219, 39, 119),
        CreateBrush(220, 38, 38),
        CreateBrush(234, 88, 12),
        CreateBrush(5, 150, 105),
        CreateBrush(8, 145, 178),
        CreateBrush(71, 85, 105),
    ];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var index = value is int number ? number : 0;

        return Palette[(index & int.MaxValue) % Palette.Length];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));

        brush.Freeze();
        return brush;
    }
}
