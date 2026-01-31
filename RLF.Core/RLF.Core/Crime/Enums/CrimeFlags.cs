using System;

namespace RLF.Core.Crime
{
    /// <summary>
    /// Flags de estado e características de um crime.
    /// Usadas para classificação, elegibilidade e reação do sistema.
    /// </summary>
    [Flags]
    public enum CrimeFlags
    {
        None = 0,

        /// <summary>
        /// Crime elegível para prisão (quando sistema de prisão existir).
        /// </summary>
        EligibleForArrest = 1 << 0,

        /// <summary>
        /// Crime envolveu violência física.
        /// </summary>
        Violent = 1 << 1,

        /// <summary>
        /// Arma de fogo foi usada ou exibida.
        /// </summary>
        WeaponUsed = 1 << 2,

        /// <summary>
        /// Houve testemunhas diretas.
        /// </summary>
        Witnessed = 1 << 3,

        /// <summary>
        /// Crime foi reportado às autoridades.
        /// </summary>
        Reported = 1 << 4,

        /// <summary>
        /// Vítima ficou ferida.
        /// </summary>
        VictimInjured = 1 << 5,

        /// <summary>
        /// Vítima morreu.
        /// </summary>
        VictimKilled = 1 << 6,

        /// <summary>
        /// Ocorreu em território de gangue.
        /// </summary>
        GangTerritory = 1 << 7,

        /// <summary>
        /// Crime foi gravado por câmera.
        /// </summary>
        CameraRecorded = 1 << 8,

        /// <summary>
        /// Suspeito foi identificado claramente.
        /// </summary>
        SuspectIdentified = 1 << 9,

        /// <summary>
        /// Veículo do suspeito foi identificado.
        /// </summary>
        VehicleIdentified = 1 << 10,

        /// <summary>
        /// Houve fuga após o crime.
        /// </summary>
        Fled = 1 << 11,

        /// <summary>
        /// Houve resistência a autoridade.
        /// </summary>
        Resisted = 1 << 12
    }
}
