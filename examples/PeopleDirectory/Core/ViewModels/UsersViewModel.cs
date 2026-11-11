// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PeopleDirectory.Core.Data;
using PeopleDirectory.Core.Models;
using PeopleDirectory.Core.Navigation;

namespace PeopleDirectory.Core.ViewModels;

public sealed partial class UsersViewModel(
    IUserRepository users,
    INavigationService navigation,
    IViewModelFactory factory
) : ViewModelBase
{
    private readonly IUserRepository users = users;
    private readonly INavigationService navigation = navigation;
    private readonly IViewModelFactory factory = factory;
    private IReadOnlyList<User> allUsers = [];
    public ObservableCollection<User> FilteredUsers { get; } = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string errorMessage;

    [ObservableProperty]
    private bool isLoading;

    public string ResultSummary =>
        string.IsNullOrWhiteSpace(SearchText)
            ? $"{FilteredUsers.Count} people"
            : $"{FilteredUsers.Count} of {allUsers.Count} people";

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        try {
            allUsers = await users.GetUsersAsync(30, cancellationToken);
            ApplyFilter();
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            ErrorMessage = ex.Message;
        } finally {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
        ClearSearchCommand.NotifyCanExecuteChanged();
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        var matches = query.Length == 0 ? allUsers : [.. allUsers.Where(x => Matches(x, query))];
        FilteredUsers.Clear();
        foreach (var user in matches)
            FilteredUsers.Add(user);
        OnPropertyChanged(nameof(ResultSummary));
    }

    private static bool Matches(User u, string q)
    {
        return Contains(u.FullName, q)
            || Contains(u.Email, q)
            || Contains(u.Phone, q)
            || Contains(u.City, q)
            || Contains(u.State, q)
            || Contains(u.Country, q)
            || Contains(u.Nationality, q);
    }

    private static bool Contains(string value, string query)
    {
        return value.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    [RelayCommand(CanExecute = nameof(CanClearSearch))]
    public void ClearSearch()
    {
        SearchText = string.Empty;
    }

    public bool CanClearSearch => !string.IsNullOrWhiteSpace(SearchText);

    [RelayCommand]
    public void Retry()
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    public void OpenDetails(User user)
    {
        if (user is not null)
            navigation.NavigateTo(factory.CreateUserDetails(user, this));
    }
}
