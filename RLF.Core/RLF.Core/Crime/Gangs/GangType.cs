namespace RLF.Core.Gangs
{
    /// <summary>
    /// Todas as gangues disponíveis no GTA V
    /// </summary>
    public enum GangType
    {
        // ===== GANGUES DE RUA (LOS SANTOS) =====
        Families,           // The Families (Verde) - Grove Street
        Ballas,             // Ballas (Roxo) - Rival das Families
        Vagos,              // Los Santos Vagos (Amarelo) - Gangue mexicana
        Marabunta,          // Marabunta Grande (Azul) - Salvadorenhos

        // ===== GANGUES ORGANIZADAS =====
        ArmenianMob,        // Máfia Armênia
        TriadTong,          // Tríade Chinesa
        KoreanMob,          // Máfia Coreana

        // ===== GRUPOS MOTORIZADOS =====
        LostMC,             // The Lost MC (Motociclistas)

        // ===== GRUPOS DIVERSOS =====
        Rednecks,           // Rednecks de Blaine County
        Hippies,            // Hippies (Pacíficos, não criminosos)

        // ===== CRIMINOSOS INDEPENDENTES =====
        Independent         // Jogador sem gangue
    }

    /// <summary>
    /// Extensões para GangType
    /// </summary>
    public static class GangTypeExtensions
    {
        public static string GetDisplayName(this GangType gang)
        {
            switch (gang)
            {
                case GangType.Families: return "The Families";
                case GangType.Ballas: return "Ballas";
                case GangType.Vagos: return "Los Santos Vagos";
                case GangType.Marabunta: return "Marabunta Grande";
                case GangType.ArmenianMob: return "Armenian Mob";
                case GangType.TriadTong: return "Triad Tong";
                case GangType.KoreanMob: return "Korean Mob";
                case GangType.LostMC: return "The Lost MC";
                case GangType.Rednecks: return "Rednecks";
                case GangType.Hippies: return "Hippies";
                case GangType.Independent: return "Independent";
                default: return gang.ToString();
            }
        }

        public static string GetColor(this GangType gang)
        {
            switch (gang)
            {
                case GangType.Families: return "~g~";      // Verde
                case GangType.Ballas: return "~p~";        // Roxo
                case GangType.Vagos: return "~y~";         // Amarelo
                case GangType.Marabunta: return "~b~";     // Azul
                case GangType.ArmenianMob: return "~u~";   // Cinza
                case GangType.TriadTong: return "~r~";     // Vermelho
                case GangType.KoreanMob: return "~c~";     // Ciano
                case GangType.LostMC: return "~o~";        // Laranja
                case GangType.Rednecks: return "~t~";      // Marrom
                case GangType.Hippies: return "~m~";       // Rosa
                case GangType.Independent: return "~w~";   // Branco
                default: return "~w~";
            }
        }

        public static bool IsStreetGang(this GangType gang)
        {
            return gang == GangType.Families ||
                   gang == GangType.Ballas ||
                   gang == GangType.Vagos ||
                   gang == GangType.Marabunta;
        }

        public static bool IsOrganizedCrime(this GangType gang)
        {
            return gang == GangType.ArmenianMob ||
                   gang == GangType.TriadTong ||
                   gang == GangType.KoreanMob;
        }

        public static bool IsPeaceful(this GangType gang)
        {
            return gang == GangType.Hippies;
        }
    }
}
