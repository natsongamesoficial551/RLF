using System;

namespace RLF.Core.Crime
{
    /// <summary>
    /// Sistema de eventos do módulo de crimes.
    /// Permite comunicação desacoplada entre Core e GTA.
    /// </summary>
    public static class CrimeEvents
    {
        public static event Action<CrimeRecord> OnCrimeCommitted;
        public static event Action<CrimeCase> OnCaseOpened;
        public static event Action<CrimeCase> OnCaseClosed;
        public static event Action<CrimeRecord, bool> OnCrimeReported;
        public static event Action<float, HeatState> OnHeatChanged;
        public static event Action<CrimeCase, bool> OnArrestEligibilityChanged;

        internal static void RaiseCrimeCommitted(CrimeRecord crime)
        {
            try
            {
                OnCrimeCommitted?.Invoke(crime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrimeEvents] Error in OnCrimeCommitted: {ex.Message}");
            }
        }

        internal static void RaiseCaseOpened(CrimeCase crimeCase)
        {
            try
            {
                OnCaseOpened?.Invoke(crimeCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrimeEvents] Error in OnCaseOpened: {ex.Message}");
            }
        }

        internal static void RaiseCaseClosed(CrimeCase crimeCase)
        {
            try
            {
                OnCaseClosed?.Invoke(crimeCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrimeEvents] Error in OnCaseClosed: {ex.Message}");
            }
        }

        internal static void RaiseCrimeReported(CrimeRecord crime, bool wasReported)
        {
            try
            {
                OnCrimeReported?.Invoke(crime, wasReported);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrimeEvents] Error in OnCrimeReported: {ex.Message}");
            }
        }

        internal static void RaiseHeatChanged(float newHeat, HeatState newState)
        {
            try
            {
                OnHeatChanged?.Invoke(newHeat, newState);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrimeEvents] Error in OnHeatChanged: {ex.Message}");
            }
        }

        internal static void RaiseArrestEligibilityChanged(CrimeCase crimeCase, bool isEligible)
        {
            try
            {
                OnArrestEligibilityChanged?.Invoke(crimeCase, isEligible);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrimeEvents] Error in OnArrestEligibilityChanged: {ex.Message}");
            }
        }

        public static void ClearAllSubscribers()
        {
            OnCrimeCommitted = null;
            OnCaseOpened = null;
            OnCaseClosed = null;
            OnCrimeReported = null;
            OnHeatChanged = null;
            OnArrestEligibilityChanged = null;
        }
    }
}
