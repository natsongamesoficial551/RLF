using System;
using System.Collections.Generic;

namespace RLF.Core.Pooling
{
    /// <summary>
    /// Pool genérico de objetos para evitar alocações.
    /// Thread-safe e GTA-safe.
    /// </summary>
    public sealed class ObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _pool;
        private readonly object _lock;
        private readonly int _maxSize;
        private readonly Action<T> _resetAction;
        private readonly Func<T> _factory;

        private int _totalCreated;
        private int _totalReused;
        private int _totalReturned;

        public int Count
        {
            get { lock (_lock) return _pool.Count; }
        }

        public int TotalCreated => _totalCreated;
        public int TotalReused => _totalReused;
        public int TotalReturned => _totalReturned;
        public int MaxSize => _maxSize;

        /// <summary>
        /// Cria um pool de objetos.
        /// </summary>
        /// <param name="maxSize">Tamanho máximo do pool (0 = ilimitado)</param>
        /// <param name="preWarm">Quantidade de objetos para pré-criar</param>
        /// <param name="resetAction">Ação para resetar objeto antes de devolver ao pool</param>
        /// <param name="factory">Factory customizada (opcional)</param>
        public ObjectPool(
            int maxSize = 100,
            int preWarm = 0,
            Action<T> resetAction = null,
            Func<T> factory = null)
        {
            _maxSize = Math.Max(0, maxSize);
            _resetAction = resetAction;
            _factory = factory ?? (() => new T());
            _pool = new Stack<T>(_maxSize > 0 ? Math.Min(_maxSize, 32) : 32);
            _lock = new object();

            // Pré-aquece o pool
            if (preWarm > 0)
            {
                int toCreate = _maxSize > 0 ? Math.Min(preWarm, _maxSize) : preWarm;
                for (int i = 0; i < toCreate; i++)
                {
                    _pool.Push(_factory());
                    _totalCreated++;
                }
            }
        }

        /// <summary>
        /// Obtém um objeto do pool ou cria um novo.
        /// </summary>
        public T Get()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    _totalReused++;
                    return _pool.Pop();
                }
            }

            _totalCreated++;
            return _factory();
        }

        /// <summary>
        /// Devolve um objeto ao pool.
        /// </summary>
        public void Return(T item)
        {
            if (item == null)
                return;

            // Reseta o objeto
            _resetAction?.Invoke(item);

            // Se implementa IPoolable, chama Reset
            if (item is IPoolable poolable)
            {
                poolable.Reset();
            }

            lock (_lock)
            {
                // Só adiciona se não exceder limite
                if (_maxSize <= 0 || _pool.Count < _maxSize)
                {
                    _pool.Push(item);
                    _totalReturned++;
                }
                // Se excedeu, deixa o GC coletar
            }
        }

        /// <summary>
        /// Limpa o pool.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _pool.Clear();
            }
        }

        /// <summary>
        /// Retorna estatísticas do pool.
        /// </summary>
        public string GetStats()
        {
            return $"[Pool<{typeof(T).Name}>] " +
                   $"Size={Count}/{_maxSize} | " +
                   $"Created={_totalCreated} | " +
                   $"Reused={_totalReused} | " +
                   $"Returned={_totalReturned}";
        }
    }
}