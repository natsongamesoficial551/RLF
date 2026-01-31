using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using RLF.Core.Vehicles;

namespace RLF.GTA.Vehicles
{
    /// <summary>
    /// Garagem RLF:
    /// - Blip no mapa
    /// - Menu para retirar veículos em State=Garage
    /// </summary>
    public sealed class VehicleGarageMenu : Script
    {
        private readonly Vector3 _garagePos = new Vector3(215.124f, -810.423f, 30.727f);
        private readonly Vector3 _spawnPos = new Vector3(229.698f, -800.114f, 30.572f);

        private bool _open;
        private int _index;
        private Blip _blip;

        public VehicleGarageMenu()
        {
            CreateBlip();
            Tick += OnTick;
        }

        private void CreateBlip()
        {
            _blip = World.CreateBlip(_garagePos);
            if (_blip == null)
                return;

            _blip.Sprite = BlipSprite.Garage;
            _blip.Color = BlipColor.White;
            _blip.Scale = 0.9f;
            _blip.Name = "Garagem RLF";
            _blip.IsShortRange = false;
        }

        private void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            float dist = player.Position.DistanceTo(_garagePos);

            if (dist < 2.5f)
            {
                global::GTA.UI.Screen.ShowHelpTextThisFrame(
                    "Pressione ~INPUT_CONTEXT~ para acessar a garagem"
                );

                if (Game.IsKeyPressed(Keys.E))
                    _open = !_open;
            }

            if (_open)
                DrawMenu();
        }

        private void DrawMenu()
        {
            var ownership = VehicleOwnershipBridge.Current;
            if (ownership == null)
                return;

            var list = ownership.Vehicles
                .Where(v => v.State == VehicleState.Garage)
                .ToList();

            if (list.Count == 0)
            {
                global::GTA.UI.Notification.Show("❌ Nenhum veículo guardado");
                _open = false;
                return;
            }

            if (_index >= list.Count)
                _index = 0;

            if (_index < 0)
                _index = list.Count - 1;

            var data = list[_index];

            global::GTA.UI.Screen.ShowSubtitle(
                $"GARAGEM RLF\n{_index + 1}/{list.Count}\nModelo: {data.Model}\n\n↑ ↓ Navegar\nENTER Retirar",
                1
            );

            if (Game.IsKeyPressed(Keys.Up))
                _index--;

            if (Game.IsKeyPressed(Keys.Down))
                _index++;

            if (Game.IsKeyPressed(Keys.Enter))
                RetrieveVehicle(data);
        }

        private void RetrieveVehicle(VehicleData data)
        {
            Model model = new Model(data.Model);
            model.Request(1000);

            Vehicle v = World.CreateVehicle(model, _spawnPos);
            if (v == null || !v.Exists())
            {
                global::GTA.UI.Notification.Show("❌ Falha ao criar veículo");
                return;
            }

            try { v.PlaceOnGround(); } catch { }

            data.State = VehicleState.World;
            VehicleOwnershipBridge.Current.Save();

            global::GTA.UI.Notification.Show("🚗 Veículo retirado da garagem");
            _open = false;
        }
    }
}
