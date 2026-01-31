using System;
using RLF.Core.Jobs.Shift;

namespace RLF.GTA.Jobs.Delivery
{
    public sealed class DeliveryShiftManager
    {
        private ShiftType _lastCompletedShift;
        private DateTime _lastShiftCompletedAt;

        public DeliveryShiftManager()
        {
            _lastCompletedShift = ShiftType.Night;
            _lastShiftCompletedAt = DateTime.MinValue;
        }

        public bool CanStartMorningShift(DateTime currentTime)
        {
            // ✅ MODO TESTE: Sempre permite
            return true;
        }

        public bool CanStartAfternoonShift(DateTime currentTime)
        {
            // ✅ MODO TESTE: Sempre permite
            return true;
        }

        public string GetUnavailabilityMessage(DateTime currentTime)
        {
            return "Turno disponível";
        }

        public void OnShiftCompleted(ShiftType type)
        {
            _lastCompletedShift = type;
            _lastShiftCompletedAt = DateTime.Now;
        }

        public ShiftType? GetAvailableShift(DateTime currentTime)
        {
            // ✅ CORREÇÃO: Retorna o turno baseado no HORÁRIO ATUAL
            TimeSpan now = currentTime.TimeOfDay;

            // Manhã: 8h-12h
            if (now >= new TimeSpan(8, 0, 0) && now < new TimeSpan(12, 0, 0))
            {
                return ShiftType.Morning;
            }

            // Tarde: 16h-20h
            if (now >= new TimeSpan(16, 0, 0) && now < new TimeSpan(20, 0, 0))
            {
                return ShiftType.Afternoon;
            }

            // ✅ MODO TESTE: Se estiver fora do horário, retorna o turno mais próximo
            // Para teste, sempre retorna Afternoon se for depois das 12h
            if (now >= new TimeSpan(12, 0, 0))
            {
                return ShiftType.Afternoon;
            }

            // Antes das 12h, retorna Morning
            return ShiftType.Morning;
        }
    }
}