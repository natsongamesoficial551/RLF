using System;
using RLF.Core.Economy;
using RLF.Core.Events;
using RLF.Core.Jobs.Core;
using RLF.Core.Jobs.Enums;
using RLF.Core.Jobs.Payment;
using RLF.Core.Jobs.Shift;
using RLF.Core.Logging;

namespace RLF.GTA.Jobs.Delivery
{
    public sealed class DeliveryJob : JobBase
    {
        private readonly DeliveryShiftManager _shiftManager;

        public DeliveryShiftManager ShiftManager => _shiftManager;

        public DeliveryJob(
            Logger logger,
            EventManager eventManager,
            EconomySystem economy)
            : base(
                JobType.Delivery,
                logger,
                eventManager,
                economy,
                new PaymentSettings
                {
                    BasePayPerTask = 25m,
                    ShiftCompletionBonus = 100m,
                    EnableBonuses = true
                })
        {
            _shiftManager = new DeliveryShiftManager();

            _schedules.Add(new ShiftSchedule(
                ShiftType.Morning,
                new TimeSpan(8, 0, 0),
                new TimeSpan(12, 0, 0),
                minTasks: 7,
                maxTasks: 12
            ));

            _schedules.Add(new ShiftSchedule(
                ShiftType.Afternoon,
                new TimeSpan(16, 0, 0),
                new TimeSpan(20, 0, 0),
                minTasks: 7,
                maxTasks: 12
            ));
        }

        protected override void StartShift(ShiftSchedule schedule)
        {
            try
            {
                ShiftType? availableShift = _shiftManager.GetAvailableShift(DateTime.Now);

                if (!availableShift.HasValue)
                {
                    _logger.Warning($"[Delivery] Nenhum turno disponível no momento");
                    return;
                }

                if (availableShift.Value != schedule.Type)
                {
                    _logger.Warning($"[Delivery] Turno disponível ({availableShift.Value}) diferente do solicitado ({schedule.Type})");
                    return;
                }

                base.StartShift(schedule);
            }
            catch (Exception ex)
            {
                _logger.Error("[Delivery] Erro em StartShift", ex);
            }
        }

        protected override void CompleteShift()
        {
            ShiftType completedType = CurrentShift.Type;

            base.CompleteShift();

            _shiftManager.OnShiftCompleted(completedType);

            _logger.Info($"[Delivery] Turno {completedType} registrado como concluído");
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