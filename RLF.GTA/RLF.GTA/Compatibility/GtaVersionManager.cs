// ============================================================================
// GtaVersionManager.cs
// Centralized Version Detection & Compatibility Rules for GTA V Mods
// ============================================================================
//
// PURPOSE:
// This file acts as a logical firewall between your mod scripts and the
// GTA V engine. It detects whether the game is running Legacy (1.0.3xxx)
// or Enhanced (1.0.10xx) and provides read-only flags that scripts can
// query before executing potentially dangerous operations.
//
// WHY ENHANCED IS MORE RESTRICTIVE:
// GTA V Enhanced (2025) uses a completely rebuilt graphics pipeline with
// new asset streaming, ray tracing support, and modernized render states.
// Operations that were safe in Legacy can corrupt the graphics state in
// Enhanced, causing ERR_GFX_STATE crashes. High-frequency native calls,
// aggressive clock manipulation, and certain weather API usages interfere
// with Enhanced's stricter state management and async rendering pipeline.
//
// WHY LEGACY ALLOWS MORE FREEDOM:
// Legacy (pre-2025) uses the original RAGE engine graphics pipeline that
// has been stable since 2015. The engine is more tolerant of rapid state
// changes, high-frequency natives, and direct clock/weather manipulation.
// Years of modding have established what works reliably in Legacy.
//
// IMPORTANT NOTES:
// - This is NOT a graphics mod or visual enhancement
// - This does NOT modify any game state directly
// - This does NOT call any natives or create any threads
// - This ONLY provides read-only compatibility information
// - This helps PREVENT ERR_GFX_STATE by letting scripts self-regulate
// - This is completely passive and reversible
//
// USAGE IN YOUR SCRIPTS:
//   if (!GtaVersionManager.AllowHighFrequencyNatives) return;
//   if (!GtaVersionManager.AllowRealTimeClock) return;
//
// ============================================================================

using System;
using GTA;

namespace RLF.GTA.Compatibility
{
    /// <summary>
    /// Identifies the major build type of GTA V currently running.
    /// </summary>
    public enum GtaBuildType
    {
        /// <summary>
        /// GTA V Legacy (original release, versions 1.0.3xxx and below).
        /// Uses the classic RAGE engine with established modding compatibility.
        /// </summary>
        Legacy,

        /// <summary>
        /// GTA V Enhanced (2025 re-release, versions 1.0.10xx).
        /// Uses rebuilt graphics pipeline with stricter state management.
        /// </summary>
        Enhanced,

        /// <summary>
        /// Version could not be determined. Applies maximum restrictions
        /// as a safety fallback to prevent crashes on unknown builds.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Centralized version detection and compatibility rule provider for GTA V mods.
    /// This class is completely passive: it performs no game modifications, creates
    /// no threads, registers no events, and calls no natives. It only provides
    /// read-only information that scripts can query to self-regulate behavior.
    /// </summary>
    public static class GtaVersionManager
    {
        // ====================================================================
        // VERSION DETECTION (performed once via static constructor)
        // ====================================================================

        private static readonly GtaBuildType _detectedBuildType;
        private static readonly GameVersion _rawGameVersion;
        private static readonly string _versionDescription;

        /// <summary>
        /// Static constructor performs one-time version detection.
        /// This runs automatically when any member of the class is first accessed.
        /// </summary>
        static GtaVersionManager()
        {
            try
            {
                _rawGameVersion = Game.Version;
                _detectedBuildType = ClassifyBuildType(_rawGameVersion);
                _versionDescription = BuildVersionDescription(_rawGameVersion, _detectedBuildType);
            }
            catch
            {
                // If detection fails for any reason, default to Unknown (most restrictive)
                _rawGameVersion = GameVersion.Unknown;
                _detectedBuildType = GtaBuildType.Unknown;
                _versionDescription = "Detection failed - applying maximum restrictions";
            }
        }

        /// <summary>
        /// Classifies the GameVersion enum value into a GtaBuildType.
        /// Enhanced versions use a different numbering scheme (1.0.10xx) than Legacy (1.0.3xxx).
        /// </summary>
        private static GtaBuildType ClassifyBuildType(GameVersion version)
        {
            if (version == GameVersion.Unknown)
                return GtaBuildType.Unknown;

            // IMPORTANTE:
            // GameVersion é um enum ordinal (ordem de declaração),
            // NÃO é o número real do build (3759, 1013, etc).
            int ordinal = (int)version;

            /*
             * ScriptHookVDotNet organiza o enum assim:
             * - Todas as versões Legacy vêm primeiro
             * - As versões Enhanced foram adicionadas depois (ordinal maior)
             *
             * Última Legacy conhecida: v1_0_3759_0
             * Primeira Enhanced conhecida: v1_0_1011_0 / v1_0_1013_29
             *
             * Esse limiar é ESTÁVEL mesmo com updates futuros.
             */
            const int ENHANCED_ORDINAL_THRESHOLD = 70;

            if (ordinal >= ENHANCED_ORDINAL_THRESHOLD)
                return GtaBuildType.Enhanced;

            return GtaBuildType.Legacy;
        }

        /// <summary>
        /// Builds a human-readable description of the detected version.
        /// </summary>
        private static string BuildVersionDescription(GameVersion version, GtaBuildType buildType)
        {
            return $"{buildType} ({version})";
        }

        // ====================================================================
        // PUBLIC READ-ONLY PROPERTIES - VERSION INFORMATION
        // ====================================================================

        /// <summary>
        /// The detected build type (Legacy, Enhanced, or Unknown).
        /// </summary>
        public static GtaBuildType BuildType => _detectedBuildType;

        /// <summary>
        /// The raw GameVersion enum value from ScriptHookVDotNet.
        /// </summary>
        public static GameVersion RawVersion => _rawGameVersion;

        /// <summary>
        /// Human-readable description of the detected version.
        /// </summary>
        public static string VersionDescription => _versionDescription;

        /// <summary>
        /// True if running on GTA V Legacy (original release).
        /// </summary>
        public static bool IsLegacy => _detectedBuildType == GtaBuildType.Legacy;

        /// <summary>
        /// True if running on GTA V Enhanced (2025 re-release).
        /// </summary>
        public static bool IsEnhanced => _detectedBuildType == GtaBuildType.Enhanced;

        /// <summary>
        /// True if version detection failed. Scripts should apply maximum caution.
        /// </summary>
        public static bool IsUnknown => _detectedBuildType == GtaBuildType.Unknown;

        // ====================================================================
        // COMPATIBILITY RULES - CENTRALIZED SAFETY FLAGS
        // ====================================================================
        //
        // These flags control which operations are considered safe for the
        // current game version. Scripts should check these before executing
        // potentially dangerous operations.
        //
        // DEFAULT BEHAVIOR:
        // - Legacy: Most operations allowed (TRUE) - established stability
        // - Enhanced: Most operations restricted (FALSE) - new engine caution
        // - Unknown: All operations restricted (FALSE) - maximum safety
        //
        // ====================================================================

        /// <summary>
        /// Whether real-time clock synchronization is allowed.
        /// 
        /// LEGACY (TRUE): Safe to sync game clock with system time at reasonable intervals.
        /// ENHANCED (FALSE): Clock manipulation can interfere with the new lighting 
        /// pipeline and cause render state desynchronization leading to ERR_GFX_STATE.
        /// 
        /// If FALSE, scripts should use static time or very infrequent updates.
        /// </summary>
        public static bool AllowRealTimeClock => _detectedBuildType == GtaBuildType.Legacy;

        /// <summary>
        /// Whether weather API manipulation is allowed.
        /// 
        /// LEGACY (TRUE): Direct weather changes are generally stable.
        /// ENHANCED (FALSE): Weather system is tightly coupled with the new volumetric
        /// clouds, ray-traced lighting, and atmospheric scattering. Rapid or unexpected
        /// weather changes can corrupt the graphics state or cause visual artifacts.
        /// 
        /// If FALSE, scripts should avoid weather manipulation or use very slow transitions.
        /// </summary>
        public static bool AllowWeatherApi => _detectedBuildType == GtaBuildType.Legacy;

        /// <summary>
        /// Whether aggressive optimization techniques are allowed.
        /// 
        /// LEGACY (TRUE): Techniques like pooling, object recycling, and aggressive
        /// cleanup are well-tested and stable.
        /// ENHANCED (FALSE): The new engine has different memory management and object
        /// lifetime expectations. Aggressive optimizations can conflict with internal
        /// reference counting and streaming systems.
        /// 
        /// If FALSE, scripts should use conservative memory management.
        /// </summary>
        public static bool AllowAggressiveOptimization => _detectedBuildType == GtaBuildType.Legacy;

        /// <summary>
        /// Whether forced clock pausing is allowed.
        /// 
        /// LEGACY (TRUE): Pausing game time for menus or effects is generally safe.
        /// ENHANCED (FALSE): Forced clock pausing can desynchronize the render pipeline
        /// from game logic, especially with the new async rendering system. This can
        /// result in ERR_GFX_STATE when the clock resumes.
        /// 
        /// If FALSE, scripts should avoid pausing time or use alternative approaches.
        /// </summary>
        public static bool AllowForcedClockPause => _detectedBuildType == GtaBuildType.Legacy;

        /// <summary>
        /// Whether high-frequency native calls are allowed (many calls per frame).
        /// 
        /// LEGACY (TRUE): The original engine handles rapid native calls well.
        /// ENHANCED (FALSE): The new engine has stricter timing requirements and
        /// thread safety constraints. High-frequency natives can overwhelm the
        /// command buffer or cause race conditions with the render thread.
        /// 
        /// If FALSE, scripts should batch operations, use caching, or reduce call frequency.
        /// </summary>
        public static bool AllowHighFrequencyNatives => _detectedBuildType == GtaBuildType.Legacy;

        /// <summary>
        /// Whether direct entity streaming control is allowed.
        /// 
        /// LEGACY (TRUE): Manual streaming requests work reliably.
        /// ENHANCED (FALSE): Enhanced has a completely rebuilt streaming system
        /// with different priorities and async loading. Direct manipulation can
        /// conflict with the engine's streaming decisions.
        /// 
        /// If FALSE, scripts should let the engine handle streaming naturally.
        /// </summary>
        public static bool AllowStreamingControl => _detectedBuildType == GtaBuildType.Legacy;

        /// <summary>
        /// Whether rapid visual effects are allowed (particles, lights, etc.).
        /// 
        /// LEGACY (TRUE): Particle and light spawning is generally stable.
        /// ENHANCED (FALSE): The new deferred rendering and ray tracing systems
        /// are sensitive to rapid light source changes. Excessive visual effects
        /// can corrupt the light buffer or cause ERR_GFX_STATE.
        /// 
        /// If FALSE, scripts should limit visual effect frequency and count.
        /// </summary>
        public static bool AllowRapidVisualEffects => _detectedBuildType == GtaBuildType.Legacy;

        /// <summary>
        /// Whether camera manipulation is allowed at high frequency.
        /// 
        /// LEGACY (TRUE): Camera changes per-frame are handled well.
        /// ENHANCED (FALSE): The new rendering pipeline pre-calculates camera
        /// data for temporal effects (TAA, motion blur, ray tracing). Rapid
        /// camera manipulation can cause visual artifacts or state corruption.
        /// 
        /// If FALSE, scripts should use smooth camera transitions and limit frequency.
        /// </summary>
        public static bool AllowHighFrequencyCamera => _detectedBuildType == GtaBuildType.Legacy;

        /// <summary>
        /// Whether texture/material replacement at runtime is allowed.
        /// 
        /// LEGACY (TRUE): Runtime texture swaps are generally stable.
        /// ENHANCED (FALSE): Enhanced uses a new material system with PBR
        /// workflows and cached shader variants. Runtime changes can leave
        /// the renderer in an inconsistent state.
        /// 
        /// If FALSE, scripts should avoid runtime texture/material changes.
        /// </summary>
        public static bool AllowRuntimeTextureSwap => _detectedBuildType == GtaBuildType.Legacy;

        /// <summary>
        /// Whether synchronous loading operations are allowed.
        /// 
        /// LEGACY (TRUE): Sync loading with brief freezes is acceptable.
        /// ENHANCED (FALSE): The new engine expects fully async loading.
        /// Synchronous operations can stall the render thread and cause
        /// timeout-related crashes or ERR_GFX_STATE.
        /// 
        /// If FALSE, scripts must use async loading patterns.
        /// </summary>
        public static bool AllowSynchronousLoading => _detectedBuildType == GtaBuildType.Legacy;

        // ====================================================================
        // UTILITY METHODS
        // ====================================================================

        /// <summary>
        /// Checks if a specific feature is allowed based on current version.
        /// Provides a centralized way to add custom feature checks.
        /// </summary>
        /// <param name="featureName">Name of the feature to check.</param>
        /// <returns>True if the feature is allowed, false otherwise.</returns>
        public static bool IsFeatureAllowed(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
            {
                return false;
            }

            switch (featureName.ToLowerInvariant())
            {
                case "realtimeclock":
                    return AllowRealTimeClock;
                case "weatherapi":
                    return AllowWeatherApi;
                case "aggressiveoptimization":
                    return AllowAggressiveOptimization;
                case "forcedclockpause":
                    return AllowForcedClockPause;
                case "highfrequencynatives":
                    return AllowHighFrequencyNatives;
                case "streamingcontrol":
                    return AllowStreamingControl;
                case "rapidvisualeffects":
                    return AllowRapidVisualEffects;
                case "highfrequencycamera":
                    return AllowHighFrequencyCamera;
                case "runtimetextureswap":
                    return AllowRuntimeTextureSwap;
                case "synchronousloading":
                    return AllowSynchronousLoading;
                default:
                    // Unknown features are denied by default for safety
                    return false;
            }
        }

        /// <summary>
        /// Gets a safe interval multiplier for tick-based operations.
        /// Scripts can multiply their base interval by this value to
        /// reduce frequency on Enhanced builds.
        /// 
        /// LEGACY: 1.0x (no change)
        /// ENHANCED: 3.0x (reduce frequency by factor of 3)
        /// UNKNOWN: 5.0x (maximum caution)
        /// </summary>
        public static float SafeIntervalMultiplier
        {
            get
            {
                switch (_detectedBuildType)
                {
                    case GtaBuildType.Legacy:
                        return 1.0f;
                    case GtaBuildType.Enhanced:
                        return 3.0f;
                    case GtaBuildType.Unknown:
                    default:
                        return 5.0f;
                }
            }
        }

        /// <summary>
        /// Gets the recommended maximum native calls per frame.
        /// Scripts should try to stay under this limit to avoid
        /// overwhelming the engine.
        /// 
        /// LEGACY: 100 calls/frame (generous limit)
        /// ENHANCED: 25 calls/frame (conservative limit)
        /// UNKNOWN: 10 calls/frame (maximum caution)
        /// </summary>
        public static int RecommendedMaxNativesPerFrame
        {
            get
            {
                switch (_detectedBuildType)
                {
                    case GtaBuildType.Legacy:
                        return 100;
                    case GtaBuildType.Enhanced:
                        return 25;
                    case GtaBuildType.Unknown:
                    default:
                        return 10;
                }
            }
        }

        /// <summary>
        /// Returns a formatted string with all current compatibility settings.
        /// Useful for debugging and logging mod initialization.
        /// </summary>
        public static string GetCompatibilityReport()
        {
            return string.Format(
                "GTA Version Manager Compatibility Report\n" +
                "=========================================\n" +
                "Detected Build: {0}\n" +
                "Raw Version: {1}\n" +
                "Description: {2}\n" +
                "-----------------------------------------\n" +
                "AllowRealTimeClock: {3}\n" +
                "AllowWeatherApi: {4}\n" +
                "AllowAggressiveOptimization: {5}\n" +
                "AllowForcedClockPause: {6}\n" +
                "AllowHighFrequencyNatives: {7}\n" +
                "AllowStreamingControl: {8}\n" +
                "AllowRapidVisualEffects: {9}\n" +
                "AllowHighFrequencyCamera: {10}\n" +
                "AllowRuntimeTextureSwap: {11}\n" +
                "AllowSynchronousLoading: {12}\n" +
                "-----------------------------------------\n" +
                "SafeIntervalMultiplier: {13:F1}x\n" +
                "RecommendedMaxNativesPerFrame: {14}\n" +
                "=========================================",
                _detectedBuildType,
                _rawGameVersion,
                _versionDescription,
                AllowRealTimeClock,
                AllowWeatherApi,
                AllowAggressiveOptimization,
                AllowForcedClockPause,
                AllowHighFrequencyNatives,
                AllowStreamingControl,
                AllowRapidVisualEffects,
                AllowHighFrequencyCamera,
                AllowRuntimeTextureSwap,
                AllowSynchronousLoading,
                SafeIntervalMultiplier,
                RecommendedMaxNativesPerFrame
            );
        }
    }
}