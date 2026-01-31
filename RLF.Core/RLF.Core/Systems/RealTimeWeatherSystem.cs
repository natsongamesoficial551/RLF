using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Logging;
using RLF.Core.Systems;
using RLF.Core.Utilities;

namespace RLF.Core.Systems
{
    /// <summary>
    /// Sistema de clima em tempo real (cidade real) usando Open-Meteo.
    /// Compatível com C# 7.3 / .NET Framework (sem System.Text.Json, sem Random.Shared, sem "using var").
    /// GTA-SAFE: NÃO chama GTA diretamente. Publica evento "weather:changed".
    ///
    /// Estratégia de localização:
    /// 1) Se [Weather] Latitude/Longitude estiverem definidos no RLF.ini -> usa fixo.
    /// 2) (Opcional) IP Geolocation (ipapi.co) -> pega lat/lon reais do player (global).
    /// 3) Fallback -> aproximação por TimeZone (bem simples).
    /// 4) Se tudo falhar -> fallback offline (estatístico).
    /// </summary>
    public sealed class RealTimeWeatherSystem : SystemBase
    {
        #region Config Defaults

        private const int DefaultUpdateIntervalMinutes = 15;
        private const int FallbackUpdateIntervalMinutes = 30;

        private const int HttpTimeoutMs = 6000;

        #endregion

        #region State

        private DateTime _lastUpdateUtc;

        private WeatherState _currentWeather;
        private bool _hasValidWeather;

        // Config carregada do INI (opcional)
        private int _updateIntervalMinutes;
        private bool _offlineFallbackEnabled;
        private bool _useIpGeolocation;

        private bool _hasFixedCoordinates;
        private double _fixedLat;
        private double _fixedLon;

        #endregion

        public RealTimeWeatherSystem(Logger logger, EventManager eventManager)
            : base("RealTimeWeatherSystem", logger, eventManager, tickRate: 60) // checa 1x por segundo (leve)
        {
            _lastUpdateUtc = DateTime.MinValue;
            _hasValidWeather = false;

            // defaults
            _updateIntervalMinutes = DefaultUpdateIntervalMinutes;
            _offlineFallbackEnabled = true;
            _useIpGeolocation = true;

            _hasFixedCoordinates = false;
            _fixedLat = 0.0;
            _fixedLon = 0.0;
        }

        protected override void OnStart()
        {
            LoadConfig();

            Logger.Info(
                $"{Name}: iniciado (Open-Meteo | Hora local | Interval={_updateIntervalMinutes}min | IPGeo={_useIpGeolocation} | FixedCoords={_hasFixedCoordinates})"
            );

            TryUpdateWeather(force: true);
        }

        protected override void OnStop()
        {
            Logger.Info($"{Name}: parado");
            _hasValidWeather = false;
        }

        protected override void OnTick()
        {
            if (ShouldUpdate())
                TryUpdateWeather(force: false);
        }

        private void LoadConfig()
        {
            try
            {
                var core = RLFCore.Instance;
                if (core == null || core.Config == null)
                    return;

                _updateIntervalMinutes = core.Config.GetInt("Weather", "UpdateIntervalMinutes", DefaultUpdateIntervalMinutes);
                if (_updateIntervalMinutes < 5) _updateIntervalMinutes = 5;
                if (_updateIntervalMinutes > 120) _updateIntervalMinutes = 120;

                _offlineFallbackEnabled = core.Config.GetBool("Weather", "OfflineFallback", true);
                _useIpGeolocation = core.Config.GetBool("Weather", "UseIpGeolocation", true);

                // Coordenadas fixas (opcional)
                // Aceita float no ini, mas armazena em double
                float lat = core.Config.GetFloat("Weather", "Latitude", 9999f);
                float lon = core.Config.GetFloat("Weather", "Longitude", 9999f);

                if (lat >= -90f && lat <= 90f && lon >= -180f && lon <= 180f)
                {
                    _fixedLat = lat;
                    _fixedLon = lon;
                    _hasFixedCoordinates = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"{Name}: falha ao carregar config, usando defaults", ex);
            }
        }

        private bool ShouldUpdate()
        {
            int interval = _hasValidWeather ? _updateIntervalMinutes : FallbackUpdateIntervalMinutes;
            return (DateTime.UtcNow - _lastUpdateUtc).TotalMinutes >= interval;
        }

        private void TryUpdateWeather(bool force)
        {
            _lastUpdateUtc = DateTime.UtcNow;

            SafeExecutor.Execute(
                () =>
                {
                    LocationInfo loc = ResolveLocation();
                    if (!loc.IsValid)
                    {
                        Logger.Warning($"{Name}: falha ao resolver localização (lat/lon).");

                        if (_offlineFallbackEnabled)
                        {
                            ApplyFallbackWeather();
                        }

                        return;
                    }

                    WeatherState weather = FetchWeather(loc);
                    if (!weather.IsValid)
                    {
                        Logger.Warning($"{Name}: falha ao obter clima online (Open-Meteo).");

                        if (_offlineFallbackEnabled)
                        {
                            ApplyFallbackWeather();
                        }

                        return;
                    }

                    ApplyWeather(weather);
                },
                $"{Name}.TryUpdateWeather"
            );
        }

        private LocationInfo ResolveLocation()
        {
            // 1) Coordenadas fixas do ini
            if (_hasFixedCoordinates)
                return new LocationInfo(_fixedLat, _fixedLon);

            // 2) IP Geolocation (global)
            if (_useIpGeolocation)
            {
                LocationInfo ipLoc = LocationResolver.TryResolveFromIp(Logger);
                if (ipLoc.IsValid)
                    return ipLoc;
            }

            // 3) TimeZone approximation
            return LocationResolver.ResolveFromTimeZone();
        }

        private WeatherState FetchWeather(LocationInfo location)
        {
            try
            {
                string lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
                string lon = location.Longitude.ToString(CultureInfo.InvariantCulture);

                string url =
                    "https://api.open-meteo.com/v1/forecast" +
                    "?latitude=" + lat +
                    "&longitude=" + lon +
                    "&current_weather=true";

                string json = HttpUtils.HttpGet(url, HttpTimeoutMs);
                if (string.IsNullOrWhiteSpace(json))
                    return WeatherState.Invalid;

                // Extrai current_weather.weathercode e current_weather.temperature
                // Formato típico:
                // "current_weather":{"temperature":27.3,"windspeed":...,"weathercode":3,...}
                int code;
                float temp;

                if (!JsonMiniParser.TryGetInt(json, "weathercode", out code))
                    return WeatherState.Invalid;

                if (!JsonMiniParser.TryGetFloat(json, "temperature", out temp))
                    temp = 0f;

                return WeatherMapper.FromOpenMeteo(code, temp);
            }
            catch (Exception ex)
            {
                Logger.Warning($"{Name}: erro ao consultar Open-Meteo", ex);
                return WeatherState.Invalid;
            }
        }

        private void ApplyWeather(WeatherState weather)
        {
            if (_hasValidWeather && weather.Equals(_currentWeather))
                return;

            _currentWeather = weather;
            _hasValidWeather = true;

            Logger.Info($"{Name}: Clima atualizado → {weather.Type} | Temp {weather.Temperature.ToString("0.#", CultureInfo.InvariantCulture)}°C");

            Events.Raise(
                "weather:changed",
                new RLFEventArgs<WeatherState>(weather)
            );
        }

        private void ApplyFallbackWeather()
        {
            WeatherState fallback = WeatherMapper.GenerateOffline(DateTime.Now);
            ApplyWeather(fallback);
        }
    }

    #region Weather Models

    public enum WeatherType
    {
        Clear,
        Clouds,
        Rain,
        Thunder,
        Fog
    }

    public struct WeatherState
    {
        public static WeatherState Invalid { get { return new WeatherState(false); } }

        public bool IsValid { get; private set; }
        public WeatherType Type { get; private set; }
        public float Temperature { get; private set; }

        public WeatherState(WeatherType type, float temperature)
        {
            IsValid = true;
            Type = type;
            Temperature = temperature;
        }

        private WeatherState(bool valid)
        {
            IsValid = valid;
            Type = WeatherType.Clear;
            Temperature = 0f;
        }
    }

    #endregion

    #region Mapping & Location

    internal static class WeatherMapper
    {
        private static readonly object _rngLock = new object();
        private static readonly Random _rng = new Random();

        public static WeatherState FromOpenMeteo(int code, float temp)
        {
            WeatherType type;

            if (code == 0)
                type = WeatherType.Clear;
            else if (code >= 1 && code <= 3)
                type = WeatherType.Clouds;
            else if (code >= 45 && code <= 48)
                type = WeatherType.Fog;
            else if (code >= 51 && code <= 67)
                type = WeatherType.Rain;
            else if (code >= 80 && code <= 82)
                type = WeatherType.Rain;
            else if (code >= 95 && code <= 99)
                type = WeatherType.Thunder;
            else
                type = WeatherType.Clouds;

            return new WeatherState(type, temp);
        }

        public static WeatherState GenerateOffline(DateTime localTime)
        {
            int month = localTime.Month;

            // Heurística simples global:
            // - Meses 11..3: "verão" no hemisfério sul (ex: BR) -> mais chuva
            // - Meses 5..9: mais chance de neblina / céu limpo
            bool summerLike = (month >= 11 || month <= 3);
            double roll;

            lock (_rngLock)
            {
                roll = _rng.NextDouble();
            }

            WeatherType type;
            if (summerLike && roll < 0.35)
                type = WeatherType.Rain;
            else if (!summerLike && roll < 0.15)
                type = WeatherType.Fog;
            else if (roll < 0.55)
                type = WeatherType.Clear;
            else
                type = WeatherType.Clouds;

            int t;
            lock (_rngLock)
            {
                t = summerLike ? _rng.Next(24, 35) : _rng.Next(10, 22);
            }

            return new WeatherState(type, (float)t);
        }
    }

    internal struct LocationInfo
    {
        public bool IsValid { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public LocationInfo(double lat, double lon)
        {
            Latitude = lat;
            Longitude = lon;
            IsValid = true;
        }

        public static LocationInfo Invalid
        {
            get { return new LocationInfo(); }
        }
    }

    internal static class LocationResolver
    {
        /// <summary>
        /// Resolve lat/lon por IP (ipapi.co) - global, sem key.
        /// </summary>
        public static LocationInfo TryResolveFromIp(Logger logger)
        {
            try
            {
                string json = HttpUtils.HttpGet("https://ipapi.co/json/", 6000);
                if (string.IsNullOrWhiteSpace(json))
                    return LocationInfo.Invalid;

                // ipapi.co retorna "latitude": -22.90, "longitude": -43.20
                double lat;
                double lon;

                if (!JsonMiniParser.TryGetDouble(json, "latitude", out lat))
                    return LocationInfo.Invalid;

                if (!JsonMiniParser.TryGetDouble(json, "longitude", out lon))
                    return LocationInfo.Invalid;

                if (lat < -90.0 || lat > 90.0 || lon < -180.0 || lon > 180.0)
                    return LocationInfo.Invalid;

                return new LocationInfo(lat, lon);
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.Warning("LocationResolver(IP): falha ao obter localização por IP", ex);

                return LocationInfo.Invalid;
            }
        }

        /// <summary>
        /// Resolve latitude/longitude aproximados usando o TimeZone local.
        /// Fallback simples (não garante cidade real).
        /// </summary>
        public static LocationInfo ResolveFromTimeZone()
        {
            try
            {
                TimeZoneInfo tz = TimeZoneInfo.Local;
                string id = (tz.Id ?? "").ToLowerInvariant();

                // Aproximações globais suficientes para fallback
                if (id.Contains("brazil"))
                    return new LocationInfo(-22.9, -43.2); // Rio base
                if (id.Contains("pacific"))
                    return new LocationInfo(34.0, -118.2); // LA
                if (id.Contains("eastern"))
                    return new LocationInfo(40.7, -74.0); // NY
                if (id.Contains("india"))
                    return new LocationInfo(28.6, 77.2); // Delhi
                if (id.Contains("europe"))
                    return new LocationInfo(48.8, 2.3); // Paris base

                // Fallback global neutro
                return new LocationInfo(0.0, 0.0);
            }
            catch
            {
                return LocationInfo.Invalid;
            }
        }
    }

    #endregion

    #region HTTP + Mini JSON Parser (Framework-friendly)

    internal static class HttpUtils
    {
        public static string HttpGet(string url, int timeoutMs)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            HttpWebRequest req = null;
            HttpWebResponse resp = null;

            try
            {
                req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = timeoutMs;
                req.ReadWriteTimeout = timeoutMs;
                req.UserAgent = "RLF/1.0 (RealTimeWeatherSystem)";

                resp = (HttpWebResponse)req.GetResponse();
                if (resp == null)
                    return null;

                using (var stream = resp.GetResponseStream())
                {
                    if (stream == null)
                        return null;

                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            finally
            {
                if (resp != null)
                    resp.Close();
            }
        }
    }

    internal static class JsonMiniParser
    {
        // Procura por: "key": <number>
        // Funciona para JSON simples (o suficiente pra nossos 3 campos).
        public static bool TryGetInt(string json, string key, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(key))
                return false;

            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)", RegexOptions.IgnoreCase);
            if (!m.Success)
                return false;

            return int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryGetFloat(string json, string key, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(key))
                return false;

            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
            if (!m.Success)
                return false;

            return float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryGetDouble(string json, string key, out double value)
        {
            value = 0.0;
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(key))
                return false;

            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
            if (!m.Success)
                return false;

            return double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }

    #endregion
}
