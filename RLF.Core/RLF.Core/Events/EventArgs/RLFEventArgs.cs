using System;

namespace RLF.Core.Events.EventArgs
{
    /// <summary>
    /// Classe base para todos os argumentos de eventos do RLF.
    /// Fornece informações comuns como timestamp e possibilidade de cancelamento.
    /// </summary>
    public class RLFEventArgs : System.EventArgs
    {
        /// <summary>
        /// Momento em que o evento foi criado.
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Indica se o evento pode ser cancelado por handlers.
        /// </summary>
        public bool IsCancellable { get; private set; }

        /// <summary>
        /// Indica se o evento foi cancelado por algum handler.
        /// Apenas válido se IsCancellable = true.
        /// </summary>
        public bool IsCancelled { get; set; }

        /// <summary>
        /// Dados customizados que podem ser anexados ao evento.
        /// Útil para passar informações adicionais sem criar novas classes.
        /// </summary>
        public object CustomData { get; set; }

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        /// <param name="cancellable">Define se o evento pode ser cancelado</param>
        public RLFEventArgs(bool cancellable = false)
        {
            Timestamp = DateTime.Now;
            IsCancellable = cancellable;
            IsCancelled = false;
            CustomData = null;
        }

        /// <summary>
        /// Tenta cancelar o evento. Retorna true se foi cancelado com sucesso.
        /// </summary>
        /// <returns>True se o evento foi cancelado, false se não é cancelável</returns>
        public bool TryCancel()
        {
            if (!IsCancellable)
                return false;

            IsCancelled = true;
            return true;
        }
    }

    /// <summary>
    /// EventArgs genérico com dados tipados.
    /// Permite passar dados de forma type-safe sem criar múltiplas classes.
    /// </summary>
    /// <typeparam name="T">Tipo dos dados carregados pelo evento</typeparam>
    public class RLFEventArgs<T> : RLFEventArgs
    {
        /// <summary>
        /// Dados tipados carregados pelo evento.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Construtor com dados.
        /// </summary>
        /// <param name="data">Dados a serem carregados</param>
        /// <param name="cancellable">Define se o evento pode ser cancelado</param>
        public RLFEventArgs(T data, bool cancellable = false) : base(cancellable)
        {
            Data = data;
        }
    }
}