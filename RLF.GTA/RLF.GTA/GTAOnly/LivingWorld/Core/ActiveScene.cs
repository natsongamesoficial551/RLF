using RLF.GTA.GTAOnly.LivingWorld.Core;

namespace RLF.GTA.GTAOnly.LivingWorld.Core
{
    /// <summary>
    /// Cena ativa no mundo (wrapper de evento)
    /// </summary>
    public class ActiveScene
    {
        public WorldEvent Event { get; }

        public ActiveScene(WorldEvent worldEvent)
        {
            Event = worldEvent;
        }

        public void Update()
        {
            Event.Update();
        }

        public void Stop()
        {
            Event.Stop();
        }
    }
}
