// ===============================
// PassengerEventType.cs
// ===============================
namespace RLF.GTA.Jobs.Uber.Passenger
{
    public enum PassengerEventType
    {
        None,
        Complaining,         // Reclamando
        Drunk,               // Bêbado
        RequestSpeedUp,      // Pedindo para correr
        ChangeDestination,   // Mudança de destino
        TryExitEarly,        // Tentando sair antes
        Friendly,            // Amigável (aumenta gorjeta)
        Impatient,           // Impaciente
        OnPhone              // Falando ao telefone (quieto)
    }
}