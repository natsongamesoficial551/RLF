using System;

namespace RLF.Core.Safety
{
    #region Enums

    public enum PlayerActivityState
    {
        Unknown,
        Idle,
        InMenu,
        InInterior,
        DrivingFast,
        DrivingSlow,
        InCombat,
        AFK,
        InCutscene,
        Walking
    }

    public enum WorldState
    {
        Normal,
        Quiet,
        NoNPCsNearby,
        DeepNight,
        HeavyWeather,
        HighActivity
    }

    public enum PerformanceLevel
    {
        Optimal,
        Normal,
        LightProtection,
        HeavyProtection,
        Critical,
        Survival
    }

    public enum TickPriority
    {
        Critical = 0,
        High = 1,
        Normal = 2,
        Low = 3,
        Background = 4,
        Batch = 5
    }

    public enum SystemCategory
    {
        Core,
        Combat,
        AI,
        Traffic,
        Crime,
        Economy,
        Jobs,
        UI,
        Weather,
        LivingWorld,
        Debug,
        Custom
    }

    public enum ScriptHealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Disabled
    }

    public enum RiskType
    {
        None,
        LongExecution,
        SilentException,
        RepetitiveLoop,
        ExcessiveErrors
    }

    #endregion

    #region Data Classes

    public class PlayerContext
    {
        public PlayerActivityState ActivityState;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float Speed;
        public int IdleFrames;
        public int AFKFrames;
        public bool IsInVehicle;
        public bool IsInCover;
        public bool IsWanted;
        public int WantedLevel;
        public bool IsInInterior;
        public int FramesSinceCombat;
        public float Health;
        public float LastHealth;
        public bool TookDamageRecently;
    }

    public class WorldContext
    {
        public WorldState CurrentState;
        public int Hour;
        public bool IsDeepNight;
        public bool IsHeavyWeather;
        public int NearbyPedCount;
        public int RelevantNPCCount;
        public bool HasActiveEvents;
        public int ActiveEventCount;
    }

    public class PerformanceContext
    {
        public PerformanceLevel Level;
        public float CurrentFPS;
        public float AverageFPS;
        public int ConsecutiveLowFPSFrames;
        public int ConsecutiveHighFPSFrames;
        public bool GCSpikeDetected;
    }

    public class SessionContext
    {
        public int TotalFrames;
        public int MinutesSinceStart;
        public bool SurvivalModeActive;
    }

    public class FullContext
    {
        public PlayerContext Player = new PlayerContext();
        public WorldContext World = new WorldContext();
        public PerformanceContext Performance = new PerformanceContext();
        public SessionContext Session = new SessionContext();
        public bool IsValid;
    }

    #endregion

    #region Tick Config

    public class SystemTickConfig
    {
        public string SystemId;
        public string DisplayName;
        public SystemCategory Category;
        public TickPriority Priority;

        public int NormalTickFrames;
        public int ReducedTickFrames;
        public int MinimalTickFrames;

        public int CurrentTickFrames;
        public int LastTickFrame;
        public int TickCount;
        public int SkippedTicks;

        public bool IsEnabled;
        public bool IsPaused;
        public bool IsFrozen;
        public int FreezeUntilFrame;

        public Action TickCallback;
        public Func<bool> CanRunCallback;
    }

    public class TickStatistics
    {
        public int TotalSystems;
        public int ActiveSystems;
        public int PausedSystems;
        public int SystemsRanThisTick;
    }

    #endregion

    #region Script Record

    public class ScriptRecord
    {
        public string ScriptId;
        public string ScriptName;
        public ScriptHealthStatus Status;

        public int ExecutionCount;
        public int ErrorCount;
        public int ConsecutiveErrors;
        public int ConsecutiveLongExecutions;

        public int LastActivityFrame;
        public int LastErrorFrame;

        public string LastErrorMessage;

        public bool IsDisabled;
        public int DisabledUntilFrame;
        public int DisableCount;

        public RiskType CurrentRisk;

        public int SameResultCount;
        public int LastResultHash;
    }

    public class ExecContext
    {
        public string ScriptId;
        public int StartFrame;
        public bool Valid;
    }

    #endregion
}