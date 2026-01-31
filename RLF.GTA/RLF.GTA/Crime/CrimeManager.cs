using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using RLF.Core.Crime;
using RLF.Core.Economy;
using RLF.Core.Economy.Debt;
using RLF.Core.Economy.Expenses;
using RLF.Core.Economy.Wallet;
using RLF.GTA.Gangs;
using System;
using CoreCrimeType = RLF.Core.Crime.CrimeType;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Script principal que gerencia todo o sistema de crimes E gangues
    /// VERSÃO CORRIGIDA - Sem dinheiro inicial ($0) para não conflitar com Character Creator
    /// </summary>
    public class CrimeManager : Script
    {
        private CrimeSystem _crimeSystem;
        private EconomySystem _economySystem;
        private CrimeDetection _crimeDetection;
        private CrimeWitnessScanner _witnessScanner;
        private CrimeReportScheduler _reportScheduler;
        private NPCReactionController _reactionController;
        private VehicleTheftBehavior _vehicleTheft;
        private CrimeHeatFeedback _heatFeedback;
        private RobberyInteraction _robberyInteraction;
        private EstablishmentRobbery _establishmentRobbery;

        // ✅ GANG SYSTEM ADICIONADO (NÃO herda de Script)
        private GangManager _gangManager;

        private bool _isInitialized = false;
        private bool _debugEnabled = true;
        private DateTime _lastDebugTime;

        public CrimeManager()
        {
            try
            {
                CrimeLogger.Initialize();
                CrimeLogger.Log("=== CRIME & GANG MANAGER STARTING ===");

                Notification.Show("~g~[Crime & Gang System] Loading...");

                // ====== CRIME SYSTEM ======
                CrimeLogger.Log("Initializing CrimeSystem...");
                _crimeSystem = new CrimeSystem();
                _crimeSystem.Initialize();
                CrimeLogger.Log("CrimeSystem initialized");

                // ====== ECONOMY SYSTEM ======
                CrimeLogger.Log("Initializing EconomySystem...");
                _economySystem = new EconomySystem(
                    initialBalance: 0m, // ✅ $0 - Character Creator gerencia o dinheiro inicial
                    walletSettings: new WalletSettings
                    {
                        AllowNegativeBalance = true,
                        MinBalanceLimit = -10000m
                    },
                    expenseSettings: new ExpenseSettings
                    {
                        DailyLivingCost = 15m,
                        DailyTransportCost = 5m,
                        WeeklyBasicTax = 50m
                    },
                    debtSettings: new DebtSettings
                    {
                        Enabled = true,
                        DailyInterestRate = 0.02m
                    }
                );
                CrimeLogger.Log("EconomySystem initialized (no initial balance)");

                // ====== CRIME SUBSYSTEMS ======
                CrimeLogger.Log("Initializing crime subsystems...");
                _crimeDetection = new CrimeDetection(_crimeSystem);
                CrimeLogger.Log("- CrimeDetection OK");

                _witnessScanner = new CrimeWitnessScanner(_crimeSystem);
                CrimeLogger.Log("- CrimeWitnessScanner OK");

                _reportScheduler = new CrimeReportScheduler(_crimeSystem);
                CrimeLogger.Log("- CrimeReportScheduler OK");

                _reactionController = new NPCReactionController(_crimeSystem);
                CrimeLogger.Log("- NPCReactionController OK");

                _vehicleTheft = new VehicleTheftBehavior(_crimeSystem);
                CrimeLogger.Log("- VehicleTheftBehavior OK");

                _heatFeedback = new CrimeHeatFeedback(_crimeSystem);
                CrimeLogger.Log("- CrimeHeatFeedback OK");

                _robberyInteraction = new RobberyInteraction(_crimeSystem, _economySystem);
                CrimeLogger.Log("- RobberyInteraction OK");

                _establishmentRobbery = new EstablishmentRobbery(_crimeSystem, _economySystem);
                CrimeLogger.Log("- EstablishmentRobbery OK");

                // ✅ ====== GANG SYSTEM ======
                CrimeLogger.Log("Initializing GangManager...");
                _gangManager = new GangManager(_crimeSystem, _economySystem);
                CrimeLogger.Log("- GangManager OK");
                CrimeLogger.Log("- Territory blips created");
                CrimeLogger.Log("- Gang NPCs spawning enabled");

                // ====== EVENT HANDLERS ======
                CrimeEvents.OnCrimeCommitted += OnCrimeCommitted;
                CrimeEvents.OnHeatChanged += OnHeatChanged;
                CrimeLogger.Log("Event handlers registered");

                _isInitialized = true;
                _lastDebugTime = DateTime.Now;

                Notification.Show("~g~[Crime & Gang System] Loaded Successfully!");
                Notification.Show("~y~F9 = Crime Debug | F7 = Gang Debug");
                CrimeLogger.Log("=== CRIME & GANG MANAGER READY ===");

                Tick += OnTick;
                KeyDown += OnKeyDown;
                Aborted += OnAborted;
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Failed to initialize CrimeManager", ex);
                Notification.Show($"~r~[Crime & Gang System] ERROR: {ex.Message}");
                Notification.Show($"~r~Check CrimeSystem.log for details");
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_isInitialized) return;

            try
            {
                float deltaTime = Game.LastFrameTime;

                // Crime systems
                _crimeSystem.Update(deltaTime);
                _crimeDetection.Update(deltaTime);
                _witnessScanner.Update(deltaTime);
                _reportScheduler.Update(deltaTime);
                _reactionController.Update(deltaTime);
                _vehicleTheft.Update(deltaTime);
                _heatFeedback.Update(deltaTime);
                _robberyInteraction.Update(deltaTime);
                _establishmentRobbery.Update(deltaTime);

                // ✅ Gang system - chama Update manualmente
                _gangManager?.Update(deltaTime);

                // Debug info
                if (_debugEnabled && (DateTime.Now - _lastDebugTime).TotalSeconds >= 1.0)
                {
                    ShowDebugInfo();
                    _lastDebugTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~[Crime & Gang System] Tick Error: {ex.Message}");
                CrimeLogger.LogError("Tick error", ex);
            }
        }

        private void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            try
            {
                // F9 = Crime Debug
                if (e.KeyCode == System.Windows.Forms.Keys.F9)
                {
                    _debugEnabled = !_debugEnabled;
                    Notification.Show(_debugEnabled ? "~g~Crime Debug ON" : "~r~Crime Debug OFF");
                }

                // F10 = Test Crime
                if (e.KeyCode == System.Windows.Forms.Keys.F10)
                {
                    TestCrime();
                }

                // F11 = Clear Heat
                if (e.KeyCode == System.Windows.Forms.Keys.F11)
                {
                    _crimeSystem.ClearHeat();
                    Notification.Show("~g~Heat Cleared!");
                }

                // ✅ F7 é gerenciado pelo GangManager
                _gangManager?.OnKeyDown(e.KeyCode);
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~[Crime & Gang System] Key Error: {ex.Message}");
            }
        }

        private void OnCrimeCommitted(CrimeRecord crime)
        {
            if (crime == null) return;

            CrimeLogger.LogCrime(crime);

            string crimeMsg = $"~r~CRIME: ~w~{crime.Type}~n~~y~Heat: +{(crime.GetHeatContribution() * 100f):F0}%";
            Notification.Show(crimeMsg);

            // ✅ Notifica o GangManager sobre o crime
            _gangManager?.OnPlayerCommittedCrime(crime);

            if (_debugEnabled)
            {
                string debugMsg = $"~w~{crime.LocationName} | {crime.Severity} | Witnessed: {crime.WasWitnessed()}";
                Notification.Show(debugMsg);
            }
        }

        private void OnHeatChanged(float newHeat, HeatState newState)
        {
            CrimeLogger.LogHeatChange(newHeat, newState);

            string stateColor = GetStateColor(newState);
            string msg = $"{stateColor}Heat State: {newState}~n~~w~Level: {(newHeat * 100f):F1}%";
            Notification.Show(msg);
        }

        private void ShowDebugInfo()
        {
            if (!_debugEnabled) return;

            float heat = _crimeSystem.CurrentHeat;
            HeatState state = _crimeSystem.CurrentHeatState;
            int cases = _crimeSystem.ActiveCaseCount;
            int crimes = _crimeSystem.TotalCrimeCount;
            int witnesses = _witnessScanner.GetActiveWitnessCount();
            int reactions = _reactionController.ActiveReactionCount;
            int reports = _reportScheduler.PendingReportCount;
            decimal balance = _economySystem.Wallet.Balance;

            string debugText =
                $"~y~=== CRIME DEBUG ===~n~" +
                $"~g~Money: ${balance:F0}~n~" +
                $"~w~Heat: ~o~{(heat * 100f):F1}% ~w~({state})~n~" +
                $"~w~Cases: ~b~{cases} ~w~Crimes: ~b~{crimes}~n~" +
                $"~w~Witnesses: ~g~{witnesses} ~w~Reactions: ~p~{reactions}~n~" +
                $"~w~Reports: ~r~{reports}";

            Screen.ShowSubtitle(debugText, 1050);
        }

        private void TestCrime()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                Vector3 pos = player.Position;

                var crime = _crimeSystem.RegisterCrime(
                    CoreCrimeType.PublicGunfire,
                    pos.X, pos.Y, pos.Z,
                    "Test Location",
                    "Test Zone"
                );

                if (crime != null)
                {
                    crime.AddFlag(CrimeFlags.Witnessed);
                    Notification.Show("~g~Test crime registered!");
                }
                else
                {
                    Notification.Show("~r~Failed to register test crime!");
                }
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~Test Crime Error: {ex.Message}");
            }
        }

        private string GetStateColor(HeatState state)
        {
            switch (state)
            {
                case HeatState.None: return "~w~";
                case HeatState.Low: return "~y~";
                case HeatState.Medium: return "~o~";
                case HeatState.High: return "~r~";
                case HeatState.Critical: return "~r~";
                case HeatState.Extreme: return "~r~";
                default: return "~w~";
            }
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                Notification.Show("~y~[Crime & Gang System] Shutting down...");

                CrimeEvents.OnCrimeCommitted -= OnCrimeCommitted;
                CrimeEvents.OnHeatChanged -= OnHeatChanged;

                _crimeDetection?.Shutdown();
                _witnessScanner?.Shutdown();
                _reportScheduler?.Shutdown();
                _reactionController?.Shutdown();
                _vehicleTheft?.Shutdown();
                _heatFeedback?.Shutdown();
                _robberyInteraction?.Shutdown();
                _establishmentRobbery?.Shutdown();
                _crimeSystem?.Shutdown();

                // ✅ Shutdown do GangManager
                _gangManager?.Shutdown();

                Notification.Show("~g~[Crime & Gang System] Shutdown complete");
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~[Crime & Gang System] Shutdown Error: {ex.Message}");
            }
        }
    }
}