namespace RLF.Core.Crime
{
    /// <summary>
    /// Classificação de severidade dos crimes.
    /// Define impacto no Heat, resposta policial e elegibilidade para prisão.
    /// </summary>
    public enum CrimeSeverity
    {
        None = 0,
        
        /// <summary>
        /// Infração leve. Multa ou advertência.
        /// Heat mínimo, resposta policial baixa.
        /// </summary>
        Infraction = 1,
        
        /// <summary>
        /// Contravenção. Multa ou detenção curta.
        /// Heat baixo, resposta policial moderada.
        /// </summary>
        Misdemeanor = 2,
        
        /// <summary>
        /// Crime grave. Prisão média.
        /// Heat alto, resposta policial séria.
        /// </summary>
        Felony = 3,
        
        /// <summary>
        /// Crime violento grave. Prisão longa.
        /// Heat muito alto, resposta policial máxima.
        /// </summary>
        ViolentFelony = 4,
        
        /// <summary>
        /// Crime capital. Prisão perpétua ou pena máxima.
        /// Heat extremo, caçada policial ativa.
        /// </summary>
        Capital = 5
    }
}
