using System;
using System.Collections.Generic;

namespace RLF.Core.Loggin
{
    /// <summary>
    /// Rate limiter para evitar spam de logs.
    /// Limita quantidade de logs por categoria em uma janela de tempo.
    /// </summary>
    public sealed class LogRateLimiter
    {
        private struct RateLimitEntry
        {
            public int Count;
            public DateTime WindowStart;
            public int Suppressed;
        }

        private readonly Dictionary<string, RateLimitEntry> _limits;
        private readonly object _lock;
        private readonly int _maxPerWindow;
        private readonly TimeSpan _windowDuration;

        private int _totalSuppressed;

        public int TotalSuppressed => _totalSuppressed;

        /// <summary>
        /// Cria um rate limiter.
        /// </summary>
        public LogRateLimiter(int maxPerWindow = 10, float windowSeconds = 1f)
        {
            _maxPerWindow = Math.Max(1, maxPerWindow);
            _windowDuration = TimeSpan.FromSeconds(Math.Max(0.1f, windowSeconds));
            _limits = new Dictionary<string, RateLimitEntry>(StringComparer.Ordinal);
            _lock = new object();
        }

        /// <summary>
        /// Verifica se um log deve ser permitido.
        /// </summary>
        public bool ShouldAllow(string category)
        {
            if (string.IsNullOrEmpty(category))
                return true;

            var now = DateTime.Now;

            lock (_lock)
            {
                if (!_limits.TryGetValue(category, out var entry))
                {
                    _limits[category] = new RateLimitEntry
                    {
                        Count = 1,
                        WindowStart = now,
                        Suppressed = 0
                    };
                    return true;
                }

                if (now - entry.WindowStart > _windowDuration)
                {
                    _limits[category] = new RateLimitEntry
                    {
                        Count = 1,
                        WindowStart = now,
                        Suppressed = 0
                    };
                    return true;
                }

                if (entry.Count < _maxPerWindow)
                {
                    entry.Count++;
                    _limits[category] = entry;
                    return true;
                }

                entry.Suppressed++;
                _limits[category] = entry;
                _totalSuppressed++;
                return false;
            }
        }

        public int GetSuppressedCount(string category)
        {
            lock (_lock)
            {
                if (_limits.TryGetValue(category, out var entry))
                {
                    return entry.Suppressed;
                }
            }
            return 0;
        }

        public void Reset(string category)
        {
            lock (_lock)
            {
                _limits.Remove(category);
            }
        }

        public void ResetAll()
        {
            lock (_lock)
            {
                _limits.Clear();
                _totalSuppressed = 0;
            }
        }

        public string GetStats()
        {
            lock (_lock)
            {
                return $"[RateLimiter] Categories={_limits.Count} | Suppressed={_totalSuppressed}";
            }
        }
    }
}