using GTA;
using GTA.Math;
using RLF.Core;
using RLF.Core.Economy.Transactions;
using RLF.Core.Vehicles;
using RLF.GTA.CoreIntegration;
using System.Linq;
using System.Windows.Forms;

namespace RLF.GTA.Vehicles
{
    public sealed class VehicleImpoundMenu : Script
    {
        private readonly Vector3 _impoundPos = new Vector3(441.482f, -999.975f, 30.723f);
        private readonly Vector3 _spawnPos = new Vector3(434.173f, -1020.335f, 28.808f);

        private Blip _impoundBlip;

        private bool _open;
        private int _index;

        private const int RETRIEVE_COST = 250;

        public VehicleImpoundMenu()
        {
            Tick += OnTick;
            Aborted += OnAborted;

            CreateBlip();
        }

        private void CreateBlip()
        {
            _impoundBlip = World.CreateBlip(_impoundPos);
            _impoundBlip.Sprite = BlipSprite.PersonalVehicleCar;
            _impoundBlip.Color = BlipColor.White;
            _impoundBlip.Scale = 0.85f;
            _impoundBlip.Name = "Pátio de Veículos";
            _impoundBlip.IsShortRange = true;
        }

        private void OnTick(object sender, System.EventArgs e)
        {
            if (Game.Player.Character.Position.DistanceTo(_impoundPos) > 2f)
                return;

            global::GTA.UI.Screen.ShowHelpTextThisFrame(
                "Pressione ~INPUT_CONTEXT~ para acessar o pátio"
            );

            if (Game.IsKeyPressed(Keys.E))
                _open = !_open;

            if (_open)
                DrawMenu();
        }

        private void DrawMenu()
        {
            var ownership = VehicleOwnershipBridge.Current;
            if (ownership == null)
                return;

            var list = ownership.Vehicles
                .Where(v => v.State == VehicleState.Impound)
                .ToList();

            if (list.Count == 0)
            {
                global::GTA.UI.Notification.Show("❌ Nenhum veículo no pátio");
                _open = false;
                return;
            }

            if (_index >= list.Count)
                _index = 0;

            var selected = list[_index];

            global::GTA.UI.Screen.ShowSubtitle(
                $"PÁTIO\n{_index + 1}/{list.Count}\nModelo: {selected.Model}\nENTER Retirar ($250)",
                1
            );

            if (Game.IsKeyPressed(Keys.Up))
                _index--;

            if (Game.IsKeyPressed(Keys.Down))
                _index++;

            if (_index < 0)
                _index = list.Count - 1;

            if (Game.IsKeyPressed(Keys.Enter))
                RetrieveVehicle(selected);
        }

        private void RetrieveVehicle(VehicleData data)
        {
            var economy = RLFCore.Instance.Economy;
            if (economy.Wallet.Balance < RETRIEVE_COST)
            {
                global::GTA.UI.Notification.Show("❌ Dinheiro insuficiente");
                return;
            }

            economy.Wallet.ApplyTransaction(
                new EconomyTransaction(
                    -RETRIEVE_COST,
                    TransactionType.Expense,
                    TransactionLegality.Legal,
                    TransactionOrigin.Fine,
                    "Retirada do pátio"
                )
            );

            Model m = new Model(data.Model);
            m.Request(1000);

            Vehicle v = World.CreateVehicle(m, _spawnPos);
            if (v == null || !v.Exists())
                return;

            v.IsPersistent = true;                 // 🔥 FIX
            v.PreviouslyOwnedByPlayer = true;      // 🔥 FIX

            data.State = VehicleState.World;
            data.PosX = 0;
            data.PosY = 0;
            data.PosZ = 0;
            data.Heading = v.Heading;

            VehicleOwnershipBridge.Current.Save();  // 🔥 GARANTE CICLO

            global::GTA.UI.Notification.Show("🚗 Veículo retirado do pátio");
            _open = false;
        }


        private void OnAborted(object sender, System.EventArgs e)
        {
            try { _impoundBlip?.Delete(); } catch { }
        }
    }
}
