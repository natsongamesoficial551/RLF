namespace RLF.Core.Entities
{
    /// <summary>
    /// Tipos de entidades rastreáveis pelo EntityRegistry.
    /// Prefixo RLF para evitar conflito com GTA.EntityType.
    /// </summary>
    public enum RLFEntityType
    {
        Vehicle = 0,
        Ped = 1,
        Object = 2,
        Blip = 3,
        Pickup = 4,
        Checkpoint = 5,
        Particle = 6,
        Audio = 7,
        Camera = 8,
        Unknown = 99
    }
}