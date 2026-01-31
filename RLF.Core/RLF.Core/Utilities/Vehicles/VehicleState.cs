namespace RLF.Core.Vehicles
{
    /// <summary>
    /// Estado lógico do veículo no sistema.
    /// </summary>
    public enum VehicleState
    {
        World,   // No mundo
        Garage,  // Guardado em garagem (vanilla)
        Impound  // No pátio
    }
}
