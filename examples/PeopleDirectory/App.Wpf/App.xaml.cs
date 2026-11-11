// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PeopleDirectory.Core.Hosting;
using PeopleDirectory.Core.ViewModels;
using PeopleDirectory.Views;

namespace PeopleDirectory;

public partial class AppWpf : Application
{
    private ApplicationHost applicationHost;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        applicationHost = ApplicationHost.Create(e.Args);
        await applicationHost.StartAsync();

        var mainViewModel = applicationHost.Services.GetRequiredService<MainViewModel>();

        MainWindow = new MainWindow(mainViewModel);
        MainWindow.Show();

        await mainViewModel.InitializeAsync();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        if (applicationHost is null)
            return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try {
            await applicationHost.StopAsync(timeout.Token);
        } finally {
            await applicationHost.DisposeAsync();
        }
    }
}
