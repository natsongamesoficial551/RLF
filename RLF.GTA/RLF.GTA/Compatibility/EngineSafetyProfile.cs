// ============================================================================
// EngineSafetyProfile.cs
// Risk-Based Safety Classification for GTA V Mod Operations
// ============================================================================
//
// PURPOSE:
// This file provides a RISK-BASED safety classification system that operates
// as an additional layer alongside SafeGameActions. While SafeGameActions
// blocks specific dangerous operations, EngineSafetyProfile classifies
// operations by CATEGORY OF RISK, allowing systems like Watchdog, Scheduler,
// and IO managers to make informed decisions based on risk profiles.
//
// WHY RISK-BASED, NOT VERSION-BASED:
// Version checks answer "what game is this?" but not "what could go wrong?"
// Risk categories answer "what TYPE of problem could this cause?" which is
// more useful for systems that need to self-regulate behavior dynamically.
//
// RISK CATEGORIES:
// - Temporal: Operations that affect time, rendering pipeline, TAA, motion blur
// - Streaming: Operations that affect asset loading, memory, object lifetime
// - Clock: Operations that manipulate game time progression
// - AsyncIO: Operations involving async file/network access
// - NativeFrequency: Operations that call natives at high frequency
//
// RELATIONSHIP WITH OTHER FILES:
// - GtaVersionManager: Detects version, provides raw compatibility flags
// - SafeGameActions: Blocks dangerous operations, provides safe alternatives
// - EngineSafetyProfile: Classifies risk, informs architectural decisions
//
// This file is COMPLETELY PASSIVE:
// - No threads created
// - No tick handlers registered
// - No natives called
// - No game state modified
// - Only provides read-only risk classification
//
// ============================================================================

using System;

namespace RLF.GTA.Compatibility
{
    /// <summary>
    /// Categories of risk for engine operations.
    /// Each category represents a different subsystem that can be affected.
    /// </summary>
    public enum RiskCategory
    {
        /// <summary>
        /// Operations affecting the rendering pipeline, temporal effects,
        /// TAA, motion blur, ray tracing, and frame timing.
        /// HIGH RISK on Enhanced due to rebuilt graphics pipeline.
        /// </summary>
        Temporal,

        /// <summary>
        /// Operations affecting asset streaming, model loading,
        /// texture management, and object lifetime.
        /// MEDIUM-HIGH RISK on Enhanced due to async streaming changes.
        /// </summary>
        Streaming,

        /// <summary>
        /// Operations affecting game clock, time progression,
        /// day/night cycle, and time-based events.
        /// HIGH RISK on Enhanced due to temporal rendering coupling.
        /// </summary>
        Clock,

        /// <summary>
        /// Operations involving asynchronous file access,
        /// network calls, or background processing.
        /// MEDIUM RISK - depends on implementation.
        /// </summary>
        AsyncIO,

        /// <summary>
        /// Operations that call natives at high frequency
        /// (multiple times per frame or in tight loops).
        /// HIGH RISK on Enhanced due to stricter timing constraints.
        /// </summary>
        NativeFrequency
    }

    /// <summary>
    /// Safety profile levels that determine how restrictive the engine should be.
    /// </summary>
    public enum SafetyLevel
    {
        /// <summary>
        /// Standard safety - most operations allowed with reasonable limits.
        /// Applied to Legacy builds with established stability.
        /// </summary>
        Standard,

        /// <summary>
        /// Elevated safety - many operations restricted or throttled.
        /// Applied to Enhanced builds or uncertain environments.
        /// </summary>
        Elevated,

        /// <summary>
        /// Maximum safety - only essential operations allowed.
        /// Applied when version is unknown or errors detected.
        /// </summary>
        Maximum
    }

    /// <summary>
    /// Provides risk-based safety classification for engine operations.
    /// This is a passive classification layer - it does not block operations,
    /// but provides information that other systems can use to self-regulate.
    /// 
    /// Use SafeGameActions for actual operation blocking.
    /// Use EngineSafetyProfile for architectural decisions and risk assessment.
    /// </summary>
    public static class EngineSafetyProfile
    {
        // ====================================================================
        // PROFILE DETECTION (performed once via static constructor)
        // ====================================================================

        private static readonly SafetyLevel _currentSafetyLevel;
        private static readonly string _profileDescription;

        static EngineSafetyProfile()
        {
            _currentSafetyLevel = DetermineSafetyLevel();
            _profileDescription = BuildProfileDescription(_currentSafetyLevel);
        }

        private static SafetyLevel DetermineSafetyLevel()
        {
            if (GtaVersionManager.IsUnknown)
            {
                return SafetyLevel.Maximum;
            }

            if (GtaVersionManager.IsEnhanced)
            {
                return SafetyLevel.Elevated;
            }

            return SafetyLevel.Standard;
        }

        private static string BuildProfileDescription(SafetyLevel level)
        {
            switch (level)
            {
                case SafetyLevel.Standard:
                    return "Standard (Legacy) - Most operations permitted";
                case SafetyLevel.Elevated:
                    return "Elevated (Enhanced) - Restricted operations";
                case SafetyLevel.Maximum:
                    return "Maximum (Unknown) - Minimal operations only";
                default:
                    return "Unknown safety level";
            }
        }

        // ====================================================================
        // PUBLIC PROPERTIES - SAFETY LEVEL
        // ====================================================================

        /// <summary>
        /// The current safety level based on detected game version.
        /// </summary>
        public static SafetyLevel CurrentSafetyLevel => _currentSafetyLevel;

        /// <summary>
        /// Human-readable description of the current safety profile.
        /// </summary>
        public static string ProfileDescription => _profileDescription;

        /// <summary>
        /// True if operating under standard (least restrictive) safety.
        /// </summary>
        public static bool IsStandardSafety => _currentSafetyLevel == SafetyLevel.Standard;

        /// <summary>
        /// True if operating under elevated (moderately restrictive) safety.
        /// </summary>
        public static bool IsElevatedSafety => _currentSafetyLevel == SafetyLevel.Elevated;

        /// <summary>
        /// True if operating under maximum (most restrictive) safety.
        /// </summary>
        public static bool IsMaximumSafety => _currentSafetyLevel == SafetyLevel.Maximum;

        // ====================================================================
        // RISK CATEGORY FLAGS - TEMPORAL
        // ====================================================================

        /// <summary>
        /// Whether temporal manipulation (time-based rendering effects) is allowed.
        /// 
        /// FALSE on Enhanced: Temporal effects (TAA, motion blur) are tightly
        /// coupled with the render pipeline. External manipulation causes
        /// desynchronization leading to ERR_GFX_STATE.
        /// 
        /// TRUE on Legacy: Original pipeline tolerates temporal manipulation.
        /// </summary>
        public static bool AllowTemporalManipulation => _currentSafetyLevel == SafetyLevel.Standard;

        /// <summary>
        /// Whether rapid visual state changes are allowed.
        /// 
        /// FALSE on Enhanced: Deferred rendering and ray tracing are sensitive
        /// to rapid state changes in lights, materials, and effects.
        /// 
        /// TRUE on Legacy: Forward rendering handles rapid changes well.
        /// </summary>
        public static bool AllowRapidVisualStateChanges => _currentSafetyLevel == SafetyLevel.Standard;

        // ====================================================================
        // RISK CATEGORY FLAGS - STREAMING
        // ====================================================================

        /// <summary>
        /// Whether manual streaming control is allowed.
        /// 
        /// FALSE on Enhanced: Rebuilt streaming system has different priorities
        /// and async loading patterns. Manual control conflicts with engine.
        /// 
        /// TRUE on Legacy: Manual streaming requests work reliably.
        /// </summary>
        public static bool AllowStreamingControl => _currentSafetyLevel == SafetyLevel.Standard;

        /// <summary>
        /// Whether aggressive cleanup (forced object disposal, pool clearing) is allowed.
        /// 
        /// FALSE on Enhanced: New memory management has different object lifetime
        /// expectations. Aggressive cleanup can break internal reference counting.
        /// 
        /// TRUE on Legacy: Aggressive cleanup is well-tested and stable.
        /// </summary>
        public static bool AllowAggressiveCleanup => _currentSafetyLevel == SafetyLevel.Standard;

        /// <summary>
        /// Whether synchronous loading operations are allowed.
        /// 
        /// FALSE on Enhanced/Maximum: Engine expects fully async loading.
        /// Sync operations can stall render thread causing timeouts.
        /// 
        /// TRUE on Legacy: Brief freezes from sync loading are acceptable.
        /// </summary>
        public static bool AllowSynchronousLoading => _currentSafetyLevel == SafetyLevel.Standard;

        // ====================================================================
        // RISK CATEGORY FLAGS - CLOCK
        // ====================================================================

        /// <summary>
        /// Whether game clock manipulation is allowed.
        /// 
        /// FALSE on Enhanced: Clock is coupled with temporal rendering.
        /// Manipulation causes render pipeline desynchronization.
        /// 
        /// TRUE on Legacy: Clock manipulation is stable.
        /// </summary>
        public static bool AllowClockManipulation => _currentSafetyLevel == SafetyLevel.Standard;

        /// <summary>
        /// Whether clock pausing is allowed.
        /// 
        /// FALSE ALWAYS: PAUSE_CLOCK native is dangerous even on Legacy
        /// when used carelessly. This flag exists for documentation but
        /// defaults to false as a safety measure. Use script-level pausing instead.
        /// 
        /// Note: SafeGameActions does not expose clock pause for this reason.
        /// </summary>
        public static bool AllowClockPause => false;

        // ====================================================================
        // RISK CATEGORY FLAGS - ASYNC IO
        // ====================================================================

        /// <summary>
        /// Whether async file/network IO is allowed.
        /// 
        /// Allowed on Standard and Elevated, blocked on Maximum.
        /// Async IO is generally safe but requires proper error handling.
        /// </summary>
        public static bool AllowAsyncIO => _currentSafetyLevel != SafetyLevel.Maximum;

        /// <summary>
        /// Whether background task scheduling is allowed.
        /// 
        /// Allowed on Standard and Elevated with appropriate limits.
        /// Blocked on Maximum to minimize system complexity.
        /// </summary>
        public static bool AllowBackgroundTasks => _currentSafetyLevel != SafetyLevel.Maximum;

        // ====================================================================
        // RISK CATEGORY FLAGS - NATIVE FREQUENCY
        // ====================================================================

        /// <summary>
        /// Whether high-frequency native calls are allowed.
        /// 
        /// FALSE on Enhanced: Stricter timing and thread safety constraints.
        /// High-frequency natives overwhelm command buffer or cause races.
        /// 
        /// TRUE on Legacy: Original engine handles rapid native calls well.
        /// </summary>
        public static bool AllowHighFrequencyNatives => _currentSafetyLevel == SafetyLevel.Standard;

        /// <summary>
        /// Whether batch operations (many operations per frame) are allowed.
        /// 
        /// FALSE on Enhanced/Maximum: Batch operations can exceed frame budget.
        /// 
        /// TRUE on Legacy: Batching is efficient and stable.
        /// </summary>
        public static bool AllowBatchOperations => _currentSafetyLevel == SafetyLevel.Standard;

        // ====================================================================
        // RISK QUERY METHODS
        // ====================================================================

        /// <summary>
        /// Checks if operations in a specific risk category are generally allowed.
        /// This is a simplified query - for specific operations, use the
        /// individual flags above.
        /// </summary>
        /// <param name="category">The risk category to check.</param>
        /// <returns>True if operations in this category are generally permitted.</returns>
        public static bool IsCategoryAllowed(RiskCategory category)
        {
            switch (category)
            {
                case RiskCategory.Temporal:
                    return AllowTemporalManipulation;

                case RiskCategory.Streaming:
                    return AllowStreamingControl;

                case RiskCategory.Clock:
                    return AllowClockManipulation;

                case RiskCategory.AsyncIO:
                    return AllowAsyncIO;

                case RiskCategory.NativeFrequency:
                    return AllowHighFrequencyNatives;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the recommended maximum operations per frame for a category.
        /// Systems can use this to self-throttle.
        /// </summary>
        /// <param name="category">The risk category.</param>
        /// <returns>Recommended maximum operations per frame.</returns>
        public static int GetMaxOperationsPerFrame(RiskCategory category)
        {
            switch (_currentSafetyLevel)
            {
                case SafetyLevel.Standard:
                    return GetStandardLimit(category);

                case SafetyLevel.Elevated:
                    return GetElevatedLimit(category);

                case SafetyLevel.Maximum:
                default:
                    return GetMaximumLimit(category);
            }
        }

        private static int GetStandardLimit(RiskCategory category)
        {
            switch (category)
            {
                case RiskCategory.Temporal:
                    return 10;
                case RiskCategory.Streaming:
                    return 20;
                case RiskCategory.Clock:
                    return 5;
                case RiskCategory.AsyncIO:
                    return 15;
                case RiskCategory.NativeFrequency:
                    return 100;
                default:
                    return 10;
            }
        }

        private static int GetElevatedLimit(RiskCategory category)
        {
            switch (category)
            {
                case RiskCategory.Temporal:
                    return 0;
                case RiskCategory.Streaming:
                    return 5;
                case RiskCategory.Clock:
                    return 0;
                case RiskCategory.AsyncIO:
                    return 10;
                case RiskCategory.NativeFrequency:
                    return 25;
                default:
                    return 5;
            }
        }

        private static int GetMaximumLimit(RiskCategory category)
        {
            switch (category)
            {
                case RiskCategory.Temporal:
                    return 0;
                case RiskCategory.Streaming:
                    return 2;
                case RiskCategory.Clock:
                    return 0;
                case RiskCategory.AsyncIO:
                    return 0;
                case RiskCategory.NativeFrequency:
                    return 10;
                default:
                    return 2;
            }
        }

        /// <summary>
        /// Gets a multiplier for timing intervals based on current safety level.
        /// Higher safety = higher multiplier = slower operations.
        /// </summary>
        public static float IntervalMultiplier
        {
            get
            {
                switch (_currentSafetyLevel)
                {
                    case SafetyLevel.Standard:
                        return 1.0f;
                    case SafetyLevel.Elevated:
                        return 3.0f;
                    case SafetyLevel.Maximum:
                    default:
                        return 5.0f;
                }
            }
        }
    }
}