// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Microsoft.Extensions.DependencyInjection;
using PeopleDirectory.Core.Hosting;
using PeopleDirectory.Core.ViewModels;
using Qt.Quick;

namespace PeopleDirectory;

public sealed class MainWindowArgs : IQmlLoadArgs
{
    public static string Module => "Views";
    public static string Name => "MainWindow";
    public MainViewModel ViewModel { get; set; }
}

[QmlElement(Singleton = true)]
public sealed class ViewModelTypes
{
    public string LoadingViewModel => typeof(LoadingViewModel).ToString();
    public string UsersViewModel => typeof(UsersViewModel).ToString();
    public string UserDetailsViewModel => typeof(UserDetailsViewModel).ToString();
}

internal static class Program
{
    private static ApplicationHost applicationHost;

    internal static async Task Main(string[] args)
    {
        applicationHost = ApplicationHost.Create(args);
        await applicationHost.StartAsync();

        var mainViewModel = applicationHost.Services.GetRequiredService<MainViewModel>();

        Qml.Load(new MainWindowArgs()
        {
            ViewModel = mainViewModel
        });

        await mainViewModel.InitializeAsync();

        Qml.WaitForExit();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try {
            await applicationHost.StopAsync(timeout.Token);
        } finally {
            await applicationHost.DisposeAsync();
        }
    }
}
