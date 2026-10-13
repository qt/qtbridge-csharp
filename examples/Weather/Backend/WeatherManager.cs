// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OpenMeteo;
using Qt.Quick;

namespace Weather.Backend
{
    public class HourlyEntry
    {
        public required string Time { get; set; }
        public double Temperature { get; set; }
        public double WindSpeed { get; set; }
        public int WeatherCode { get; set; }
        public bool IsNow { get; set; }
    }

    public class DailyEntry
    {
        public required string Day { get; set; }
        public int WeatherCode { get; set; }
        public double TempMax { get; set; }
        public double WindMax { get; set; }
        public double Precipitation { get; set; }
    }

    [QmlElement(Singleton = true)]
    public sealed class WeatherManager : INotifyPropertyChanged
    {
        // Keyless, HTTPS, city-level accuracy from the caller's public IP --
        // as rough as a weather panel needs.
        private const string GeoIpUrl = "https://get.geojs.io/v1/ip/geo.json";
        private static readonly string[] DayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" ];
        private static readonly string[] CompassPoints = [
            "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
            "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"
        ];

        private readonly HttpClient geoIpClient = new();

        // Resolved once by Locate(), then used by every Fetch(). Not exposed
        // to QML -- the panels only ever need the label, not the coordinate.
        private double latitude = 60.1699;
        private double longitude = 24.9384;
        // A Fetch() that arrives while one is already in flight is honoured
        // once the in-flight one lands, rather than lost. Both flags are
        // touched from the Qt GUI thread and from continuation threads, so
        // they are only read and written under fetchGate. Loading stays
        // outside it: it is the QML-visible flag, and notifying under a lock
        // would evaluate QML bindings while holding it.
        private readonly object fetchGate = new();
        private bool fetchInFlight;
        private bool refetchQueued;
        private bool hasForecast;

        // Fallback values shown until the real fetch lands, so the panel is
        // never bound to a set of zeros.
        private double temperature = 18.0;
        private double feelsLike = 17.0;
        private double windGusts = 22.0;
        private int windDirection = 230;
        private double precipitation;
        private int weatherCode = 1;
        private string locationName = "Helsinki, Southern Finland";
        private bool loading;
        private string errorMessage = "";
        private HourlyEntry[] hourlyForecast = [];
        private DailyEntry[] dailyForecast = [];
        private DailyEntry[] dailyForecastUpcoming = [];
        private string dailyForecastSummary = "";
        private string nextRainSummary = "No precipitation expected";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public double Temperature
        {
            get => temperature;
            private set => SetProperty(ref temperature, value);
        }

        public double FeelsLike
        {
            get => feelsLike;
            private set => SetProperty(ref feelsLike, value);
        }

        public double WindGusts
        {
            get => windGusts;
            private set => SetProperty(ref windGusts, value);
        }

        public int WindDirection
        {
            get => windDirection;
            private set => SetProperty(ref windDirection, value);
        }

        public double Precipitation
        {
            get => precipitation;
            private set => SetProperty(ref precipitation, value);
        }

        public int WeatherCode
        {
            get => weatherCode;
            private set => SetProperty(ref weatherCode, value);
        }

        public string LocationName
        {
            get => locationName;
            private set => SetProperty(ref locationName, value);
        }

        public bool Loading
        {
            get => loading;
            private set => SetProperty(ref loading, value);
        }

        public string ErrorMessage
        {
            get => errorMessage;
            private set => SetProperty(ref errorMessage, value);
        }

        public HourlyEntry[] HourlyForecast
        {
            get => hourlyForecast;
            private set => SetProperty(ref hourlyForecast, value);
        }

        public DailyEntry[] DailyForecast
        {
            get => dailyForecast;
            private set => SetProperty(ref dailyForecast, value);
        }

        // DailyForecast with "Today" dropped. The arrays reach QML as list
        // models, read through delegates, so anything derived from the series
        // is computed here -- as are the two summaries below.
        public DailyEntry[] DailyForecastUpcoming
        {
            get => dailyForecastUpcoming;
            private set => SetProperty(ref dailyForecastUpcoming, value);
        }

        // Pre-computed here for the same reason as DailyForecastUpcoming above.
        public string DailyForecastSummary
        {
            get => dailyForecastSummary;
            private set => SetProperty(ref dailyForecastSummary, value);
        }

        public string NextRainSummary
        {
            get => nextRainSummary;
            private set => SetProperty(ref nextRainSummary, value);
        }

        public string WindDirectionLabel(int degrees)
        {
            var normalized = (degrees % 360 + 360) % 360;
            return CompassPoints[(int)Math.Round(normalized / 22.5) % 16];
        }

        public string WeatherDescription(int code)
        {
            return code switch
            {
                0 => "Clear sky",
                1 => "Mainly clear",
                2 => "Partly cloudy",
                3 => "Overcast",
                <= 48 => "Foggy",
                <= 55 => "Drizzle",
                <= 65 => "Rainy",
                <= 67 => "Freezing rain",
                <= 75 => "Snowy",
                <= 77 => "Snow grains",
                <= 82 => "Rain showers",
                <= 86 => "Snow showers",
                <= 99 => "Thunderstorm",
                _ => "Unknown"
            };
        }

        public string WeatherEmoji(int code)
        {
            return code switch
            {
                0 => "☀️",
                <= 2 => "🌤️",
                3 => "☁️",
                <= 48 => "🌫️",
                <= 55 => "🌦️",
                <= 67 => "🌧️",
                <= 77 => "❄️",
                <= 82 => "🌧️",
                <= 86 => "🌨️",
                _ => "⛈️"
            };
        }

        // IP geolocation lookup. Called once at startup from Main.qml,
        // alongside Fetch() -- whichever lands first, Fetch() re-runs once
        // Locate() also lands so the forecast follows the resolved location.
        public void Locate() => _ = LocateAsync();

        // Fetches current conditions plus hourly/daily forecast for the
        // current latitude/longitude through the Open-Meteo OpenAPI client.
        // Called once at startup from Main.qml (see also Locate(), which
        // re-triggers this once a resolved location lands).
        public void Fetch() => _ = FetchAsync();

        private async Task LocateAsync()
        {
            string body;
            try {
                // HTTP/1.1 is mandatory here: over h2 this endpoint never
                // delivers a byte and the request just times out. The
                // forecast client is unaffected -- that host is fine over h2.
                using var request = new HttpRequestMessage(HttpMethod.Get, GeoIpUrl);
                request.Version = System.Net.HttpVersion.Version11;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
                using var response = await geoIpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) {
                    await Console.Error.WriteLineAsync($"[weather] geolocation request failed: "
                      + $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                    return;
                }
                body = await response.Content.ReadAsStringAsync();
            } catch (Exception ex) {
                await Console.Error.WriteLineAsync($"[weather] geolocation request failed: {ex.Message}");
                return;
            }

            JsonDocument json;
            try {
                json = JsonDocument.Parse(body);
            } catch (JsonException ex) {
                await Console.Error.WriteLineAsync($"[weather] geolocation response was not JSON: {ex.Message}");
                return;
            }
            using (json) {
                var root = json.RootElement;
                if (!TryParseCoord(root, "latitude", out var lat) ||
                    !TryParseCoord(root, "longitude", out var lon) ||
                    !Plausible(lat, lon)) {
                    return;
                }
                latitude = lat;
                longitude = lon;

                // Same "City, Region" shape as the hardcoded fallback.
                // Either half may be absent, and the name is only a label,
                // so an empty result keeps the fallback name.
                var city = root.TryGetProperty("city", out var cityEl) ? cityEl.GetString()?.Trim() ?? "" : "";
                var region = root.TryGetProperty("region", out var regionEl) ? regionEl.GetString()?.Trim() ?? "" : "";
                var parts = new List<string>();
                if (city.Length > 0) parts.Add(city);
                if (region.Length > 0 && region != city) parts.Add(region);
                if (parts.Count > 0) LocationName = string.Join(", ", parts);
            }

            Fetch();
        }

        private async Task FetchAsync()
        {
            lock (fetchGate) {
                if (fetchInFlight) {
                    refetchQueued = true;
                    return;
                }
                fetchInFlight = true;
            }
            // The gate stays held across rounds, so a queued refetch is served
            // by another pass here. Releasing it and calling Fetch() again
            // would let a concurrent caller claim the gate in between and push
            // this refetch behind a request it should have replaced.
            while (true) {
                await FetchOnceAsync();
                lock (fetchGate) {
                    if (!refetchQueued) {
                        fetchInFlight = false;
                        return;
                    }
                    refetchQueued = false;
                }
            }
        }

        private OpenMeteoClient WeatherClient { get; } = new();

        private WeatherForecastOptions WeatherOptions = new()
        {
            Current = new()
            {
                CurrentOptionsParameter.apparent_temperature,
                CurrentOptionsParameter.precipitation,
                CurrentOptionsParameter.temperature_2m,
                CurrentOptionsParameter.weathercode,
                CurrentOptionsParameter.winddirection_10m,
                CurrentOptionsParameter.windgusts_10m
            },
            Hourly = new()
            {
                HourlyOptionsParameter.temperature_2m,
                HourlyOptionsParameter.weathercode,
                HourlyOptionsParameter.windspeed_10m
            },
            Daily = new()
            {
                DailyOptionsParameter.precipitation_sum,
                DailyOptionsParameter.temperature_2m_max,
                DailyOptionsParameter.weathercode,
                DailyOptionsParameter.windspeed_10m_max
            },
            Temperature_Unit = TemperatureUnitType.celsius,
            Precipitation_Unit = PrecipitationUnitType.mm,
            Windspeed_Unit = WindspeedUnitType.kmh,
            Start_date = DateTime.Today.ToString("yyyy-MM-dd"),
            End_date = DateTime.Today.AddDays(9).ToString("yyyy-MM-dd"),
            Timezone = "auto"
        };

        private async Task FetchOnceAsync()
        {
            Loading = true;
            ErrorMessage = "";

            try {

                WeatherOptions.Latitude = (float)latitude;
                WeatherOptions.Longitude = (float)longitude;
                var weatherForecast = await WeatherClient.QueryAsync(WeatherOptions);

                Temperature = weatherForecast?.Current?.Temperature_2m ?? float.NaN;
                FeelsLike = weatherForecast?.Current?.Apparent_temperature ?? float.NaN;
                WindGusts = weatherForecast?.Current?.Windgusts_10m ?? float.NaN;
                WindDirection = weatherForecast?.Current?.Winddirection_10m ?? -1;
                Precipitation = weatherForecast?.Current?.Precipitation ?? float.NaN;
                WeatherCode = weatherForecast?.Current?.Weathercode ?? -1;

                // Align the hourly slice to the current hour.
                var times = weatherForecast?.Hourly?.Time?.ToList() ?? [];
                var temps = weatherForecast?.Hourly?.Temperature_2m
                    ?.Select(x => x.HasValue ? x.Value : float.NaN)
                    ?.ToList() ?? [];
                var winds = weatherForecast?.Hourly?.Windspeed_10m
                    ?.Select(x => x.HasValue ? x.Value : float.NaN)
                    ?.ToList() ?? [];
                var codes = weatherForecast?.Hourly?.Weathercode
                    ?.Select(x => x.HasValue ? x.Value : -1)
                    ?.ToList() ?? [];

                var currentHourPrefix = HourPrefix(weatherForecast?.Current?.Time);
                var start = 0;
                var aligned = false;
                if (currentHourPrefix.Length > 0) {
                    var match = times.
                        FindIndex(t => string.CompareOrdinal(HourPrefix(t), currentHourPrefix) >= 0);
                    if (match >= 0) {
                        start = match;
                        aligned = HourPrefix(times[match]) == currentHourPrefix;
                    }
                }
                var end = Math.Min(start + 24, times.Count);

                var hourlyEntries = new List<HourlyEntry>();
                for (var i = start; i < end; i++) {
                    hourlyEntries.Add(new HourlyEntry {
                        Time = times[i].Length >= 11 ? times[i][11..] : times[i],
                        Temperature = i < temps.Count ? (double)temps[i] : 0.0,
                        WindSpeed = i < winds.Count ? (double)winds[i] : 0.0,
                        WeatherCode = i < codes.Count ? codes[i] : 0,
                        IsNow = aligned && i == start
                    });
                }
                HourlyForecast = hourlyEntries.ToArray();

                var dailyTimes = weatherForecast?.Daily?.Time ?? [];
                var dailyCodes = weatherForecast?.Daily?.Weathercode
                    ?.Select(x => Convert.ToInt32(x))
                    ?.ToList() ?? [];
                var dailyMax = weatherForecast?.Daily?.Temperature_2m_max
                    ?.ToList() ?? [];
                var dailyWindMax = weatherForecast?.Daily?.Windspeed_10m_max
                    ?.ToList() ?? [];
                var dailyPrecip = weatherForecast?.Daily?.Precipitation_sum
                    ?.ToList() ?? [];

                var dailyEntries = dailyTimes.Select((t, i) => new DailyEntry
                {
                    Day = DayLabel(i, t),
                    WeatherCode = i < dailyCodes.Count ? dailyCodes[i] : 0,
                    TempMax = i < dailyMax.Count ? (double) dailyMax[i] : 0.0,
                    WindMax = i < dailyWindMax.Count ? (double) dailyWindMax[i] : 0.0,
                    Precipitation = i < dailyPrecip.Count ? (double) dailyPrecip[i] : 0.0
                })
                .ToList();
                DailyForecast = dailyEntries.ToArray();
                DailyForecastUpcoming = dailyEntries.Skip(1).ToArray();
                DailyForecastSummary = ComputeDailyForecastSummary(dailyEntries);
                NextRainSummary = ComputeNextRainSummary(dailyEntries);

                hasForecast = true;
                Loading = false;
            } catch (Exception ex) {
                Loading = false;
                ErrorMessage = hasForecast
                    ? "Unable to refresh weather."
                    : "Unable to load weather. Showing demo data for Helsinki.";
                await Console.Error.WriteLineAsync($"[weather] forecast request failed: {ex.Message}");
            }
        }

        private static string HourPrefix(string? isoTime) =>
            isoTime is { Length: >= 13 } ? isoTime[..13] : "";

        private static string DayLabel(int index, string? isoDate)
        {
            if (index == 0) return "Today";
            if (DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
                return DayNames[(int)date.DayOfWeek];
            }
            Console.Error.WriteLine($"[weather] unparseable forecast date: '{isoDate}'");
            return isoDate ?? "";
        }

        // Picks the most common weather description across the week.
        private string ComputeDailyForecastSummary(List<DailyEntry> forecast)
        {
            if (forecast.Count == 0) return "";
            var counts = new Dictionary<int, int>();
            foreach (var entry in forecast) {
                counts[entry.WeatherCode] = counts.GetValueOrDefault(entry.WeatherCode) + 1;
            }
            var dominant = counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First();
            var description = WeatherDescription(dominant.Key);
            return dominant.Value > forecast.Count / 2
                ? $"{description} conditions all week."
                : $"{description} conditions on {dominant.Value} of {forecast.Count} days.";
        }

        private static string ComputeNextRainSummary(List<DailyEntry> forecast)
        {
            for (var i = 1; i < forecast.Count; i++) {
                if (forecast[i].Precipitation > 0) {
                    return $"Next expected is {forecast[i].Precipitation:F0} mm on {forecast[i].Day}";
                }
            }
            return "No precipitation expected";
        }

        private static bool TryParseCoord(JsonElement root, string name, out double value)
        {
            value = 0;
            if (!root.TryGetProperty(name, out var el)) return false;
            return el.ValueKind switch
            {
                // geojs.io sends coordinates as JSON strings, but a format
                // change to numbers would parse just as well, so both are
                // accepted.
                JsonValueKind.String => double.TryParse(el.GetString(), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out value),
                JsonValueKind.Number => el.TryGetDouble(out value),
                _ => false
            };
        }

        // (0, 0) is in the Atlantic and is what a provider returns when it
        // has no idea, so treat it as a failure rather than as a location.
        private static bool Plausible(double lat, double lon)
        {
            if (lat == 0.0 && lon == 0.0) return false;
            return Math.Abs(lat) <= 90.0 && Math.Abs(lon) <= 180.0;
        }
    }
}
