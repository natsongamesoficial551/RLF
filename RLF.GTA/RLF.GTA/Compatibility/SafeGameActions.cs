// ============================================================================
// SafeGameActions.cs
// Centralized Safe Operations Layer for GTA V Mods (Legacy & Enhanced)
// ============================================================================
//
// PURPOSE:
// This file provides a unified API for performing sensitive game operations
// that behave differently between GTA V Legacy and Enhanced. Scripts call
// these methods without needing to check version flags - all compatibility
// logic is handled internally by consulting GtaVersionManager.
//
// DESIGN PHILOSOPHY:
// - Scripts should NEVER need to write "if (GtaVersionManager.IsLegacy)"
// - All methods are safe to call on any version - they self-regulate
// - Operations unsafe for Enhanced are BLOCKED, not throttled
// - Return values indicate whether the operation was actually performed
// - No threads, no events, no ticks, no dangerous natives
// - Minimal API surface - only what's actually needed
//
// WHAT THIS FILE DOES NOT DO:
// - NO clock pausing (PAUSE_CLOCK causes ERR_GFX_STATE on Enhanced)
// - NO "smart" throttling for graphics operations on Enhanced
// - NO complex state management that could desync with the engine
//
// COVERED OPERATIONS:
// - Weather control (Legacy only - blocked on Enhanced)
// - Clock control (Legacy only - blocked on Enhanced)
// - Tick frequency helpers (for script logic, not graphics)
//
// ============================================================================

using System;
using GTA;

namespace RLF.GTA.Compatibility
{
    /// <summary>
    /// Result of a safe game action attempt.
    /// </summary>
    public enum SafeActionResult
    {
        /// <summary>
        /// Operation was performed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// Operation was skipped because it's not allowed on this game version.
        /// This is expected behavior - Enhanced blocks these operations entirely.
        /// </summary>
        SkippedVersionRestriction,

        /// <summary>
        /// Operation failed due to invalid parameters.
        /// </summary>
        FailedInvalidParameters,

        /// <summary>
        /// Operation failed due to an unexpected error.
        /// </summary>
        FailedError
    }

    /// <summary>
    /// Provides safe, version-aware methods for sensitive game operations.
    /// All methods internally consult GtaVersionManager and self-regulate.
    /// 
    /// On Enhanced: operations are BLOCKED (not throttled).
    /// On Legacy: operations execute directly (no artificial delays).
    /// </summary>
    public static class SafeGameActions
    {
        // ====================================================================
        // WEATHER OPERATIONS (Legacy only)
        // ====================================================================

        /// <summary>
        /// Sets the current weather immediately.
        /// 
        /// LEGACY: Performs the weather change.
        /// ENHANCED: Blocked entirely - returns SkippedVersionRestriction.
        /// </summary>
        /// <param name="weather">The weather type to set.</param>
        /// <returns>Result indicating whether the operation was performed.</returns>
        public static SafeActionResult SetWeather(Weather weather)
        {
            if (!GtaVersionManager.AllowWeatherApi)
            {
                return SafeActionResult.SkippedVersionRestriction;
            }

            try
            {
                World.Weather = weather;
                return SafeActionResult.Success;
            }
            catch
            {
                return SafeActionResult.FailedError;
            }
        }

        /// <summary>
        /// Transitions to a new weather type over time.
        /// 
        /// LEGACY: Performs the transition with specified duration.
        /// ENHANCED: Blocked entirely - returns SkippedVersionRestriction.
        /// </summary>
        /// <param name="weather">The target weather type.</param>
        /// <param name="duration">Transition duration in seconds (clamped 1-60).</param>
        /// <returns>Result indicating whether the operation was performed.</returns>
        public static SafeActionResult TransitionWeather(Weather weather, float duration)
        {
            if (!GtaVersionManager.AllowWeatherApi)
            {
                return SafeActionResult.SkippedVersionRestriction;
            }

            float safeDuration = Math.Max(1f, Math.Min(duration, 60f));

            try
            {
                World.TransitionToWeather(weather, safeDuration);
                return SafeActionResult.Success;
            }
            catch
            {
                return SafeActionResult.FailedError;
            }
        }

        /// <summary>
        /// Gets the current weather. Safe on all versions (read-only).
        /// </summary>
        public static Weather GetCurrentWeather()
        {
            try
            {
                return World.Weather;
            }
            catch
            {
                return Weather.Clear;
            }
        }

        /// <summary>
        /// Whether weather control is available on this version.
        /// </summary>
        public static bool IsWeatherControlAvailable => GtaVersionManager.AllowWeatherApi;

        // ====================================================================
        // CLOCK / TIME OPERATIONS (Legacy only)
        // ====================================================================

        /// <summary>
        /// Sets the game time to specified hour, minute, and second.
        /// 
        /// LEGACY: Performs the time change.
        /// ENHANCED: Blocked entirely - returns SkippedVersionRestriction.
        /// </summary>
        /// <param name="hour">Hour (0-23).</param>
        /// <param name="minute">Minute (0-59).</param>
        /// <param name="second">Second (0-59).</param>
        /// <returns>Result indicating whether the operation was performed.</returns>
        public static SafeActionResult SetGameTime(int hour, int minute, int second)
        {
            if (!GtaVersionManager.AllowRealTimeClock)
            {
                return SafeActionResult.SkippedVersionRestriction;
            }

            if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            {
                return SafeActionResult.FailedInvalidParameters;
            }

            try
            {
                World.CurrentTimeOfDay = new TimeSpan(hour, minute, second);
                return SafeActionResult.Success;
            }
            catch
            {
                return SafeActionResult.FailedError;
            }
        }

        /// <summary>
        /// Sets the game time using a TimeSpan.
        /// 
        /// LEGACY: Performs the time change.
        /// ENHANCED: Blocked entirely.
        /// </summary>
        /// <param name="time">The time to set.</param>
        /// <returns>Result indicating whether the operation was performed.</returns>
        public static SafeActionResult SetGameTime(TimeSpan time)
        {
            return SetGameTime(time.Hours, time.Minutes, time.Seconds);
        }

        /// <summary>
        /// Synchronizes game time with the system's real time.
        /// 
        /// LEGACY: Syncs immediately.
        /// ENHANCED: Blocked entirely - clock sync interferes with temporal rendering.
        /// 
        /// WARNING: Even on Legacy, call this sparingly (e.g., once per minute max).
        /// Continuous sync every frame is unnecessary and wasteful.
        /// </summary>
        /// <returns>Result indicating whether the operation was performed.</returns>
        public static SafeActionResult SyncWithRealTime()
        {
            if (!GtaVersionManager.AllowRealTimeClock)
            {
                return SafeActionResult.SkippedVersionRestriction;
            }

            try
            {
                DateTime now = DateTime.Now;
                World.CurrentTimeOfDay = new TimeSpan(now.Hour, now.Minute, now.Second);
                return SafeActionResult.Success;
            }
            catch
            {
                return SafeActionResult.FailedError;
            }
        }

        /// <summary>
        /// Gets the current game time. Safe on all versions (read-only).
        /// </summary>
        public static TimeSpan GetCurrentGameTime()
        {
            try
            {
                return World.CurrentTimeOfDay;
            }
            catch
            {
                return TimeSpan.FromHours(12);
            }
        }

        /// <summary>
        /// Whether clock control is available on this version.
        /// </summary>
        public static bool IsClockControlAvailable => GtaVersionManager.AllowRealTimeClock;

        // ====================================================================
        // TICK FREQUENCY HELPER (for script logic, not graphics)
        // ====================================================================

        /// <summary>
        /// Helper to determine if a periodic action should execute.
        /// Use this for YOUR SCRIPT'S internal logic throttling,
        /// NOT for throttling graphics operations.
        /// 
        /// Example: checking player position every 500ms instead of every frame.
        /// </summary>
        /// <param name="lastExecution">DateTime of last execution.</param>
        /// <param name="intervalMs">Minimum interval between executions.</param>
        /// <param name="newExecutionTime">Output: new time to store if returning true.</param>
        /// <returns>True if enough time has passed.</returns>
        public static bool ShouldExecuteThrottled(DateTime lastExecution, int intervalMs, out DateTime newExecutionTime)
        {
            newExecutionTime = lastExecution;
            DateTime now = DateTime.UtcNow;

            if ((now - lastExecution).TotalMilliseconds >= intervalMs)
            {
                newExecutionTime = now;
                return true;
            }

            return false;
        }
    }
}