// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Microsoft.Extensions.Hosting;
using PeopleDirectory.Core.DependencyInjection;

namespace PeopleDirectory.Core.Hosting;

public sealed class ApplicationHost : IApplicationHost
{
    private readonly IHost host;

    private ApplicationHost(IHost host)
    {
        this.host = host;
    }

    public IServiceProvider Services => host.Services;

    public static ApplicationHost Create(string[] args = null)
    {
        var builder = Host.CreateApplicationBuilder(args ?? []);
        _ = builder.Services.AddPeopleDirectoryCore();
        return new ApplicationHost(builder.Build());
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return host.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return host.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (host is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(true);
        else
            host.Dispose();
    }
}
