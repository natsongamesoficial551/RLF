// ===============================
// UberAppInterface.cs
// ===============================
using System;
using RLF.Core.Logging;

namespace RLF.GTA.Jobs.Uber.Phone
{
    public sealed class UberAppInterface
    {
        private readonly Logger _logger;
        private bool _isActive;

        public bool IsActive => _isActive;

        public UberAppInterface(Logger logger)
        {
            _logger = logger;
            _isActive = false;
        }

        public void Activate()
        {
            if (_isActive)
                return;

            _isActive = true;
            _logger.Info("[Uber] App ativado");
        }

        public void Deactivate()
        {
            if (!_isActive)
                return;

            _isActive = false;
            _logger.Info("[Uber] App desativado");
        }

        public string GetStatusMessage(Core.UberAccount account)
        {
            if (account.IsBanned)
            {
                TimeSpan remaining = account.BannedUntil.Value - DateTime.UtcNow;
                return $"❌ Conta suspensa\nTempo restante: {remaining.Hours}h {remaining.Minutes}m";
            }

            if (_isActive)
            {
                return $"✅ App Ativo\n⭐ {account.AverageRating:F2}/5.0\n🚗 {account.TotalRides} corridas\n💰 ${account.TotalEarned:F2} total";
            }

            return "🔴 App Desativado";
        }
    }
}