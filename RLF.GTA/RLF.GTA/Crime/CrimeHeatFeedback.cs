using System;
using GTA;
using GTA.UI;
using RLF.Core.Crime;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Sistema de feedback visual de Heat para o jogador.
    /// Exibe indicadores visuais do nível de pressão criminal atual sem usar wanted stars.
    /// </summary>
    public class CrimeHeatFeedback
    {
        private readonly CrimeSystem _crimeSystem;

        private HeatState _currentDisplayedState;
        private float _currentDisplayedHeat;
        private DateTime _lastStateChangeTime;
        private bool _isWarningActive;

        private const float UPDATE_INTERVAL = 0.5f;
        private float _updateTimer;

        public bool IsEnabled { get; set; }
        public bool ShowVisualFeedback { get; set; }
        public bool ShowTextFeedback { get; set; }

        public CrimeHeatFeedback(CrimeSystem crimeSystem)
        {
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));

            _currentDisplayedState = HeatState.None;
            _currentDisplayedHeat = 0f;
            _lastStateChangeTime = DateTime.Now;
            _isWarningActive = false;
            _updateTimer = 0f;

            IsEnabled = true;
            ShowVisualFeedback = true;
            ShowTextFeedback = true;

            CrimeEvents.OnHeatChanged += OnHeatChanged;
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            _updateTimer += deltaTime;
            if (_updateTimer < UPDATE_INTERVAL) return;
            _updateTimer = 0f;

            UpdateFeedback();
        }

        private void OnHeatChanged(float newHeat, HeatState newState)
        {
            if (newState != _currentDisplayedState)
            {
                _lastStateChangeTime = DateTime.Now;
                ShowStateChangeNotification(newState);
            }

            _currentDisplayedState = newState;
            _currentDisplayedHeat = newHeat;
        }

        private void UpdateFeedback()
        {
            if (!ShowVisualFeedback && !ShowTextFeedback) return;

            HeatState current = _crimeSystem.CurrentHeatState;
            float heat = _crimeSystem.CurrentHeat;

            if (current == HeatState.None) return;

            if (ShowVisualFeedback)
            {
                ApplyVisualEffects(current, heat);
            }
        }

        private void ShowStateChangeNotification(HeatState newState)
        {
            if (!ShowTextFeedback) return;

            string message = GetStateMessage(newState);
            if (string.IsNullOrEmpty(message)) return;

            Notification.Show(message, true);
        }

        private string GetStateMessage(HeatState state)
        {
            switch (state)
            {
                case HeatState.None:
                    return "~g~Heat cleared";

                case HeatState.Low:
                    return "~y~Low heat - Police may be alerted";

                case HeatState.Medium:
                    return "~o~Medium heat - Police searching area";

                case HeatState.High:
                    return "~r~High heat - Police actively searching for you";

                case HeatState.Critical:
                    return "~r~CRITICAL HEAT - Active manhunt in progress";

                case HeatState.Extreme:
                    return "~r~EXTREME HEAT - Maximum police response";

                default:
                    return string.Empty;
            }
        }

        private void ApplyVisualEffects(HeatState state, float heat)
        {
            if (state >= HeatState.High)
            {
                if (!_isWarningActive || (DateTime.Now - _lastStateChangeTime).TotalSeconds < 2.0)
                {
                    ApplyScreenEffect(state);
                }
            }
        }

        private void ApplyScreenEffect(HeatState state)
        {
            _isWarningActive = true;
        }

        public void ForceShowCurrentHeat()
        {
            float heat = _crimeSystem.CurrentHeat;
            HeatState state = _crimeSystem.CurrentHeatState;

            string message = $"Current Heat: {(heat * 100f):F0}% - State: {state}";
            Notification.Show(message, false);
        }

        public void Shutdown()
        {
            CrimeEvents.OnHeatChanged -= OnHeatChanged;
            _isWarningActive = false;
        }
    }
}
