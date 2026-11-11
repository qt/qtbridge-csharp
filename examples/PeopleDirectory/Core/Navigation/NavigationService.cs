// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using CommunityToolkit.Mvvm.ComponentModel;
using PeopleDirectory.Core.ViewModels;

namespace PeopleDirectory.Core.Navigation;

public sealed class NavigationService : ObservableObject, INavigationService
{
    private ViewModelBase current;

    public ViewModelBase Current
    {
        get => current;
        private set => SetProperty(ref current, value);
    }

    public void NavigateTo(ViewModelBase viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        Current = viewModel;
    }
}
