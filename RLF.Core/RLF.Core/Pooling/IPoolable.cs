namespace RLF.Core.Pooling
{
    /// <summary>
    /// Interface para objetos que podem ser reutilizados em pools.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Reseta o objeto para estado inicial antes de retornar ao pool.
        /// </summary>
        void Reset();
    }
}