namespace RLF.Core.Crime
{
    /// <summary>
    /// Estados de pressão criminal (Heat).
    /// Define nível de atenção policial e comportamento do sistema.
    /// </summary>
    public enum HeatState
    {
        /// <summary>
        /// Sem atividade criminal recente.
        /// Polícia não procura ativamente.
        /// </summary>
        None = 0,

        /// <summary>
        /// Atividade criminal leve recente.
        /// Polícia pode estar em alerta na região.
        /// </summary>
        Low = 1,

        /// <summary>
        /// Crimes moderados ou reincidência.
        /// Polícia patrulha ativamente áreas relacionadas.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// Crimes graves ou múltiplos crimes.
        /// Polícia busca ativamente pelo suspeito.
        /// </summary>
        High = 3,

        /// <summary>
        /// Crimes violentos ou fuga prolongada.
        /// Caçada policial ativa com recursos extras.
        /// </summary>
        Critical = 4,

        /// <summary>
        /// Múltiplos crimes graves ou resistência prolongada.
        /// Operação especial para captura.
        /// </summary>
        Extreme = 5
    }
}
