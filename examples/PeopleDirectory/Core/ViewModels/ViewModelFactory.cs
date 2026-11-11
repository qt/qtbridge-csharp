// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Microsoft.Extensions.DependencyInjection;
using PeopleDirectory.Core.Models;

namespace PeopleDirectory.Core.ViewModels;

internal sealed class ViewModelFactory(IServiceProvider services) : IViewModelFactory
{
    public LoadingViewModel CreateLoading()
    {
        return services.GetRequiredService<LoadingViewModel>();
    }

    public UsersViewModel CreateUsers()
    {
        return services.GetRequiredService<UsersViewModel>();
    }

    public UserDetailsViewModel CreateUserDetails(User user, UsersViewModel origin)
    {
        return ActivatorUtilities.CreateInstance<UserDetailsViewModel>(services, user, origin);
    }
}
