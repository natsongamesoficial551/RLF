using System;
using System.Collections.Generic;
using System.Text;

namespace RLF.Core.Pooling
{
    /// <summary>
    /// Pool especializado para StringBuilder.
    /// Essencial para evitar alocações em formatação de strings.
    /// </summary>
    public static class StringBuilderPool
    {
        private static readonly Stack<StringBuilder> _pool = new Stack<StringBuilder>();
        private static readonly object _lock = new object();
        private static readonly int _maxSize = 30;
        private static readonly int _defaultCapacity = 256;
        private static readonly int _maxCapacity = 4096;

        private static int _totalRented;
        private static int _totalReturned;

        /// <summary>
        /// Obtém um StringBuilder do pool.
        /// </summary>
        public static StringBuilder Rent()
        {
            lock (_lock)
            {
                _totalRented++;

                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }
            }

            return new StringBuilder(_defaultCapacity);
        }

        /// <summary>
        /// Obtém um StringBuilder com capacidade específica.
        /// </summary>
        public static StringBuilder Rent(int capacity)
        {
            var sb = Rent();

            if (sb.Capacity < capacity)
            {
                sb.Capacity = capacity;
            }

            return sb;
        }

        /// <summary>
        /// Devolve um StringBuilder ao pool.
        /// </summary>
        public static void Return(StringBuilder sb)
        {
            if (sb == null)
                return;

            // Não guarda StringBuilders muito grandes
            if (sb.Capacity > _maxCapacity)
                return;

            sb.Clear();

            lock (_lock)
            {
                _totalReturned++;

                if (_pool.Count < _maxSize)
                {
                    _pool.Push(sb);
                }
            }
        }

        /// <summary>
        /// Executa uma ação com um StringBuilder temporário.
        /// </summary>
        public static void Use(Action<StringBuilder> action)
        {
            var sb = Rent();
            try
            {
                action(sb);
            }
            finally
            {
                Return(sb);
            }
        }

        /// <summary>
        /// Constrói uma string usando o pool.
        /// </summary>
        public static string Build(Action<StringBuilder> builder)
        {
            var sb = Rent();
            try
            {
                builder(sb);
                return sb.ToString();
            }
            finally
            {
                Return(sb);
            }
        }

        /// <summary>
        /// Concatena strings sem alocações intermediárias.
        /// </summary>
        public static string Concat(params string[] parts)
        {
            if (parts == null || parts.Length == 0)
                return string.Empty;

            if (parts.Length == 1)
                return parts[0] ?? string.Empty;

            var sb = Rent();
            try
            {
                foreach (var part in parts)
                {
                    if (part != null)
                        sb.Append(part);
                }
                return sb.ToString();
            }
            finally
            {
                Return(sb);
            }
        }

        /// <summary>
        /// Formata uma string usando pool.
        /// </summary>
        public static string Format(string format, params object[] args)
        {
            var sb = Rent();
            try
            {
                sb.AppendFormat(format, args);
                return sb.ToString();
            }
            finally
            {
                Return(sb);
            }
        }

        public static string GetStats()
        {
            lock (_lock)
            {
                return $"[StringBuilderPool] " +
                       $"Size={_pool.Count}/{_maxSize} | " +
                       $"Rented={_totalRented} | " +
                       $"Returned={_totalReturned}";
            }
        }
    }
}