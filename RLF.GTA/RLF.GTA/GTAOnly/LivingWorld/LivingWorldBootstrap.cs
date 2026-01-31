using GTA;
using GTA.Native;
using RLF.Core.Debug;
using RLF.GTA.GTAOnly.LivingWorld.Core;
using System;

namespace RLF.GTA.GTAOnly.LivingWorld
{
    public class LivingWorldBootstrap : Script
    {
        private static WorldEventManager _manager;
        private static bool _started;

        private DateTime _lastTick = DateTime.MinValue;
        private DateTime _bootTime;

        public LivingWorldBootstrap()
        {
            Tick += OnTick;
            Aborted += OnAbort;

            if (!_started)
            {
                _manager = new WorldEventManager();
                _bootTime = DateTime.Now;
                _started = true;

                RLFDebug.Info(
                    DebugChannel.LivingWorld,
                    "LivingWorldBootstrap iniciado (SAFE MODE / SHVDN 3.9)"
                );
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_manager == null)
                return;

            // 🔒 WARMUP FORTE – evita crash no carregamento inicial
            if ((DateTime.Now - _bootTime).TotalSeconds < 20.0)
                return;

            // 🔒 THROTTLE – evita spam de natives
            if ((DateTime.Now - _lastTick).TotalSeconds < 1.5)
                return;

            _lastTick = DateTime.Now;

            try
            {
                Ped ped = Game.Player.Character;

                // Player ainda não está pronto
                if (ped == null || !ped.Exists() || ped.IsDead || ped.IsRagdoll)
                    return;

                // Evita rodar durante fade / loading
                try
                {
                    bool fadedIn = Function.Call<bool>(Hash.IS_SCREEN_FADED_IN);
                    if (!fadedIn)
                        return;
                }
                catch
                {
                    // Se esse native falhar, apenas ignora o tick
                    return;
                }

                _manager.Tick();
            }
            catch (Exception ex)
            {
                // Nunca deixa crashar o jogo
                RLFDebug.Error(
                    DebugChannel.Crash,
                    "LivingWorld Tick protegido contra crash",
                    ex
                );
            }
        }

        private void OnAbort(object sender, EventArgs e)
        {
            try
            {
                _manager?.Reset();
            }
            catch { }
        }
    }
}
