using System;
using RLF.Core.Jobs.Shift;

namespace RLF.GTA.Jobs.Postal
{
    public sealed class PostalShiftManager
    {
        private ShiftType _lastCompletedShift;
        private DateTime _lastShiftCompletedAt;

        public PostalShiftManager()
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
            TimeSpan now = currentTime.TimeOfDay;

            // Dentro do horário da manhã (05h-10h)
            if (now >= new TimeSpan(5, 0, 0) && now < new TimeSpan(10, 0, 0))
            {
                return "Turno da Manhã disponível (05h-10h)";
            }

            // Dentro do horário da noite (20h-00h)
            if (now >= new TimeSpan(20, 0, 0) || now < new TimeSpan(0, 0, 1))
            {
                return "Turno da Noite disponível (20h-00h)";
            }

            // Fora do horário
            if (now < new TimeSpan(5, 0, 0))
            {
                return "Aguarde até 05h para o turno da manhã";
            }
            else if (now >= new TimeSpan(10, 0, 0) && now < new TimeSpan(20, 0, 0))
            {
                return "Próximo turno: 20h (Noite)";
            }

            return "Nenhum turno disponível no momento";
        }

        public void OnShiftCompleted(ShiftType type)
        {
            _lastCompletedShift = type;
            _lastShiftCompletedAt = DateTime.Now;
        }

        public ShiftType? GetAvailableShift(DateTime currentTime)
        {
            // ✅ Retorna o turno baseado no HORÁRIO ATUAL
            TimeSpan now = currentTime.TimeOfDay;

            // Manhã: 05h-10h
            if (now >= new TimeSpan(5, 0, 0) && now < new TimeSpan(10, 0, 0))
            {
                return ShiftType.Morning;
            }

            // Noite: 20h-00h (até meia-noite)
            if (now >= new TimeSpan(20, 0, 0) || now < new TimeSpan(0, 0, 1))
            {
                return ShiftType.Night;
            }

            // Fora do horário: retorna null (não disponível)
            return null;
        }
    }
}