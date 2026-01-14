// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.ComponentModel;
using System.Diagnostics;
using Refit;
using Qt.Quick;

namespace ColorPalette
{
    public class RestService : IQmlElement, INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private HttpClient Client { get; set; }
        public void Dispose() => Client?.Dispose();

        public Uri Url
        {
            get => Client?.BaseAddress;
            set
            {
                if (value == null || value == Client?.BaseAddress)
                    return;
                Client?.Dispose();
                Resources.ForEach(resource => resource.ResetConnection());
                Client = new(new LoggingHandler(new HttpClientHandler())) { BaseAddress = value };
                // reqres.in requires an API-key, see https://reqres.in/signup
                if (value.Host.StartsWith("reqres", StringComparison.OrdinalIgnoreCase))
                    Client.DefaultRequestHeaders.Add("x-api-key", "reqres-free-v1");
                else
                    Client.DefaultRequestHeaders.Remove("x-api-key");
                PropertyChanged?.Invoke(this, new(nameof(Url)));
            }
        }

        public bool SslSupported => true;

        public void QmlClassBegin()
        { }

        private List<AbstractResource> Resources { get; } = new();

        public void QmlComponentComplete(object[] nestedElements)
        {
            foreach (var element in nestedElements) {
                if (element is not AbstractResource resource)
                    continue;
                Resources.Add(resource);
                resource.Service = this;
            }
        }

        internal TApi Create<TApi>()
            where TApi : class
        {
            if (Client == null)
                return null;
            return Refit.RestService.For<TApi>(Client);
        }

        internal string AuthToken
        {
            get
            {
                if (Client == null)
                    return null;
                if (!Client.DefaultRequestHeaders.TryGetValues("token", out var tokenHeaders))
                    return null;
                return tokenHeaders.FirstOrDefault();
            }
            set
            {
                if (Client == null)
                    return;
                Client.DefaultRequestHeaders.Remove("token");
                if (string.IsNullOrEmpty(value))
                    return;
                Client.DefaultRequestHeaders.Add("token", value);
            }
        }

        internal static bool LogRequests { get; set; }

        private class LoggingHandler : DelegatingHandler
        {
            public LoggingHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            { }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancel)
            {
                if (LogRequests && request != null) {
                    Debug.WriteLine("Request:");
                    Debug.WriteLine(request.ToString());
                    if (request.Content != null &&
                        await request.Content.ReadAsStringAsync(cancel) is { Length: > 0 } msg) {
                        Debug.WriteLine(msg);
                    }
                }

                HttpResponseMessage response = await base.SendAsync(request, cancel);

                if (LogRequests && response != null) {
                    Debug.WriteLine("Response:");
                    Debug.WriteLine(response.ToString());
                    if (await response.Content.ReadAsStringAsync(cancel) is { Length: > 0 } msg)
                        Debug.WriteLine(msg);
                }

                return response;
            }
        }
    }
}
