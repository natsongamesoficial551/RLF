using RLF.Core.Economy;
using RLF.Core.Economy.Transactions;
using RLF.Core.Events;
using RLF.Core.Jobs.Enums;
using RLF.Core.Jobs.Events;
using RLF.Core.Jobs.Payment;
using RLF.Core.Jobs.Shift;
using RLF.Core.Logging;
using System;
using System.Collections.Generic;

namespace RLF.Core.Jobs.Core
{
    public abstract class JobBase
    {
        protected readonly Logger _logger;
        protected readonly EventManager _eventManager;
        protected readonly EconomySystem _economy;

        public JobType Type { get; }
        public JobStatus Status { get; protected set; }
        public ShiftState CurrentShift { get; }
        public PaymentSettings PaymentSettings { get; }

        protected readonly List<ShiftSchedule> _schedules;
        protected readonly Random _rng;

        protected JobBase(
            JobType type,
            Logger logger,
            EventManager eventManager,
            EconomySystem economy,
            PaymentSettings paymentSettings)
        {
            Type = type;
            _logger = logger;
            _eventManager = eventManager;
            _economy = economy;
            PaymentSettings = paymentSettings;

            CurrentShift = new ShiftState();
            _schedules = new List<ShiftSchedule>();
            _rng = new Random();
            Status = JobStatus.Inactive;
        }

        public bool TryStartShift(DateTime currentTime)
        {
            if (Status == JobStatus.OnShift)
                return false;

            ShiftSchedule availableShift = FindAvailableShift(currentTime);
            if (availableShift == null)
                return false;

            StartShift(availableShift);
            return true;
        }

        protected virtual void StartShift(ShiftSchedule schedule)
        {
            CurrentShift.Type = schedule.Type;
            CurrentShift.IsActive = true;
            CurrentShift.StartedAt = DateTime.Now;
            CurrentShift.TasksTotal = schedule.GenerateTaskCount(_rng);
            CurrentShift.TasksCompleted = 0;

            Status = JobStatus.OnShift;

            _eventManager.Raise(
                "job:shift_started",
                new ShiftStartedEvent(Type, schedule.Type, CurrentShift.TasksTotal)
            );

            _logger.Info($"[{Type}] Turno iniciado: {schedule.Type} ({CurrentShift.TasksTotal} tarefas)");
        }

        public void CompleteTask()
        {
            if (Status != JobStatus.OnShift || !CurrentShift.IsActive)
                return;

            CurrentShift.TasksCompleted++;

            _eventManager.Raise(
                "job:task_completed",
                new TaskCompletedEvent(Type, CurrentShift.TasksCompleted, CurrentShift.TasksRemaining)
            );

            if (CurrentShift.IsCompleted)
            {
                CompleteShift();
            }
        }

        protected virtual void CompleteShift()
        {
            decimal payment = PaymentCalculator.CalculateShiftPayment(
                CurrentShift.TasksCompleted,
                PaymentSettings
            );

            _economy.ApplyTransaction(
                new EconomyTransaction(
                    payment,
                    TransactionType.Income,
                    TransactionLegality.Legal,
                    TransactionOrigin.Salary,
                    $"Turno {CurrentShift.Type} concluído ({Type})"
                )
            );

            _eventManager.Raise(
                "job:shift_completed",
                new ShiftCompletedEvent(Type, CurrentShift.Type, CurrentShift.TasksCompleted, payment)
            );

            _logger.Info($"[{Type}] Turno concluído: {CurrentShift.Type} | Pagamento: ${payment:F2}");

            CurrentShift.IsActive = false;
            Status = JobStatus.Completed;
        }

        protected ShiftSchedule FindAvailableShift(DateTime currentTime)
        {
            foreach (var schedule in _schedules)
            {
                if (schedule.IsAvailable(currentTime))
                    return schedule;
            }
            return null;
        }

        public abstract string GetStatusMessage();
    }
}