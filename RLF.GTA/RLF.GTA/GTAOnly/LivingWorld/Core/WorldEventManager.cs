using GTA;
using GTA.Math;
using GTA.UI;
using System;
using System.Collections.Generic;
using RLF.Core.Debug;

namespace RLF.GTA.GTAOnly.LivingWorld.Core
{
    public class WorldEventManager
    {
        private readonly List<ActiveScene> _activeScenes = new List<ActiveScene>();
        private DateTime _nextSpawnAllowed = DateTime.MinValue;

        private static readonly Random _random = new Random();

        // Anti-repetição
        private Type _lastEventType;

        public void Reset()
        {
            for (int i = _activeScenes.Count - 1; i >= 0; i--)
            {
                try { _activeScenes[i].Stop(); } catch { }
            }
            _activeScenes.Clear();
        }

        public void Tick()
        {
            for (int i = _activeScenes.Count - 1; i >= 0; i--)
            {
                ActiveScene scene = _activeScenes[i];

                try { scene.Update(); }
                catch
                {
                    try { scene.Stop(); } catch { }
                    _activeScenes.RemoveAt(i);
                    continue;
                }

                if (!scene.Event.IsActive)
                    _activeScenes.RemoveAt(i);
            }

            TrySpawnEvent();
        }

        private void TrySpawnEvent()
        {
            if (_activeScenes.Count >= LivingWorldConfig.MaxActiveScenes)
                return;

            if (DateTime.Now < _nextSpawnAllowed)
                return;

            // Chance global
            if (_random.NextDouble() > LivingWorldConfig.SpawnChance)
                return;

            WorldEventContext context = new WorldEventContext();
            if (context.PlayerPed == null || !context.PlayerPed.Exists())
                return;

            Vector3 spawnPos = global::GTA.World.GetNextPositionOnStreet(
                context.PlayerPosition + context.PlayerForward * LivingWorldConfig.MinSpawnDistance
            );

            if (spawnPos == Vector3.Zero)
                return;

            _nextSpawnAllowed = DateTime.Now.AddSeconds(LivingWorldConfig.SpawnCooldownSeconds);

            // ===============================
            // 🎯 PESOS REALISTAS
            // Pane: 55%
            // Assalto: 30%
            // Acidente: 15%
            // ===============================
            double roll = _random.NextDouble();
            WorldEvent evt;

            if (roll < 0.55)
                evt = new Events.VehicleBreakdownEvent();
            else if (roll < 0.85)
                evt = new Events.StreetRobberyEvent();
            else
                evt = new Events.TrafficAccidentEvent();

            // Anti-repetição simples
            if (_lastEventType != null && evt.GetType() == _lastEventType)
                return;

            evt.Start(context);

            if (!evt.IsActive)
                return;

            _lastEventType = evt.GetType();
            _activeScenes.Add(new ActiveScene(evt));

            if (LivingWorldConfig.NotifyOnSpawn)
            {
                int dist = (int)context.PlayerPosition.DistanceTo(evt.Position);
                Notification.PostTicker(
                    $"~y~LivingWorld~w~: {evt.DisplayName} (~g~{dist}m~w~)",
                    true,
                    false
                );
            }

            RLFDebug.Info(
                DebugChannel.LivingWorld,
                $"{evt.DisplayName} iniciado em {evt.Position}"
            );
        }
    }
}
