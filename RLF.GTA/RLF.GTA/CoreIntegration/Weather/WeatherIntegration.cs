using System;
using System.IO;
using GTA;
using GTA.Native;
using RLF.Core;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Logging;
using RLF.Core.Systems;

namespace RLF.GTA.CoreIntegration.Weather
{
    /// <summary>
    /// Integração GTA do clima vindo do Core.
    /// Escuta o evento "weather:changed" e aplica no GTA.
    /// Debug: gera log dedicado WeatherLog.log (sem UI).
    /// Compatível com C# 7.3 (ScriptHookDotNet).
    /// </summary>
    public sealed class WeatherIntegration
    {
        private readonly Logger _logger;
        private readonly EventManager _events;

        private WeatherType _currentApplied;
        private bool _hasApplied;
        private DateTime _lastApplyUtc;

        // Evita spam de aplicação
        private static readonly TimeSpan MinApplyInterval = TimeSpan.FromSeconds(10);

        // ===== DEBUG LOG =====
        private static readonly string DebugDir =
            @"D:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced\scripts\RLF\Debug";

        private static readonly string DebugLogPath =
            Path.Combine(DebugDir, "WeatherLog.log");

        public WeatherIntegration()
        {
            var core = RLFCore.Instance;

            _logger = core.Logger;
            _events = core.EventManager;

            _hasApplied = false;
            _lastApplyUtc = DateTime.MinValue;

            EnsureDebugDirectory();
            WriteDebug("WeatherIntegration inicializada");

            _events.Subscribe("weather:changed", OnWeatherChanged);
            WriteDebug("Escutando evento weather:changed");

            // 🔥 aplica o clima imediatamente ao entrar no jogo
            ApplyInitialWeather();

        }

        /// <summary>
        /// Evento disparado pelo Core quando o clima muda.
        /// </summary>
        private void OnWeatherChanged(object sender, RLFEventArgs e)
        {
            var typed = e as RLFEventArgs<WeatherState>;
            if (typed == null)
                return;

            WriteDebug(
                "Evento recebido -> " +
                typed.Data.Type +
                " | Temp " +
                typed.Data.Temperature.ToString("0.#")
            );

            ApplyWeather(typed.Data.Type);
        }

        private void ApplyWeather(WeatherType type)
        {
            // Proteção contra spam
            if (_hasApplied)
            {
                if (_currentApplied == type &&
                    (DateTime.UtcNow - _lastApplyUtc) < MinApplyInterval)
                {
                    WriteDebug("Ignorado (anti-spam): " + type);
                    return;
                }
            }

            _currentApplied = type;
            _hasApplied = true;
            _lastApplyUtc = DateTime.UtcNow;

            string gtaWeather = MapToGtaWeather(type);

            // Transição suave + persistência
            Function.Call(Hash.SET_WEATHER_TYPE_OVERTIME_PERSIST, gtaWeather, 30f);
            Function.Call(Hash.SET_OVERRIDE_WEATHER, gtaWeather);

            WriteDebug("Clima aplicado no GTA -> " + gtaWeather);
            _logger.Info("WeatherIntegration: Clima aplicado -> " + gtaWeather);
        }

        private static string MapToGtaWeather(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Clear:
                    return "EXTRASUNNY";

                case WeatherType.Clouds:
                    return "CLOUDS";

                case WeatherType.Rain:
                    return "RAIN";

                case WeatherType.Thunder:
                    return "THUNDER";

                case WeatherType.Fog:
                    return "FOGGY";

                default:
                    return "CLEAR";
            }
        }

        /// <summary>
        /// Liberação correta no shutdown.
        /// </summary>
        public void Dispose()
        {
            if (_events != null)
                _events.Unsubscribe("weather:changed", OnWeatherChanged);

            WriteDebug("WeatherIntegration finalizada");
        }

        // ===== DEBUG HELPERS =====

        private static void EnsureDebugDirectory()
        {
            try
            {
                if (!Directory.Exists(DebugDir))
                    Directory.CreateDirectory(DebugDir);
            }
            catch
            {
                // nunca pode quebrar o jogo
            }
        }

        private void ApplyInitialWeather()
        {
            try
            {
                // Pega o sistema de clima direto do Core
                var weatherSystem =
                    RLFCore.Instance.Systems.Get("RealTimeWeatherSystem")
                    as RLF.Core.Systems.RealTimeWeatherSystem;

                if (weatherSystem == null)
                {
                    WriteDebug("ApplyInitialWeather: sistema não encontrado");
                    return;
                }

                // Usa o último clima conhecido do Core
                var field = weatherSystem.GetType().GetField(
                    "_currentWeather",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance
                );

                if (field == null)
                {
                    WriteDebug("ApplyInitialWeather: campo _currentWeather não encontrado");
                    return;
                }

                var weather = (WeatherState)field.GetValue(weatherSystem);

                if (!weather.IsValid)
                {
                    WriteDebug("ApplyInitialWeather: clima inválido");
                    return;
                }

                WriteDebug("Aplicando clima inicial -> " + weather.Type);
                ApplyWeather(weather.Type);
            }
            catch (Exception ex)
            {
                WriteDebug("ApplyInitialWeather: erro " + ex.Message);
            }
        }

        private static void WriteDebug(string message)
        {
            try
            {
                string line =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                    " | " +
                    message;

                File.AppendAllText(DebugLogPath, line + Environment.NewLine);
            }
            catch
            {
                // debug nunca pode quebrar o jogo
            }


        }
    }
}
