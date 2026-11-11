// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.ComponentModel;
using PeopleDirectory.Core.Navigation;

namespace PeopleDirectory.Core.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService navigation;
    private readonly IViewModelFactory factory;

    public ViewModelBase Current => navigation.Current;

    public string Title => "People" + Current switch
    {
        UsersViewModel => " - Directory",
        UserDetailsViewModel vm => " - Directory - " + vm.User.FullName,
        _ => ""
    };

    public MainViewModel(INavigationService navigation, IViewModelFactory factory)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(factory);
        this.navigation = navigation;
        this.factory = factory;
        navigation.PropertyChanged += OnNavigationPropertyChanged;
    }

    private void OnNavigationPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or { Length: 0 } or nameof(INavigationService.Current)) {
            OnPropertyChanged(nameof(Current));
            OnPropertyChanged(nameof(Title));
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        navigation.NavigateTo(factory.CreateLoading());
        var users = factory.CreateUsers();
        await users.LoadAsync(cancellationToken);
        navigation.NavigateTo(users);
    }
}
