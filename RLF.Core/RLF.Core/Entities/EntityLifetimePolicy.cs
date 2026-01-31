namespace RLF.Core.Entities
{
    public sealed class EntityLifetimePolicy
    {
        public string Name { get; set; }
        public RLFEntityType[] AffectedTypes { get; set; }
        public string[] AffectedTags { get; set; }
        public float MaxLifetimeSeconds { get; set; }
        public float MaxDistanceFromPlayer { get; set; }
        public int MaxCount { get; set; }
        public float CleanupIntervalSeconds { get; set; }
        public bool LogRemovals { get; set; }

        /// <summary>
        /// 🆕 NOVO: Tags que tornam entidades persistentes (não limpam por distância/tempo)
        /// Útil para veículos de missão, cobertura em combate, etc.
        /// </summary>
        public string[] PersistentTags { get; set; }

        /// <summary>
        /// 🆕 NOVO: Tempo mínimo (segundos) que jogador precisa estar longe antes de cleanup
        /// Previne cleanup imediato quando jogador passa rapidamente pela área
        /// </summary>
        public float MinimumDistanceTime { get; set; }

        public static EntityLifetimePolicy DefaultVehicle => new EntityLifetimePolicy
        {
            Name = "DefaultVehicle",
            AffectedTypes = new[] { RLFEntityType.Vehicle },
            MaxLifetimeSeconds = 300f,  // 5 minutos
            MaxDistanceFromPlayer = 200f,  // 200 metros
            MaxCount = 10,
            CleanupIntervalSeconds = 30f,
            LogRemovals = true,
            PersistentTags = new[] { "mission", "combat", "player_owned", "important" },
            MinimumDistanceTime = 30f  // 🆕 30s longe antes de limpar
        };

        public static EntityLifetimePolicy DefaultPed => new EntityLifetimePolicy
        {
            Name = "DefaultPed",
            AffectedTypes = new[] { RLFEntityType.Ped },
            MaxLifetimeSeconds = 180f,  // 3 minutos
            MaxDistanceFromPlayer = 150f,  // 150 metros
            MaxCount = 20,
            CleanupIntervalSeconds = 30f,
            LogRemovals = true,
            PersistentTags = new[] { "mission", "companion", "important_npc" },
            MinimumDistanceTime = 20f  // 🆕 20s longe antes de limpar
        };

        public static EntityLifetimePolicy DefaultBlip => new EntityLifetimePolicy
        {
            Name = "DefaultBlip",
            AffectedTypes = new[] { RLFEntityType.Blip },
            MaxLifetimeSeconds = 0f,  // Sem limite de tempo
            MaxDistanceFromPlayer = 0f,  // Sem limite de distância
            MaxCount = 50,
            CleanupIntervalSeconds = 60f,
            LogRemovals = false,
            PersistentTags = new[] { "waypoint", "mission", "permanent" },
            MinimumDistanceTime = 0f  // Blips não usam hysteresis
        };

        public static EntityLifetimePolicy Aggressive => new EntityLifetimePolicy
        {
            Name = "Aggressive",
            AffectedTypes = null,  // Afeta todos os tipos
            MaxLifetimeSeconds = 120f,  // 2 minutos
            MaxDistanceFromPlayer = 100f,  // 100 metros
            MaxCount = 5,
            CleanupIntervalSeconds = 15f,
            LogRemovals = true,
            PersistentTags = new[] { "mission", "combat" },
            MinimumDistanceTime = 10f  // 🆕 10s longe antes de limpar (mais rápido)
        };

        /// <summary>
        /// 🆕 NOVO: Política para entidades de missão (nunca limpa)
        /// </summary>
        public static EntityLifetimePolicy Mission => new EntityLifetimePolicy
        {
            Name = "Mission",
            AffectedTypes = null,
            AffectedTags = new[] { "mission", "quest", "story" },
            MaxLifetimeSeconds = 0f,  // Nunca expira
            MaxDistanceFromPlayer = 0f,  // Sem limite de distância
            MaxCount = 0,  // Sem limite de quantidade
            CleanupIntervalSeconds = 0f,  // Não limpa automaticamente
            LogRemovals = true,
            PersistentTags = new[] { "mission", "quest", "story" },
            MinimumDistanceTime = 0f
        };

        /// <summary>
        /// 🆕 NOVO: Política para entidades temporárias (limpa rápido)
        /// </summary>
        public static EntityLifetimePolicy Temporary => new EntityLifetimePolicy
        {
            Name = "Temporary",
            AffectedTypes = null,
            AffectedTags = new[] { "temp", "disposable", "effect" },
            MaxLifetimeSeconds = 30f,  // 30 segundos
            MaxDistanceFromPlayer = 50f,  // 50 metros
            MaxCount = 100,  // Pode ter muitos temporários
            CleanupIntervalSeconds = 10f,  // Limpa frequentemente
            LogRemovals = false,  // Não loga (muito spam)
            PersistentTags = null,  // Nenhuma tag persistente
            MinimumDistanceTime = 0f  // Limpa imediatamente
        };

        /// <summary>
        /// 🆕 NOVO: Verifica se uma tag torna a entidade persistente
        /// </summary>
        public bool IsPersistentTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || PersistentTags == null || PersistentTags.Length == 0)
                return false;

            foreach (var persistentTag in PersistentTags)
            {
                if (string.Equals(tag, persistentTag, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}