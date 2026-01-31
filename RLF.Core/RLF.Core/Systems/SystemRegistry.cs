using RLF.Core.Logging;
using System;
using System.Collections.Generic;

namespace RLF.Core.Systems
{
    /// <summary>
    /// Gerenciador central de sistemas do Real Life Framework.
    /// </summary>
    public sealed class SystemRegistry
    {
        private readonly Logger _logger;
        private readonly Dictionary<string, SystemBase> _systems;

        public int Count => _systems.Count;

        public SystemRegistry(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _systems = new Dictionary<string, SystemBase>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Registra um sistema.
        /// </summary>
        public bool Register(SystemBase system)
        {
            if (system == null)
            {
                _logger.Warning("SystemRegistry: Tentativa de registrar sistema null");
                return false;
            }

            if (_systems.ContainsKey(system.Name))
            {
                _logger.Warning($"SystemRegistry: Sistema '{system.Name}' já está registrado");
                return false;
            }

            _systems.Add(system.Name, system);
            _logger.Info($"SystemRegistry: Sistema '{system.Name}' registrado");
            return true;
        }

        /// <summary>
        /// Remove um sistema.
        /// </summary>
        public bool Unregister(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (!_systems.TryGetValue(name, out var system))
            {
                _logger.Warning($"SystemRegistry: Sistema '{name}' não encontrado");
                return false;
            }

            if (system.IsRunning || system.IsPaused)
            {
                system.Stop();
            }

            _systems.Remove(name);
            _logger.Info($"SystemRegistry: Sistema '{name}' removido");
            return true;
        }

        /// <summary>
        /// Obtém um sistema pelo nome.
        /// </summary>
        public SystemBase Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return _systems.TryGetValue(name, out var system) ? system : null;
        }

        /// <summary>
        /// Obtém um sistema tipado.
        /// </summary>
        public T Get<T>(string name) where T : SystemBase
        {
            return Get(name) as T;
        }

        /// <summary>
        /// Verifica se um sistema está registrado.
        /// </summary>
        public bool Has(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && _systems.ContainsKey(name);
        }

        /// <summary>
        /// Inicia todos os sistemas.
        /// </summary>
        public void StartAll()
        {
            _logger.Info($"SystemRegistry: Iniciando {_systems.Count} sistema(s)...");

            int success = 0;
            int failed = 0;

            foreach (var system in _systems.Values)
            {
                if (system.Start())
                {
                    success++;
                }
                else
                {
                    failed++;
                }
            }

            _logger.Info($"SystemRegistry: {success} iniciado(s), {failed} falha(s)");
        }

        /// <summary>
        /// Para todos os sistemas.
        /// </summary>
        public void StopAll()
        {
            _logger.Info($"SystemRegistry: Parando {_systems.Count} sistema(s)...");

            foreach (var system in _systems.Values)
            {
                system.Stop();
            }

            _logger.Info("SystemRegistry: Todos os sistemas parados");
        }

        /// <summary>
        /// Executa Tick() em todos os sistemas.
        /// </summary>
        public void TickAll()
        {
            foreach (var system in _systems.Values)
            {
                system.Tick();
            }
        }

        /// <summary>
        /// Retorna todos os sistemas registrados.
        /// </summary>
        public IEnumerable<SystemBase> GetAll()
        {
            return _systems.Values;
        }

        /// <summary>
        /// Limpa todos os sistemas.
        /// </summary>
        public void Clear()
        {
            StopAll();
            _systems.Clear();
            _logger.Info("SystemRegistry: Todos os sistemas removidos");
        }
    }
}
