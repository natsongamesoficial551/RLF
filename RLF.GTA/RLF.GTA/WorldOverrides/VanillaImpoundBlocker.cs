using GTA;
using GTA.Math;
using GTA.UI;
using GTA.Native;
using System;
using System.Collections.Generic;

namespace RLF.GTA.GTAOnly.WorldOverrides
{
    public sealed class VanillaImpoundBlocker : Script
    {
        private readonly List<Vector3> _vanillaImpounds = new List<Vector3>
        {
            new Vector3(408.97f, -1625.57f, 29.29f), // Mission Row
            new Vector3(401.30f, -1631.00f, 29.29f), // Davis
            new Vector3(1651.87f, 3804.42f, 35.42f)  // Sandy Shores
        };

        private const float BLOCK_RADIUS = 12f;
        private const float PUSH_DISTANCE = 22f;

        private int _nextNotifyAllowed;

        public VanillaImpoundBlocker()
        {
            Tick += OnTick;
            Interval = 200;
        }

        private void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            // ✅ ADICIONE ISSO: Remove blips do pátio
            RemoveImpoundBlips();

            foreach (var impound in _vanillaImpounds)
            {
                float distSq = player.Position.DistanceToSquared(impound);
                if (distSq <= BLOCK_RADIUS * BLOCK_RADIUS)
                {
                    KillVanillaImpoundUsage(player, impound);
                    break;
                }
            }
        }

        // ✅ NOVO MÉTODO: Remove blips do pátio
        private void RemoveImpoundBlips()
        {
            try
            {
                // Percorre todos os blips ativos
                foreach (var impoundPos in _vanillaImpounds)
                {
                    // Pega blips próximos às coordenadas do pátio
                    Blip blip = Function.Call<Blip>(Hash.GET_CLOSEST_BLIP_INFO_ID, (int)BlipSprite.Standard);

                    if (blip != null && blip.Exists())
                    {
                        // Verifica se está perto de algum pátio
                        if (blip.Position.DistanceTo(impoundPos) < 30f)
                        {
                            blip.Delete();
                        }
                    }
                }

                // Método alternativo: remove por iteração
                for (int i = 0; i < 1000; i++)
                {
                    if (Function.Call<bool>(Hash.DOES_BLIP_EXIST, i))
                    {
                        Vector3 blipPos = Function.Call<Vector3>(Hash.GET_BLIP_COORDS, i);

                        foreach (var impound in _vanillaImpounds)
                        {
                            if (blipPos.DistanceTo(impound) < 30f)
                            {
                                Function.Call(Hash.REMOVE_BLIP, new OutputArgument(i));
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void KillVanillaImpoundUsage(Ped player, Vector3 impoundPos)
        {
            Vehicle v = null;
            try
            {
                v = World.GetClosestVehicle(impoundPos, 8f);
            }
            catch { }

            if (v != null && v.Exists())
            {
                try { v.Delete(); } catch { }
            }

            Vector3 dir = (player.Position - impoundPos);
            if (dir.Length() < 0.1f)
                dir = player.ForwardVector;

            dir.Normalize();

            Vector3 safePos = impoundPos + dir * PUSH_DISTANCE;
            safePos.Z = player.Position.Z;

            player.Position = safePos;

            try
            {
                Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 51, true);
                Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 23, true);
                Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 38, true);
            }
            catch { }

            int now = Game.GameTime;
            if (now > _nextNotifyAllowed)
            {
                Notification.Show("🚫 Pátio do jogo desativado\nUse o ~y~Pátio RLF~s~");
                _nextNotifyAllowed = now + 3000;
            }
        }
    }
}