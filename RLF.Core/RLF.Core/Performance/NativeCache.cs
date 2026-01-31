using System;
using System.Collections.Generic;

namespace RLF.Core.Performance
{
    /// <summary>
    /// Cache genérico com TTL para resultados de natives/operações caras.
    /// GTA-SAFE: Thread-safe, sem alocações no hit.
    /// </summary>
    public sealed class NativeCache<TKey, TValue>
    {
        private struct CacheEntry
        {
            public TValue Value;
            public long ExpirationMs;
            public bool HasValue;
        }

        private readonly Dictionary<TKey, CacheEntry> _cache;
        private readonly object _lock;
        private readonly int _defaultTTLMs;
        private readonly int _maxEntries;

        private long _hits;
        private long _misses;

        public long Hits => _hits;
        public long Misses => _misses;
        public int Count => _cache.Count;
        public double HitRate => (_hits + _misses) > 0
            ? (double)_hits / (_hits + _misses) * 100
            : 0;

        /// <summary>
        /// Cria um cache com TTL padrão.
        /// </summary>
        /// <param name="defaultTTLMs">Tempo de vida padrão em ms</param>
        /// <param name="maxEntries">Máximo de entradas (0 = ilimitado)</param>
        public NativeCache(int defaultTTLMs = 500, int maxEntries = 1000)
        {
            _defaultTTLMs = Math.Max(1, defaultTTLMs);
            _maxEntries = maxEntries;
            _cache = new Dictionary<TKey, CacheEntry>();
            _lock = new object();
        }

        /// <summary>
        /// Tenta obter valor do cache.
        /// </summary>
        public bool TryGet(TKey key, out TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    if (entry.HasValue && now < entry.ExpirationMs)
                    {
                        value = entry.Value;
                        _hits++;
                        return true;
                    }

                    // Expirado - remove
                    _cache.Remove(key);
                }

                value = default;
                _misses++;
                return false;
            }
        }

        /// <summary>
        /// Obtém valor do cache ou executa factory se não existir/expirado.
        /// </summary>
        public TValue GetOrAdd(TKey key, Func<TValue> factory, int? ttlMs = null)
        {
            if (TryGet(key, out var cached))
                return cached;

            var value = factory();
            Set(key, value, ttlMs);
            return value;
        }

        /// <summary>
        /// Define valor no cache.
        /// </summary>
        public void Set(TKey key, TValue value, int? ttlMs = null)
        {
            int ttl = ttlMs ?? _defaultTTLMs;
            long expiration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ttl;

            lock (_lock)
            {
                // Evita crescimento infinito
                if (_maxEntries > 0 && _cache.Count >= _maxEntries && !_cache.ContainsKey(key))
                {
                    CleanExpired();

                    // Se ainda cheio, remove o primeiro
                    if (_cache.Count >= _maxEntries)
                    {
                        var enumerator = _cache.GetEnumerator();
                        if (enumerator.MoveNext())
                        {
                            _cache.Remove(enumerator.Current.Key);
                        }
                    }
                }

                _cache[key] = new CacheEntry
                {
                    Value = value,
                    ExpirationMs = expiration,
                    HasValue = true
                };
            }
        }

        /// <summary>
        /// Invalida uma entrada específica.
        /// </summary>
        public bool Invalidate(TKey key)
        {
            lock (_lock)
            {
                return _cache.Remove(key);
            }
        }

        /// <summary>
        /// Limpa entradas expiradas.
        /// </summary>
        public int CleanExpired()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var toRemove = new List<TKey>();

            lock (_lock)
            {
                foreach (var kvp in _cache)
                {
                    if (now >= kvp.Value.ExpirationMs)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in toRemove)
                {
                    _cache.Remove(key);
                }
            }

            return toRemove.Count;
        }

        /// <summary>
        /// Limpa todo o cache.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _hits = 0;
                _misses = 0;
            }
        }

        /// <summary>
        /// Retorna estatísticas do cache.
        /// </summary>
        public string GetStats()
        {
            return $"[Cache] Entries={Count} | Hits={_hits} | Misses={_misses} | HitRate={HitRate:F1}%";
        }
    }
}