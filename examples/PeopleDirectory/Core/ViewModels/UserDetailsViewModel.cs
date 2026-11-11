// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PeopleDirectory.Core.Models;
using PeopleDirectory.Core.Navigation;

namespace PeopleDirectory.Core.ViewModels;

public sealed partial class UserDetailsViewModel(
    INavigationService navigation,
    User user,
    UsersViewModel origin
) : ViewModelBase
{
    private readonly INavigationService navigation = navigation;
    private readonly UsersViewModel origin = origin;

    [ObservableProperty]
    private User user = user;

    [RelayCommand]
    public void Back()
    {
        navigation.NavigateTo(origin);
    }
}
