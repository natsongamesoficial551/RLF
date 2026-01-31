using GTA;
using GTA.Math;
using GTA.Native;
using System;
using RLF.GTA.GTAOnly.LivingWorld.Core;

namespace RLF.GTA.GTAOnly.LivingWorld.Events
{
    public class StreetRobberyEvent : WorldEvent
    {
        public override string DisplayName => "Assalto na Calçada";

        private Ped _robber;
        private Ped _victim;
        private Blip _blip;

        private DateTime _endTime;
        private Vector3 _centerPos;

        private bool _sceneStarted;
        private bool _robberyDone;

        protected override bool OnStart(WorldEventContext ctx)
        {
            Vector3 basePos = FindBestPedPosition(ctx);

            if (basePos == Vector3.Zero)
                return false;

            Position = basePos;
            _centerPos = basePos;

            Model robberModel = new Model("g_m_y_salvagoon_01");
            Model victimModel = new Model("a_m_y_business_01");

            if (!TryLoadModel(robberModel) || !TryLoadModel(victimModel))
                return false;

            _robber = global::GTA.World.CreatePed(robberModel, basePos);
            _victim = global::GTA.World.CreatePed(victimModel, basePos + ctx.PlayerForward * 1.1f);

            if (_robber == null || !_robber.Exists() || _victim == null || !_victim.Exists())
                return false;

            _robber.IsPersistent = true;
            _victim.IsPersistent = true;

            try
            {
                _robber.Weapons.Give(WeaponHash.Pistol, 30, true, true);
            }
            catch { }

            _sceneStarted = false;
            _robberyDone = false;
            _endTime = DateTime.Now.AddSeconds(25);

            if (LivingWorldConfig.CreateBlipForEvents)
            {
                _blip = global::GTA.World.CreateBlip(basePos);
                if (_blip != null && _blip.Exists())
                {
                    _blip.Sprite = BlipSprite.Standard;
                    _blip.Color = BlipColor.Red;
                    _blip.Name = DisplayName;
                    _blip.IsShortRange = false;
                }
            }

            return true;
        }

        protected override void OnUpdate()
        {
            if (_robber == null || !_robber.Exists() || _victim == null || !_victim.Exists())
            {
                Stop();
                return;
            }

            if (Game.Player.Character.Position.DistanceTo(_centerPos) > 120f)
            {
                Stop();
                return;
            }

            if (!_sceneStarted)
            {
                _sceneStarted = true;

                try
                {
                    _robber.Task.ClearAllImmediately();
                    _victim.Task.ClearAllImmediately();

                    Function.Call(Hash.TASK_AIM_GUN_AT_ENTITY, _robber, _victim, 5000, false);
                    Function.Call(Hash.TASK_HANDS_UP, _victim, 5000, _robber, -1, false);
                }
                catch { }
            }

            if (!_robberyDone && DateTime.Now > _endTime.AddSeconds(-15))
            {
                _robberyDone = true;

                try
                {
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        _robber,
                        "mp_common",
                        "givetake1_a",
                        8f,
                        -8f,
                        1200,
                        0,
                        0,
                        false,
                        false,
                        false
                    );
                }
                catch { }
            }

            if (_robberyDone && DateTime.Now > _endTime.AddSeconds(-10))
            {
                try
                {
                    _robber.Task.ReactAndFlee(Game.Player.Character);
                    _victim.Task.ReactAndFlee(_robber);
                }
                catch { }

                Stop();
            }

            if (DateTime.Now > _endTime)
                Stop();
        }

        protected override void OnStop()
        {
            try { _blip?.Delete(); } catch { }
            try { if (_robber.Exists()) _robber.MarkAsNoLongerNeeded(); } catch { }
            try { if (_victim.Exists()) _victim.MarkAsNoLongerNeeded(); } catch { }
        }

        // ⭐ FUNÇÃO FINAL: tenta calçada, mas SEM deixar evento morrer
        private Vector3 FindBestPedPosition(WorldEventContext ctx)
        {
            Vector3 forward = ctx.PlayerForward;
            Vector3 right = Vector3.Cross(forward, Vector3.WorldUp);

            Vector3 origin = ctx.PlayerPosition + forward * LivingWorldConfig.MinSpawnDistance;

            // 1️⃣ Tentativa forte de calçada
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = origin + right * (2.5f + i) + forward * (i * 1.2f);

                float groundZ;
                if (global::GTA.World.GetGroundHeight(p, out groundZ))
                {
                    p.Z = groundZ;

                    bool isRoad = Function.Call<bool>(Hash.IS_POINT_ON_ROAD, p.X, p.Y, p.Z, null);
                    if (!isRoad)
                        return p;
                }
            }

            // 2️⃣ Fallback seguro: lateral da rua (NUNCA meio)
            Vector3 street = global::GTA.World.GetNextPositionOnStreet(origin);
            if (street != Vector3.Zero)
            {
                street += right * 2.0f;

                float z;
                if (global::GTA.World.GetGroundHeight(street, out z))
                {
                    street.Z = z;
                    return street;
                }
            }

            return Vector3.Zero;
        }

        private bool TryLoadModel(Model model)
        {
            try
            {
                if (!model.IsInCdImage || !model.IsValid)
                    return false;

                if (model.IsLoaded)
                    return true;

                model.Request();
                int start = Game.GameTime;

                while (!model.IsLoaded && Game.GameTime - start < 1000)
                    Script.Yield();

                return model.IsLoaded;
            }
            catch
            {
                return false;
            }
        }
    }
}
