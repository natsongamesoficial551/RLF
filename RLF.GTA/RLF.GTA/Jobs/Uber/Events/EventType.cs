// ===============================
// EventType.cs
// ===============================
namespace RLF.GTA.Jobs.Uber.Events
{
    public enum EventType
    {
        None,
        TrafficJam,          // Trânsito intenso
        Accident,            // Acidente no trajeto
        UrgentRide,          // Corrida urgente
        VIPPassenger,        // Passageiro VIP
        NightRide,           // Corrida noturna perigosa
        PoliceIntervention,  // Intervenção policial
        BadWeather,          // Clima ruim
        Construction         // Obra na via
    }
}