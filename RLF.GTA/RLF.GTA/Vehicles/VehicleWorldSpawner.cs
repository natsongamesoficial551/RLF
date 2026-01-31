using GTA;
using RLF.Core.Vehicles;
using System;

namespace RLF.GTA.Vehicles
{
    /// <summary>
    /// Inicializador do sistema de veículos.
    /// IMPORTANTE:
    /// - NÃO altera estado de veículo.
    /// - Apenas garante inicialização única.
    /// </summary>
    public sealed class VehicleWorldSpawner : Script
    {
        private bool _initialized;

        public VehicleWorldSpawner()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_initialized)
                return;

            var ownership = VehicleOwnershipBridge.Current;
            if (ownership == null)
                return;

            // ❌ NÃO mexer em State aqui
            // World / Garage / Impound são responsabilidade de outros sistemas

            _initialized = true;
        }
    }
}
