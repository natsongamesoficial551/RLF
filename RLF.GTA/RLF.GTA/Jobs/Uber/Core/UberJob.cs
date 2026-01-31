// ===============================
// UberJob.cs
// ===============================
using System;
using RLF.Core.Economy;
using RLF.Core.Events;
using RLF.Core.Jobs.Core;
using RLF.Core.Jobs.Enums;
using RLF.Core.Logging;
using RLF.GTA.Jobs.Uber.Core;
using RLF.GTA.Jobs.Uber.History;
using RLF.GTA.Jobs.Uber.Penalty;
using RLF.GTA.Jobs.Uber.Rating;
using RLF.GTA.Jobs.Uber.Ride;
using RLF.GTA.Jobs.Uber.Storage;

namespace RLF.GTA.Jobs.Uber
{
    public sealed class UberJob
    {
        private readonly Logger _logger;
        private readonly EventManager _eventManager;
        private readonly EconomySystem _economy;

        public UberSettings Settings { get; }
        public UberAccount Account { get; }
        public RideHistory History { get; }
        public RideManager RideManager { get; }
        public RatingSystem RatingSystem { get; }
        public PenaltySystem PenaltySystem { get; }

        private readonly IUberStore _store;
        private readonly Random _rng;

        public bool AppActive { get; set; }

        public UberJob(
            Logger logger,
            EventManager eventManager,
            EconomySystem economy)
        {
            _logger = logger;
            _eventManager = eventManager;
            _economy = economy;
            _rng = new Random();

            Settings = new UberSettings();
            _store = new UberStore("scripts/RLF/uber.ini");

            var loaded = _store.Load();
            Account = loaded.account;
            History = loaded.history;

            RideManager = new RideManager(logger);
            RatingSystem = new RatingSystem(logger);
            PenaltySystem = new PenaltySystem(logger, Settings);

            AppActive = false;

            _logger.Info($"[Uber] Sistema carregado - Avaliação: {Account.AverageRating:F2} | Corridas: {Account.TotalRides}");
        }

        public void Save()
        {
            _store.Save(Account, History);
            _logger.Info("[Uber] Progresso salvo");
        }

        public void CompleteRide(float timeExpectedSeconds)
        {
            var ride = RideManager.CurrentRide;

            decimal payment = RidePaymentCalculator.CalculatePayment(ride, Settings);
            decimal tip = RidePaymentCalculator.CalculateTip(ride, Account.AverageRating, Settings, _rng);
            decimal total = payment + tip;

            float rating = RatingSystem.CalculateRideRating(ride, (int)timeExpectedSeconds);

            bool success = _economy.Wallet.ApplyTransaction(
                new RLF.Core.Economy.Transactions.EconomyTransaction(
                    total,
                    RLF.Core.Economy.Transactions.TransactionType.Income,
                    RLF.Core.Economy.Transactions.TransactionLegality.Legal,
                    RLF.Core.Economy.Transactions.TransactionOrigin.Salary,
                    "Corrida Uber"
                )
            );

            if (success)
            {
                Account.RecordRide(rating, total);

                var record = new RideRecord
                {
                    Date = DateTime.UtcNow,
                    Category = ride.Category,
                    Origin = "Ponto de Coleta",
                    Destination = "Destino",
                    Distance = ride.DistanceTraveled,
                    Payment = payment,
                    Tip = tip,
                    Rating = rating,
                    Events = $"Batidas: {ride.CrashCount}"
                };
                History.AddRecord(record);

                Save();

                _logger.Info($"[Uber] Corrida concluída - ${total:F2} | {rating:F1}★");
            }
            else
            {
                _logger.Error("[Uber] Falha ao aplicar transação");
            }
        }
    }
}