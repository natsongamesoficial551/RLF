using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using RLF.Core.Economy;
using RLF.Core.Economy.Transactions;
using RLF.GTA.CoreIntegration;

namespace RLF.GTA.CoreIntegration.IllegalWeapons
{
    public sealed class IllegalWeaponMarketController : Script
    {
        private sealed class IllegalMarket
        {
            public Vector3 Position;
            public bool WithVan;

            public Ped Dealer;
            public Vehicle Van;
            public Blip Blip;
        }

        private readonly List<IllegalMarket> _markets = new List<IllegalMarket>();
        private IllegalMarket _activeMarket;

        private readonly MenuPool _menuPool;
        private readonly UIMenu _menu;

        private EconomySystem _economy;
        private bool _initialized;

        public IllegalWeaponMarketController()
        {
            _menuPool = new MenuPool();
            _menu = new UIMenu("Mercado Ilegal", "ARMAS SEM REGISTRO");
            _menuPool.Add(_menu);

            Tick += OnTick;
            Aborted += OnAbort;
        }

        // =====================================================
        // INIT
        // =====================================================

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            // Pega o EconomySystem do bridge (igual a Concessionária)
            _economy = EconomyBridge.Current;

            CreateMarkets();
            BuildMenu();

            _initialized = true;
        }

        // =====================================================
        // MARKETS
        // =====================================================

        private void CreateMarkets()
        {
            // Loja ilegal 1 (com van)
            CreateMarket(new Vector3(-2159.147f, 5196.89f, 20f), withVan: true);

            // Loja ilegal 2 (sem van)
            CreateMarket(new Vector3(-35.702f, -1554.494f, 30.677f), withVan: false);
        }

        private void CreateMarket(Vector3 pos, bool withVan)
        {
            var market = new IllegalMarket
            {
                Position = pos,
                WithVan = withVan
            };

            // BLIP
            try
            {
                Blip blip = World.CreateBlip(pos);
                blip.Sprite = BlipSprite.AmmuNationShootingRange;
                blip.Color = BlipColor.Red;
                blip.IsShortRange = true;
                blip.Name = "Loja de Armas ilegal";
                market.Blip = blip;
            }
            catch { }

            // NPC Dealer
            Ped dealer = CreateDealer(pos);
            if (dealer == null || !dealer.Exists())
            {
                // Se falhou criar dealer, remove blip e não registra mercado (evita null refs)
                try { market.Blip?.Delete(); } catch { }
                return;
            }
            market.Dealer = dealer;

            // VAN (opcional)
            if (withVan)
            {
                Vehicle van = CreateVanNearDealer(pos, dealer.Heading);
                if (van != null && van.Exists())
                    market.Van = van;
            }

            _markets.Add(market);
        }

        private Ped CreateDealer(Vector3 pos)
        {
            try
            {
                Model pedModel = new Model(PedHash.Dealer01SMY);
                if (!RequestModel(pedModel, 1500))
                    return null;

                Ped dealer = World.CreatePed(pedModel, pos);
                if (dealer == null || !dealer.Exists())
                    return null;

                dealer.IsPersistent = true;
                dealer.BlockPermanentEvents = true;
                dealer.Task.StandStill(-1);

                // braços cruzados (se falhar, sem problema)
                try
                {
                    Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, dealer.Handle, "WORLD_HUMAN_GUARD_STAND", 0, true);
                }
                catch { }

                pedModel.MarkAsNoLongerNeeded();
                return dealer;
            }
            catch
            {
                return null;
            }
        }

        private Vehicle CreateVanNearDealer(Vector3 pos, float heading)
        {
            try
            {
                Model vanModel = new Model(VehicleHash.Speedo);
                if (!RequestModel(vanModel, 1500))
                    return null;

                Vector3 vanPos = pos + new Vector3(2.0f, 0f, 0f);
                Vehicle van = World.CreateVehicle(vanModel, vanPos, heading);
                if (van == null || !van.Exists())
                    return null;

                van.IsPersistent = true;

                // abrir porta traseira / porta-malas via native (mais compatível)
                try
                {
                    // 5 = trunk em GTA V
                    Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, van.Handle, 5, false, false);
                }
                catch { }

                vanModel.MarkAsNoLongerNeeded();
                return van;
            }
            catch
            {
                return null;
            }
        }

        private bool RequestModel(Model model, int timeoutMs)
        {
            if (!model.IsInCdImage || !model.IsValid)
                return false;

            int start = Game.GameTime;
            model.Request();

            while (!model.IsLoaded && (Game.GameTime - start) < timeoutMs)
                Wait(0);

            return model.IsLoaded;
        }

        // =====================================================
        // MENU
        // =====================================================

        private void BuildMenu()
        {
            _menu.Clear();

            // ===== ARMAS DE CONTATO =====
            AddWeapon("Faca", WeaponHash.Knife, 800);
            AddWeapon("Taco de Beisebol", WeaponHash.Bat, 600);
            AddWeapon("Bastão", WeaponHash.Nightstick, 900);
            AddWeapon("Soco Inglês", WeaponHash.KnuckleDuster, 500);
            AddWeapon("Machado", WeaponHash.Hatchet, 1200);
            AddWeapon("Pé de Cabra", WeaponHash.Crowbar, 700);
            AddWeapon("Taco de Golf", WeaponHash.GolfClub, 650);
            AddWeapon("Martelo", WeaponHash.Hammer, 850);
            AddWeapon("Adaga", WeaponHash.Dagger, 950);
            AddWeapon("Machete", WeaponHash.Machete, 1100);
            AddWeapon("Chave Inglesa", WeaponHash.Wrench, 750);

            // ===== PISTOLAS =====
            AddWeapon("Pistola", WeaponHash.Pistol, 2500);
            AddWeapon("Pistola .50", WeaponHash.Pistol50, 3800);
            AddWeapon("Pistola Pesada", WeaponHash.HeavyPistol, 4800);
            AddWeapon("SNS Pistol", WeaponHash.SNSPistol, 1800);
            AddWeapon("Vintage Pistol", WeaponHash.VintagePistol, 3200);
            AddWeapon("AP Pistol", WeaponHash.APPistol, 5500);
            AddWeapon("Combat Pistol", WeaponHash.CombatPistol, 4200);
            AddWeapon("Pistol MK2", WeaponHash.PistolMk2, 5800);

            // ===== REVÓLVERES =====
            AddWeapon("Revólver", WeaponHash.Revolver, 3500);
            AddWeapon("Double Action Revolver", WeaponHash.DoubleActionRevolver, 4000);

            // ===== SMGs / SUBMETRALHADORAS =====
            AddWeapon("Micro SMG", WeaponHash.MicroSMG, 6500);
            AddWeapon("SMG", WeaponHash.SMG, 8500);
            AddWeapon("SMG MK2", WeaponHash.SMGMk2, 10500);
            AddWeapon("Assault SMG", WeaponHash.AssaultSMG, 9800);
            AddWeapon("Combat PDW", WeaponHash.CombatPDW, 9200);
            AddWeapon("Machine Pistol", WeaponHash.MachinePistol, 7500);
            AddWeapon("Mini SMG", WeaponHash.MiniSMG, 7800);

            // ===== SHOTGUNS / ESCOPETAS =====
            AddWeapon("Shotgun", WeaponHash.PumpShotgun, 7200);
            AddWeapon("Sawed-Off Shotgun", WeaponHash.SawnOffShotgun, 6800);
            AddWeapon("Assault Shotgun", WeaponHash.AssaultShotgun, 11500);
            AddWeapon("Bullpup Shotgun", WeaponHash.BullpupShotgun, 10800);
            AddWeapon("Heavy Shotgun", WeaponHash.HeavyShotgun, 9500);
            AddWeapon("Double Barrel Shotgun", WeaponHash.DoubleBarrelShotgun, 8200);
            AddWeapon("Sweeper Shotgun", WeaponHash.SweeperShotgun, 12000);

            // ===== RIFLES DE ASSALTO =====
            AddWeapon("Assault Rifle", WeaponHash.AssaultRifle, 16500);
            AddWeapon("Carbine Rifle", WeaponHash.CarbineRifle, 14000);
            AddWeapon("Carbine Rifle MK2", WeaponHash.CarbineRifleMk2, 17000);
            AddWeapon("Advanced Rifle", WeaponHash.AdvancedRifle, 15500);
            AddWeapon("Special Carbine", WeaponHash.SpecialCarbine, 15800);
            AddWeapon("Special Carbine MK2", WeaponHash.SpecialCarbineMk2, 18500);
            AddWeapon("Bullpup Rifle", WeaponHash.BullpupRifle, 14800);
            AddWeapon("Bullpup Rifle MK2", WeaponHash.BullpupRifleMk2, 17500);
            AddWeapon("Compact Rifle", WeaponHash.CompactRifle, 13500);
            AddWeapon("Military Rifle", WeaponHash.MilitaryRifle, 21000);

            // ===== METRALHADORAS =====
            AddWeapon("MG", WeaponHash.MG, 18000);
            AddWeapon("Combat MG", WeaponHash.CombatMG, 22000);
            AddWeapon("Combat MG MK2", WeaponHash.CombatMGMk2, 25000);
            AddWeapon("Gusenberg Sweeper", WeaponHash.Gusenberg, 19500);

            // ===== RIFLES DE PRECISÃO =====
            AddWeapon("Sniper Rifle", WeaponHash.SniperRifle, 24000);
            AddWeapon("Heavy Sniper", WeaponHash.HeavySniper, 32000);
            AddWeapon("Heavy Sniper MK2", WeaponHash.HeavySniperMk2, 38000);
            AddWeapon("Marksman Rifle", WeaponHash.MarksmanRifle, 28000);
            AddWeapon("Marksman Rifle MK2", WeaponHash.MarksmanRifleMk2, 33000);

            // ===== EXPLOSIVOS / LANÇADORES =====
            AddWeapon("Granada", WeaponHash.Grenade, 3500);
            AddWeapon("Sticky Bomb", WeaponHash.StickyBomb, 4500);
            AddWeapon("Bomba de Proximidade", WeaponHash.ProximityMine, 5000);
            AddWeapon("Molotov", WeaponHash.Molotov, 2800);
            AddWeapon("Grenade Launcher", WeaponHash.GrenadeLauncher, 28000);
            AddWeapon("Compact Grenade Launcher", WeaponHash.CompactGrenadeLauncher, 24000);

            _menu.RefreshIndex();
        }

        private void AddWeapon(string name, WeaponHash hash, int price)
        {
            var item = new UIMenuItem($"{name} - ${price:N0}", "Arma ilegal com munição completa");
            _menu.AddItem(item);

            item.Activated += (sender, selectedItem) =>
            {
                TryBuyWeapon(hash, price);
            };
        }

        // =====================================================
        // ECONOMY
        // =====================================================

        private void TryBuyWeapon(WeaponHash weapon, int price)
        {
            // segurança: economy pode ainda não ter subido
            if (_economy == null)
            {
                global::GTA.UI.Notification.Show("❌ Sistema econômico indisponível");
                return;
            }

            decimal priceDec = price;

            if (_economy.Wallet.Balance < priceDec)
            {
                global::GTA.UI.Notification.Show("❌ Dinheiro insuficiente");
                return;
            }

            bool applied = _economy.Wallet.ApplyTransaction(
                new EconomyTransaction(
                    -priceDec,
                    TransactionType.Expense,
                    TransactionLegality.Illegal,
                    TransactionOrigin.Unknown,
                    $"Compra ilegal: {weapon}"
                )
            );

            if (!applied)
            {
                global::GTA.UI.Notification.Show("❌ Falha ao processar pagamento");
                return;
            }

            Ped player = Game.Player.Character;
            player.Weapons.Give(weapon, 999, true, true);

            // zera procurado (durante e após)
            Game.Player.WantedLevel = 0;

            global::GTA.UI.Notification.Show($"⚠️ Arma adquirida (-${price:N0})");
        }

        // =====================================================
        // TICK
        // =====================================================

        private void OnTick(object sender, EventArgs e)
        {
            EnsureInitialized();

            // processa NativeUI
            _menuPool.ProcessMenus();

            // keep wanted 0 quando estiver negociando (anti polícia)
            if (_menu.Visible)
                Game.Player.WantedLevel = 0;

            Ped player = Game.Player.Character;
            _activeMarket = null;

            foreach (var market in _markets)
            {
                if (market == null)
                    continue;

                bool dealerOk = market.Dealer != null && market.Dealer.Exists();
                bool vanOk = market.Van != null && market.Van.Exists();

                float distDealer = dealerOk ? player.Position.DistanceTo(market.Dealer.Position) : float.MaxValue;
                float distVan = vanOk ? player.Position.DistanceTo(market.Van.Position) : float.MaxValue;

                if (distDealer < 2.5f || distVan < 2.5f)
                {
                    _activeMarket = market;

                    global::GTA.UI.Screen.ShowHelpTextThisFrame("Pressione ~INPUT_CONTEXT~ para negociar");

                    if (Game.IsControlJustPressed(global::GTA.Control.Context))
                    {
                        _menu.Visible = !_menu.Visible;
                        if (_menu.Visible)
                            Game.Player.WantedLevel = 0;
                    }

                    return;
                }
            }

            // fora de qualquer mercado
            _menu.Visible = false;
        }

        // =====================================================
        // CLEANUP
        // =====================================================

        private void OnAbort(object sender, EventArgs e)
        {
            try { _menu.Visible = false; } catch { }

            foreach (var market in _markets)
            {
                try { market?.Blip?.Delete(); } catch { }
                try { market?.Dealer?.Delete(); } catch { }
                try { market?.Van?.Delete(); } catch { }
            }

            _markets.Clear();
        }
    }
}