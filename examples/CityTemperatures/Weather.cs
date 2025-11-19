/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using OpenMeteo;
using Qt.DotNet.Utils;

namespace CityTemperatures
{
    public class Weather : INotifyPropertyChanged
    {
        private LazyFactory lazy = new();

        public Weather()
        {
            lazy.PropertyChanged += OnPropertyChanged;
            StartRefreshLoop();
        }

        public string Location
        {
            get => lazy.Get(() => Location, () => "");
            set => lazy.Set(() => Location, value);
        }

        public bool IsValid
        {
            get => lazy.Get(() => IsValid, () => false);
            set => lazy.Set(() => IsValid, value);
        }

        public double Temperature
        {
            get => lazy.Get(() => Temperature, () => 0.0);
            set => lazy.Set(() => Temperature, value);
        }

        public string TemperatureUnits
        {
            get => lazy.Get(() => TemperatureUnits, () => "");
            set => lazy.Set(() => TemperatureUnits, value);
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => lazy.PropertyChanged += value;
            remove => lazy.PropertyChanged -= value;
        }

        private OpenMeteoClient Client { get; } = new();

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Location))
                Refresh();
        }

        private void StartRefreshLoop()
        {
            Refresh();
            _ = Task.Run(() =>
            {
                var refreshTimer = Stopwatch.StartNew();
                while (true) {
                    Thread.Sleep(100);
                    if (refreshTimer.Elapsed.TotalMinutes >= 2) {
                        Refresh();
                        refreshTimer.Restart();
                    }
                }
            });
        }

        public void Refresh()
        {
            var location = Location;
            if (!string.IsNullOrEmpty(location) && Client.Query(location) is { } result) {
                Temperature = result.Current.Temperature ?? 0;
                TemperatureUnits = result.CurrentUnits.Temperature;
                IsValid = true;
            } else {
                Temperature = 0;
                TemperatureUnits = "";
                IsValid = false;
            }
        }
    }
}
