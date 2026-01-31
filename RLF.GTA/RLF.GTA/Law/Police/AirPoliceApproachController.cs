using GTA;
using GTA.Native;
using GTA.UI;
using Vector3 = global::GTA.Math.Vector3;

namespace RLF.GTA.Law.Police
{
    public sealed class AirPoliceApproachController
    {
        private readonly AirPoliceTarget _target;

        private AirPoliceApproachState _state;
        private int _stateUntil;

        private const int WARNING_TIME = 60000; // 1 minuto
        private const float DESCENT_ALTITUDE = 200f;
        private const float LANDING_ALTITUDE = 50f;

        private bool _warningNotified;
        private bool _wantedApplied;
        private bool _fineApplied;

        public bool IsFinished => _state == AirPoliceApproachState.Finished;

        public AirPoliceApproachController(AirPoliceTarget target)
        {
            _target = target;
            _state = AirPoliceApproachState.WarningPhase;
            _stateUntil = Game.GameTime + WARNING_TIME;
            _warningNotified = false;
            _wantedApplied = false;
            _fineApplied = false;

            Notification.Show("~o~ALERTA DA FORÇA AÉREA\n~w~Você está voando sem CHT válido!\n~y~Desça abaixo de 200m em 1 minuto!");
        }

        public void Tick()
        {
            if (!_target.IsValid())
            {
                Finish();
                return;
            }

            // Player morreu - termina
            if (_target.Ped.IsDead)
            {
                Finish();
                return;
            }

            switch (_state)
            {
                case AirPoliceApproachState.WarningPhase:
                    UpdateWarningPhase();
                    break;

                case AirPoliceApproachState.InterceptionPhase:
                    UpdateInterceptionPhase();
                    break;
            }
        }

        private void UpdateWarningPhase()
        {
            float altitude = GetCurrentAltitude();

            // Player desceu a tempo
            if (altitude < DESCENT_ALTITUDE)
            {
                Notification.Show("~g~Você desceu a tempo. Infração evitada!");
                Finish();
                return;
            }

            // Avisos progressivos
            int timeLeft = (_stateUntil - Game.GameTime) / 1000;
            if (timeLeft > 0 && timeLeft % 20 == 0 && !_warningNotified)
            {
                Notification.Show($"~o~ÚLTIMA CHANCE!\n~w~Desça abaixo de 200m\n~r~{timeLeft}s restantes");
                _warningNotified = true;
            }

            if (timeLeft % 20 != 0)
                _warningNotified = false;

            // Tempo esgotou - ativa wanted
            if (Game.GameTime > _stateUntil)
            {
                ActivateWanted();
                _state = AirPoliceApproachState.InterceptionPhase;
            }
        }

        private void UpdateInterceptionPhase()
        {
            float altitude = GetCurrentAltitude();

            // Verifica se saiu da aeronave
            if (!_target.Ped.IsInVehicle(_target.Aircraft))
            {
                Notification.Show("~o~Você saiu da aeronave.\n~w~Fuja da polícia ou se entregue!");
                Finish();
                return;
            }

            // WANTED CONSTANTE enquanto voando
            if (Game.Player.WantedLevel < 4)
            {
                Game.Player.WantedLevel = 4;
            }

            // Mantém wanted sempre visível (não deixa diminuir)
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player.Handle, false);

            // Se pousou (abaixo de 50m) - aplica multa
            if (altitude < LANDING_ALTITUDE && !_fineApplied)
            {
                ApplyPenalties();
                _fineApplied = true;
                Notification.Show("~r~MULTA APLICADA!\n~o~Voo ilegal detectado\n~w~Wanted permanece ativo - fuja ou se entregue!");
            }
        }

        private void ActivateWanted()
        {
            if (_wantedApplied)
                return;

            Game.Player.WantedLevel = 4;
            _wantedApplied = true;

            Notification.Show("~r~FORÇA AÉREA ACIONADA!\n~o~4 ESTRELAS ATIVADAS\n~w~Pouse e fuja ou será abatido!");
        }

        private void ApplyPenalties()
        {
            try
            {
                const int FINE_NO_CHT = 15000;

                var economy = RLF.Core.RLFCore.Instance?.Economy;
                if (economy != null && economy.Wallet != null)
                {
                    economy.Wallet.ApplyTransaction(
                        new RLF.Core.Economy.Transactions.EconomyTransaction(
                            amount: -FINE_NO_CHT,
                            type: RLF.Core.Economy.Transactions.TransactionType.Fine,
                            legality: RLF.Core.Economy.Transactions.TransactionLegality.Legal,
                            origin: RLF.Core.Economy.Transactions.TransactionOrigin.Fine,
                            description: "Voo ilegal sem CHT válido"
                        )
                    );
                }

                var docSystem = RLF.Core.RLFCore.Instance?.Systems?.Get("DocumentSystem")
                    as RLF.Core.Identity.DocumentSystem;

                if (docSystem != null)
                {
                    docSystem.DetectViolation(
                        RLF.Core.Identity.Enums.ViolationType.DrivingWithoutLicense,
                        RLF.Core.Identity.Enums.ViolationSeverity.Critical,
                        "Voo ilegal sem CHT válido - Força Aérea acionada"
                    );
                }
            }
            catch { }
        }

        private float GetCurrentAltitude()
        {
            float groundZ = World.GetGroundHeight(_target.Aircraft.Position);
            return _target.Aircraft.Position.Z - groundZ;
        }

        private void Finish()
        {
            _state = AirPoliceApproachState.Finished;
        }
    }
}