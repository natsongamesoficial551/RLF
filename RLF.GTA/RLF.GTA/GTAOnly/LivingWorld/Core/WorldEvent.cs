namespace RLF.GTA.GTAOnly.LivingWorld.Core
{
    /// <summary>
    /// Evento abstrato do mundo.
    /// Tudo que acontecer no LivingWorld herda disso.
    /// </summary>
    public abstract class WorldEvent
    {
        public bool IsActive { get; private set; }

        /// <summary>Nome amigável pro HUD/Debug/Blip</summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary>Posição principal do evento (para blip e limpeza)</summary>
        public virtual global::GTA.Math.Vector3 Position { get; protected set; }

        public void Start(WorldEventContext context)
        {
            if (IsActive)
                return;

            IsActive = OnStart(context);
        }

        public void Update()
        {
            if (!IsActive)
                return;

            OnUpdate();
        }

        public void Stop()
        {
            if (!IsActive)
                return;

            OnStop();
            IsActive = false;
        }

        protected abstract bool OnStart(WorldEventContext context);
        protected abstract void OnUpdate();
        protected abstract void OnStop();
    }
}
