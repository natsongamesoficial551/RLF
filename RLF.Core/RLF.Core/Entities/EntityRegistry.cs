using System;
using System.Collections.Generic;
using RLF.Core.Logging;
using RLF.Core.Scheduling;

namespace RLF.Core.Entities
{
    /// <summary>
    /// Callback para deletar entidade no jogo.
    /// </summary>
    public delegate bool EntityDeleteHandler(int handle, RLFEntityType type);

    /// <summary>
    /// Callback para verificar se entidade ainda existe.
    /// </summary>
    public delegate bool EntityExistsHandler(int handle, RLFEntityType type);

    /// <summary>
    /// Registro centralizado de entidades com cleanup automático.
    /// 🆕 MELHORADO: Agora com hysteresis de distância e persistent tags
    /// </summary>
    public sealed class EntityRegistry : ISchedulable
    {
        #region Fields

        private readonly Dictionary<int, TrackedEntity> _entities;
        private readonly Dictionary<RLFEntityType, List<int>> _entitiesByType;
        private readonly Dictionary<string, List<int>> _entitiesByTag;
        private readonly Dictionary<string, List<int>> _entitiesByOwner;
        private readonly List<EntityLifetimePolicy> _policies;
        private readonly object _lock;
        private readonly Logger _logger;

        private EntityDeleteHandler _deleteHandler;
        private EntityExistsHandler _existsHandler;

        private float _playerX, _playerY, _playerZ;

        private long _totalCreated;
        private long _totalRemoved;
        private long _totalCleanedByTime;
        private long _totalCleanedByDistance;
        private long _totalCleanedByLimit;
        private long _totalCleanedByInvalid;

        // 🆕 NOVO: Tracking de hysteresis de distância
        private long _totalPreventedByHysteresis;
        private readonly Dictionary<int, DateTime> _distanceExceedStart;

        private int _tickInterval;
        private DateTime _lastCleanup;
        private float _cleanupIntervalSeconds;

        #endregion

        #region Properties

        public int Count
        {
            get { lock (_lock) return _entities.Count; }
        }

        public long TotalCreated => _totalCreated;
        public long TotalRemoved => _totalRemoved;

        /// <summary>
        /// 🆕 NOVO: Quantidade de cleanups prevenidos por hysteresis
        /// </summary>
        public long TotalPreventedByHysteresis => _totalPreventedByHysteresis;

        #endregion

        #region ISchedulable

        public string ScheduleName => "EntityRegistry";
        public TaskPriority Priority => TaskPriority.Low;
        public int TickInterval => _tickInterval;
        public bool IsActive => true;

        public void ExecuteScheduled()
        {
            RunCleanupCycle();
        }

        #endregion

        #region Constructor

        public EntityRegistry(Logger logger, float cleanupIntervalSeconds = 10f, int tickInterval = 60)
        {
            _logger = logger;
            _cleanupIntervalSeconds = Math.Max(1f, cleanupIntervalSeconds);
            _tickInterval = Math.Max(1, tickInterval);

            _entities = new Dictionary<int, TrackedEntity>();
            _entitiesByType = new Dictionary<RLFEntityType, List<int>>();
            _entitiesByTag = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            _entitiesByOwner = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            _policies = new List<EntityLifetimePolicy>();
            _lock = new object();

            // 🆕 NOVO: Inicializa tracking de hysteresis
            _distanceExceedStart = new Dictionary<int, DateTime>();

            _lastCleanup = DateTime.Now;

            AddPolicy(EntityLifetimePolicy.DefaultVehicle);
            AddPolicy(EntityLifetimePolicy.DefaultPed);
            AddPolicy(EntityLifetimePolicy.DefaultBlip);

            _logger?.Info("[EntityRegistry] Inicializado com hysteresis de distância");
        }

        #endregion

        #region Configuration

        public void SetDeleteHandler(EntityDeleteHandler handler)
        {
            _deleteHandler = handler;
        }

        public void SetExistsHandler(EntityExistsHandler handler)
        {
            _existsHandler = handler;
        }

        public void UpdatePlayerPosition(float x, float y, float z)
        {
            _playerX = x;
            _playerY = y;
            _playerZ = z;
        }

        public void AddPolicy(EntityLifetimePolicy policy)
        {
            if (policy == null)
                return;

            lock (_lock)
            {
                _policies.Add(policy);
                _logger?.Debug($"[EntityRegistry] Política adicionada: {policy.Name}");
            }
        }

        public bool RemovePolicy(string name)
        {
            lock (_lock)
            {
                int removed = _policies.RemoveAll(p => p.Name == name);
                return removed > 0;
            }
        }

        #endregion

        #region Registration

        public bool Register(TrackedEntity entity)
        {
            if (entity == null)
                return false;

            lock (_lock)
            {
                if (_entities.ContainsKey(entity.Handle))
                {
                    _logger?.Warning($"[EntityRegistry] Entidade já registrada: {entity.Handle}");
                    return false;
                }

                _entities[entity.Handle] = entity;

                if (!_entitiesByType.TryGetValue(entity.Type, out var typeList))
                {
                    typeList = new List<int>();
                    _entitiesByType[entity.Type] = typeList;
                }
                typeList.Add(entity.Handle);

                if (!string.IsNullOrEmpty(entity.Tag))
                {
                    if (!_entitiesByTag.TryGetValue(entity.Tag, out var tagList))
                    {
                        tagList = new List<int>();
                        _entitiesByTag[entity.Tag] = tagList;
                    }
                    tagList.Add(entity.Handle);
                }

                if (!_entitiesByOwner.TryGetValue(entity.Owner, out var ownerList))
                {
                    ownerList = new List<int>();
                    _entitiesByOwner[entity.Owner] = ownerList;
                }
                ownerList.Add(entity.Handle);

                _totalCreated++;
            }

            _logger?.Debug($"[EntityRegistry] Registrado: {entity}");
            return true;
        }

        public bool Register(
            int handle,
            RLFEntityType type,
            string tag = null,
            string owner = null,
            float maxLifetime = 0f,
            float maxDistance = 0f,
            bool persistent = false,
            float spawnX = 0f,
            float spawnY = 0f,
            float spawnZ = 0f)
        {
            var entity = new TrackedEntity(
                handle, type, tag, owner,
                maxLifetime, maxDistance, persistent,
                null, spawnX, spawnY, spawnZ
            );

            return Register(entity);
        }

        public bool Unregister(int handle)
        {
            lock (_lock)
            {
                if (!_entities.TryGetValue(handle, out var entity))
                    return false;

                RemoveFromIndexes(entity);
                _entities.Remove(handle);

                // 🆕 NOVO: Remove do tracking de hysteresis
                _distanceExceedStart.Remove(handle);

                _totalRemoved++;

                _logger?.Debug($"[EntityRegistry] Removido: {entity}");
                return true;
            }
        }

        public bool Delete(int handle)
        {
            TrackedEntity entity;

            lock (_lock)
            {
                if (!_entities.TryGetValue(handle, out entity))
                    return false;
            }

            bool deleted = _deleteHandler?.Invoke(handle, entity.Type) ?? false;
            Unregister(handle);

            return deleted;
        }

        #endregion

        #region Queries

        public TrackedEntity Get(int handle)
        {
            lock (_lock)
            {
                _entities.TryGetValue(handle, out var entity);
                return entity;
            }
        }

        public bool IsRegistered(int handle)
        {
            lock (_lock)
            {
                return _entities.ContainsKey(handle);
            }
        }

        public List<TrackedEntity> GetByType(RLFEntityType type)
        {
            var result = new List<TrackedEntity>();

            lock (_lock)
            {
                if (_entitiesByType.TryGetValue(type, out var handles))
                {
                    foreach (var handle in handles)
                    {
                        if (_entities.TryGetValue(handle, out var entity))
                            result.Add(entity);
                    }
                }
            }

            return result;
        }

        public List<TrackedEntity> GetByTag(string tag)
        {
            var result = new List<TrackedEntity>();

            if (string.IsNullOrEmpty(tag))
                return result;

            lock (_lock)
            {
                if (_entitiesByTag.TryGetValue(tag, out var handles))
                {
                    foreach (var handle in handles)
                    {
                        if (_entities.TryGetValue(handle, out var entity))
                            result.Add(entity);
                    }
                }
            }

            return result;
        }

        public List<TrackedEntity> GetByOwner(string owner)
        {
            var result = new List<TrackedEntity>();

            if (string.IsNullOrEmpty(owner))
                return result;

            lock (_lock)
            {
                if (_entitiesByOwner.TryGetValue(owner, out var handles))
                {
                    foreach (var handle in handles)
                    {
                        if (_entities.TryGetValue(handle, out var entity))
                            result.Add(entity);
                    }
                }
            }

            return result;
        }

        public int CountByType(RLFEntityType type)
        {
            lock (_lock)
            {
                return _entitiesByType.TryGetValue(type, out var list) ? list.Count : 0;
            }
        }

        #endregion

        #region Cleanup

        public void RunCleanupCycle()
        {
            if ((DateTime.Now - _lastCleanup).TotalSeconds < _cleanupIntervalSeconds)
                return;

            _lastCleanup = DateTime.Now;

            var toRemove = new List<int>();
            var toRemoveReasons = new Dictionary<int, string>();

            lock (_lock)
            {
                foreach (var kvp in _entities)
                {
                    var entity = kvp.Value;

                    if (entity.IsPersistent)
                        continue;

                    // 🆕 NOVO: Verifica se entidade tem tag persistente em alguma política
                    if (HasPersistentTag(entity))
                        continue;

                    if (_existsHandler != null && !_existsHandler(entity.Handle, entity.Type))
                    {
                        toRemove.Add(entity.Handle);
                        toRemoveReasons[entity.Handle] = "invalid";
                        continue;
                    }

                    if (entity.IsExpiredByTime())
                    {
                        toRemove.Add(entity.Handle);
                        toRemoveReasons[entity.Handle] = "time";
                        continue;
                    }

                    // 🆕 MELHORADO: Verifica distância COM hysteresis
                    if (ShouldRemoveByDistance(entity))
                    {
                        toRemove.Add(entity.Handle);
                        toRemoveReasons[entity.Handle] = "distance";
                        continue;
                    }
                }

                foreach (var policy in _policies)
                {
                    ApplyCountLimit(policy, toRemove, toRemoveReasons);
                }
            }

            foreach (var handle in toRemove)
            {
                TrackedEntity entity;
                lock (_lock)
                {
                    _entities.TryGetValue(handle, out entity);
                }

                if (entity != null)
                {
                    string reason = toRemoveReasons.TryGetValue(handle, out var r) ? r : "unknown";
                    switch (reason)
                    {
                        case "time": _totalCleanedByTime++; break;
                        case "distance": _totalCleanedByDistance++; break;
                        case "limit": _totalCleanedByLimit++; break;
                        case "invalid": _totalCleanedByInvalid++; break;
                    }

                    _deleteHandler?.Invoke(handle, entity.Type);

                    lock (_lock)
                    {
                        RemoveFromIndexes(entity);
                        _entities.Remove(handle);
                        _totalRemoved++;
                    }

                    _logger?.Debug($"[EntityRegistry] Cleanup ({reason}): {entity}");
                }
            }

            if (toRemove.Count > 0)
            {
                _logger?.Info($"[EntityRegistry] Cleanup: {toRemove.Count} entidades removidas");
            }
        }

        /// <summary>
        /// 🆕 NOVO: Verifica se entidade tem tag persistente em alguma política
        /// </summary>
        private bool HasPersistentTag(TrackedEntity entity)
        {
            if (string.IsNullOrEmpty(entity.Tag))
                return false;

            foreach (var policy in _policies)
            {
                if (policy.IsPersistentTag(entity.Tag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 🆕 NOVO: Verifica se deve remover por distância COM hysteresis
        /// </summary>
        private bool ShouldRemoveByDistance(TrackedEntity entity)
        {
            if (entity.MaxDistanceFromPlayer <= 0)
                return false;

            bool isExpired = entity.IsExpiredByDistance(_playerX, _playerY, _playerZ);

            // Se não está expirado, limpa hysteresis e retorna false
            if (!isExpired)
            {
                if (_distanceExceedStart.ContainsKey(entity.Handle))
                {
                    _distanceExceedStart.Remove(entity.Handle);
                }
                return false;
            }

            // Está expirado - verifica hysteresis das políticas
            float minimumDistanceTime = GetMinimumDistanceTime(entity);

            // Se não tem hysteresis configurado (0), remove imediatamente
            if (minimumDistanceTime <= 0)
            {
                return true;
            }

            // Verifica há quanto tempo está expirado
            if (!_distanceExceedStart.TryGetValue(entity.Handle, out var exceedStart))
            {
                // Primeira vez que excede - marca timestamp
                _distanceExceedStart[entity.Handle] = DateTime.Now;
                _totalPreventedByHysteresis++;
                return false;
            }

            // Verifica se passou tempo suficiente
            double secondsExceeded = (DateTime.Now - exceedStart).TotalSeconds;

            if (secondsExceeded >= minimumDistanceTime)
            {
                // Passou tempo suficiente, pode remover
                _distanceExceedStart.Remove(entity.Handle);
                return true;
            }

            // Ainda em hysteresis, não remove
            _totalPreventedByHysteresis++;
            return false;
        }

        /// <summary>
        /// 🆕 NOVO: Obtém o MinimumDistanceTime aplicável para a entidade
        /// </summary>
        private float GetMinimumDistanceTime(TrackedEntity entity)
        {
            foreach (var policy in _policies)
            {
                // Verifica se política se aplica a esta entidade
                bool matches = false;

                if (policy.AffectedTypes != null)
                {
                    foreach (var type in policy.AffectedTypes)
                    {
                        if (entity.Type == type)
                        {
                            matches = true;
                            break;
                        }
                    }
                }

                if (!matches && policy.AffectedTags != null && !string.IsNullOrEmpty(entity.Tag))
                {
                    foreach (var tag in policy.AffectedTags)
                    {
                        if (string.Equals(entity.Tag, tag, StringComparison.OrdinalIgnoreCase))
                        {
                            matches = true;
                            break;
                        }
                    }
                }

                if (matches)
                {
                    return policy.MinimumDistanceTime;
                }
            }

            return 0f; // Sem hysteresis
        }

        private void ApplyCountLimit(
            EntityLifetimePolicy policy,
            List<int> toRemove,
            Dictionary<int, string> reasons)
        {
            if (policy.MaxCount <= 0)
                return;

            var affected = new List<TrackedEntity>();

            foreach (var kvp in _entities)
            {
                var entity = kvp.Value;

                if (entity.IsPersistent)
                    continue;

                // 🆕 NOVO: Respeita persistent tags
                if (HasPersistentTag(entity))
                    continue;

                if (toRemove.Contains(entity.Handle))
                    continue;

                if (policy.AffectedTypes != null)
                {
                    bool typeMatch = false;
                    foreach (var type in policy.AffectedTypes)
                    {
                        if (entity.Type == type)
                        {
                            typeMatch = true;
                            break;
                        }
                    }
                    if (!typeMatch)
                        continue;
                }

                if (policy.AffectedTags != null && policy.AffectedTags.Length > 0)
                {
                    bool tagMatch = false;
                    foreach (var tag in policy.AffectedTags)
                    {
                        if (string.Equals(entity.Tag, tag, StringComparison.OrdinalIgnoreCase))
                        {
                            tagMatch = true;
                            break;
                        }
                    }
                    if (!tagMatch)
                        continue;
                }

                affected.Add(entity);
            }

            if (affected.Count > policy.MaxCount)
            {
                affected.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));

                int toRemoveCount = affected.Count - policy.MaxCount;
                for (int i = 0; i < toRemoveCount; i++)
                {
                    int handle = affected[i].Handle;
                    if (!toRemove.Contains(handle))
                    {
                        toRemove.Add(handle);
                        reasons[handle] = "limit";
                    }
                }
            }
        }

        private void RemoveFromIndexes(TrackedEntity entity)
        {
            if (_entitiesByType.TryGetValue(entity.Type, out var typeList))
            {
                typeList.Remove(entity.Handle);
                if (typeList.Count == 0)
                    _entitiesByType.Remove(entity.Type);
            }

            if (!string.IsNullOrEmpty(entity.Tag) && _entitiesByTag.TryGetValue(entity.Tag, out var tagList))
            {
                tagList.Remove(entity.Handle);
                if (tagList.Count == 0)
                    _entitiesByTag.Remove(entity.Tag);
            }

            if (_entitiesByOwner.TryGetValue(entity.Owner, out var ownerList))
            {
                ownerList.Remove(entity.Handle);
                if (ownerList.Count == 0)
                    _entitiesByOwner.Remove(entity.Owner);
            }
        }

        public int CleanupByOwner(string owner)
        {
            var entities = GetByOwner(owner);
            int count = 0;

            foreach (var entity in entities)
            {
                if (Delete(entity.Handle))
                    count++;
            }

            return count;
        }

        public int CleanupByTag(string tag)
        {
            var entities = GetByTag(tag);
            int count = 0;

            foreach (var entity in entities)
            {
                if (Delete(entity.Handle))
                    count++;
            }

            return count;
        }

        public int CleanupAll(bool includePersistent = false)
        {
            var toDelete = new List<int>();

            lock (_lock)
            {
                foreach (var kvp in _entities)
                {
                    if (includePersistent || !kvp.Value.IsPersistent)
                    {
                        // 🆕 NOVO: Respeita persistent tags mesmo com includePersistent=false
                        if (!includePersistent && HasPersistentTag(kvp.Value))
                            continue;

                        toDelete.Add(kvp.Key);
                    }
                }
            }

            int count = 0;
            foreach (var handle in toDelete)
            {
                if (Delete(handle))
                    count++;
            }

            _logger?.Info($"[EntityRegistry] CleanupAll: {count} entidades removidas");
            return count;
        }

        #endregion

        #region Stats

        public string GetStats()
        {
            lock (_lock)
            {
                return $"[EntityRegistry] " +
                       $"Total={_entities.Count} | " +
                       $"Created={_totalCreated} | " +
                       $"Removed={_totalRemoved} | " +
                       $"Cleanup(Time={_totalCleanedByTime}, Dist={_totalCleanedByDistance}, " +
                       $"Limit={_totalCleanedByLimit}, Invalid={_totalCleanedByInvalid}) | " +
                       $"PreventedByHysteresis={_totalPreventedByHysteresis}";
            }
        }

        public string GetCountsByType()
        {
            lock (_lock)
            {
                var parts = new List<string>();
                foreach (var kvp in _entitiesByType)
                {
                    parts.Add($"{kvp.Key}={kvp.Value.Count}");
                }
                return string.Join(", ", parts);
            }
        }

        #endregion
    }
}