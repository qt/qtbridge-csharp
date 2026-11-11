// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace PeopleDirectory.Converters;

public sealed class LoadingItemsSourceConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return null;

        var isLoading = values[1] is true;

        return isLoading ? null : values[0] as IEnumerable;
    }

    public object[] ConvertBack(
        object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
