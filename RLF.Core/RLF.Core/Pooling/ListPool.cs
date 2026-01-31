using System;
using System.Collections.Generic;

namespace RLF.Core.Pooling
{
    /// <summary>
    /// Pool especializado para List&lt;T&gt;.
    /// Evita alocações frequentes de listas temporárias.
    /// </summary>
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> _pool = new Stack<List<T>>();
        private static readonly object _lock = new object();
        private static readonly int _maxSize = 50;
        private static readonly int _defaultCapacity = 16;

        private static int _totalRented;
        private static int _totalReturned;

        /// <summary>
        /// Obtém uma lista do pool.
        /// </summary>
        public static List<T> Rent()
        {
            lock (_lock)
            {
                _totalRented++;

                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }
            }

            return new List<T>(_defaultCapacity);
        }

        /// <summary>
        /// Obtém uma lista com capacidade específica.
        /// </summary>
        public static List<T> Rent(int capacity)
        {
            var list = Rent();

            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }

            return list;
        }

        /// <summary>
        /// Devolve uma lista ao pool.
        /// </summary>
        public static void Return(List<T> list)
        {
            if (list == null)
                return;

            list.Clear();

            lock (_lock)
            {
                _totalReturned++;

                if (_pool.Count < _maxSize)
                {
                    _pool.Push(list);
                }
            }
        }

        /// <summary>
        /// Executa uma ação com uma lista temporária.
        /// Automaticamente devolve ao pool após uso.
        /// </summary>
        public static void Use(Action<List<T>> action)
        {
            var list = Rent();
            try
            {
                action(list);
            }
            finally
            {
                Return(list);
            }
        }

        /// <summary>
        /// Executa uma função com uma lista temporária e retorna resultado.
        /// </summary>
        public static TResult Use<TResult>(Func<List<T>, TResult> func)
        {
            var list = Rent();
            try
            {
                return func(list);
            }
            finally
            {
                Return(list);
            }
        }

        public static string GetStats()
        {
            lock (_lock)
            {
                return $"[ListPool<{typeof(T).Name}>] " +
                       $"Size={_pool.Count}/{_maxSize} | " +
                       $"Rented={_totalRented} | " +
                       $"Returned={_totalReturned}";
            }
        }
    }
}