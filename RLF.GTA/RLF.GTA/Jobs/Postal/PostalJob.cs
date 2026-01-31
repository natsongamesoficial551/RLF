using System;
using RLF.Core.Economy;
using RLF.Core.Events;
using RLF.Core.Jobs.Core;
using RLF.Core.Jobs.Enums;
using RLF.Core.Jobs.Payment;
using RLF.Core.Jobs.Shift;
using RLF.Core.Logging;

namespace RLF.GTA.Jobs.Postal
{
    public sealed class PostalJob : JobBase
    {
        private readonly PostalShiftManager _shiftManager;

        public PostalShiftManager ShiftManager => _shiftManager;

        public PostalJob(
            Logger logger,
            EventManager eventManager,
            EconomySystem economy)
            : base(
                JobType.Delivery, // Usando o mesmo JobType por ora
                logger,
                eventManager,
                economy,
                new PaymentSettings
                {
                    BasePayPerTask = 18m,      // Paga menos que entregador ($18 vs $25)
                    ShiftCompletionBonus = 75m, // Bônus menor ($75 vs $100)
                    EnableBonuses = true
                })
        {
            _shiftManager = new PostalShiftManager();

            // Turno da Manhã: 8h-12h (4-7 entregas)
            _schedules.Add(new ShiftSchedule(
                ShiftType.Morning,
                new TimeSpan(8, 0, 0),
                new TimeSpan(12, 0, 0),
                minTasks: 4,
                maxTasks: 7
            ));

            // Turno da Tarde: 14h-18h (4-7 entregas)
            _schedules.Add(new ShiftSchedule(
                ShiftType.Afternoon,
                new TimeSpan(14, 0, 0),
                new TimeSpan(18, 0, 0),
                minTasks: 4,
                maxTasks: 7
            ));
        }

        protected override void StartShift(ShiftSchedule schedule)
        {
            try
            {
                ShiftType? availableShift = _shiftManager.GetAvailableShift(DateTime.Now);

                if (!availableShift.HasValue)
                {
                    _logger.Warning($"[Postal] Nenhum turno disponível no momento");
                    return;
                }

                if (availableShift.Value != schedule.Type)
                {
                    _logger.Warning($"[Postal] Turno disponível ({availableShift.Value}) diferente do solicitado ({schedule.Type})");
                    return;
                }

                base.StartShift(schedule);
            }
            catch (Exception ex)
            {
                _logger.Error("[Postal] Erro em StartShift", ex);
            }
        }

        protected override void CompleteShift()
        {
            ShiftType completedType = CurrentShift.Type;

            base.CompleteShift();

            _shiftManager.OnShiftCompleted(completedType);

            _logger.Info($"[Postal] Turno {completedType} registrado como concluído");
        }

        public override string GetStatusMessage()
        {
            if (Status == JobStatus.OnShift && CurrentShift.IsActive)
            {
                return $"Entregas restantes: {CurrentShift.TasksRemaining}/{CurrentShift.TasksTotal}";
            }

            return _shiftManager.GetUnavailabilityMessage(DateTime.Now);
        }
    }
}
