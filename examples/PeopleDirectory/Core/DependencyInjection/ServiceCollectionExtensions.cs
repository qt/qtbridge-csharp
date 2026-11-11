// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Microsoft.Extensions.DependencyInjection;
using PeopleDirectory.Core.Data;
using PeopleDirectory.Core.Navigation;
using PeopleDirectory.Core.ViewModels;

namespace PeopleDirectory.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPeopleDirectoryCore(this IServiceCollection services)
    {
        _ = services
            .AddHttpClient<IUserRepository, RandomUserRepository>(client =>
            {
                client.BaseAddress = new Uri("https://randomuser.me/api/1.4/");
                client.Timeout = TimeSpan.FromSeconds(15);
            })
            .AddStandardResilienceHandler();

        _ = services.AddSingleton<INavigationService, NavigationService>();
        _ = services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        _ = services.AddSingleton<MainViewModel>();
        _ = services.AddTransient<LoadingViewModel>();
        _ = services.AddTransient<UsersViewModel>();

        return services;
    }
}
