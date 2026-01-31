using GTA;
using GTA.Native;
using RLF.Core.Safety;

namespace RLF.GTA.Safety
{
    /// <summary>
    /// Implementação do ISafetyDataProvider para GTA V.
    /// ÚNICO lugar onde GTA é acessado para o sistema de segurança.
    /// </summary>
    public sealed class SafetyDataProvider : ISafetyDataProvider
    {
        #region Singleton

        private static SafetyDataProvider _instance;
        private static readonly object _lock = new object();

        public static SafetyDataProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new SafetyDataProvider();
                    }
                }
                return _instance;
            }
        }

        private SafetyDataProvider() { }

        #endregion

        #region Player Data

        public float GetPlayerPositionX()
        {
            try
            {
                Ped player = Game.Player?.Character;
                return player?.Position.X ?? 0f;
            }
            catch { return 0f; }
        }

        public float GetPlayerPositionY()
        {
            try
            {
                Ped player = Game.Player?.Character;
                return player?.Position.Y ?? 0f;
            }
            catch { return 0f; }
        }

        public float GetPlayerPositionZ()
        {
            try
            {
                Ped player = Game.Player?.Character;
                return player?.Position.Z ?? 0f;
            }
            catch { return 0f; }
        }

        public float GetPlayerSpeed()
        {
            try
            {
                Ped player = Game.Player?.Character;
                if (player == null) return 0f;
                return player.Velocity.Length();
            }
            catch { return 0f; }
        }

        public float GetPlayerHealth()
        {
            try
            {
                Ped player = Game.Player?.Character;
                return player?.Health ?? 0f;
            }
            catch { return 0f; }
        }

        public bool IsPlayerInVehicle()
        {
            try
            {
                Ped player = Game.Player?.Character;
                return player?.IsInVehicle() ?? false;
            }
            catch { return false; }
        }

        public bool IsPlayerInCover()
        {
            try
            {
                Ped player = Game.Player?.Character;
                return player?.IsInCover ?? false;
            }
            catch { return false; }
        }

        public bool IsPlayerInCombat()
        {
            try
            {
                Ped player = Game.Player?.Character;
                return player?.IsInCombat ?? false;
            }
            catch { return false; }
        }

        public int GetPlayerWantedLevel()
        {
            try
            {
                return Game.Player?.WantedLevel ?? 0;
            }
            catch { return 0; }
        }

        #endregion

        #region Input Data

        public bool IsAnyMovementInputPressed()
        {
            try
            {
                return Game.IsControlPressed(Control.MoveUpOnly) ||
                       Game.IsControlPressed(Control.MoveDownOnly) ||
                       Game.IsControlPressed(Control.MoveLeftOnly) ||
                       Game.IsControlPressed(Control.MoveRightOnly) ||
                       Game.IsControlPressed(Control.MoveUp) ||
                       Game.IsControlPressed(Control.MoveDown) ||
                       Game.IsControlPressed(Control.MoveLeft) ||
                       Game.IsControlPressed(Control.MoveRight);
            }
            catch { return false; }
        }

        public bool IsAttackInputPressed()
        {
            try
            {
                return Game.IsControlPressed(Control.Attack);
            }
            catch { return false; }
        }

        public bool IsAimInputPressed()
        {
            try
            {
                return Game.IsControlPressed(Control.Aim);
            }
            catch { return false; }
        }

        #endregion

        #region Game State

        public bool IsGamePaused()
        {
            try
            {
                return Game.IsPaused;
            }
            catch { return false; }
        }

        public bool IsCutsceneActive()
        {
            try
            {
                return Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) ||
                       Function.Call<bool>(Hash.IS_CUTSCENE_PLAYING);
            }
            catch { return false; }
        }

        public bool IsInteriorScene()
        {
            try
            {
                return Function.Call<bool>(Hash.IS_INTERIOR_SCENE);
            }
            catch { return false; }
        }

        public int GetGameHour()
        {
            try
            {
                return Function.Call<int>(Hash.GET_CLOCK_HOURS);
            }
            catch { return 12; }
        }

        public int GetCurrentWeather()
        {
            try
            {
                return (int)World.Weather;
            }
            catch { return 0; }
        }

        #endregion

        #region World Data

        public int GetNearbyPedCount(float radius)
        {
            try
            {
                Ped player = Game.Player?.Character;
                if (player == null) return 0;

                Ped[] peds = World.GetNearbyPeds(player.Position, radius);
                return peds?.Length ?? 0;
            }
            catch { return 0; }
        }

        public int GetNearbyVehicleCount(float radius)
        {
            try
            {
                Ped player = Game.Player?.Character;
                if (player == null) return 0;

                Vehicle[] vehicles = World.GetNearbyVehicles(player.Position, radius);
                return vehicles?.Length ?? 0;
            }
            catch { return 0; }
        }

        #endregion

        #region Performance

        public float GetLastFrameTime()
        {
            try
            {
                return Game.LastFrameTime;
            }
            catch { return 0.033f; } // ~30fps fallback
        }

        #endregion
    }
}