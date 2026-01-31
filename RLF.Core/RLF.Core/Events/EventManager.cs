using RLF.Core.Events.EventArgs;
using RLF.Core.Logging;
using System;
using System.Collections.Generic;

namespace RLF.Core.Events
{
    /// <summary>
    /// Gerenciador central de eventos do RLF.
    /// GTA-SAFE: protegido contra abuso, leaks e excesso de handlers.
    /// </summary>
    public sealed class EventManager
    {
        private readonly Dictionary<string, List<EventHandler<RLFEventArgs>>> _eventHandlers;
        private readonly object _lock = new object();

        private bool _isInitialized;
        private int _maxHandlersPerEvent;

        private Logger _logger;

        /// <summary>
        /// Indica se o EventManager está inicializado.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                lock (_lock)
                {
                    return _isInitialized;
                }
            }
        }

        public EventManager()
        {
            _eventHandlers = new Dictionary<string, List<EventHandler<RLFEventArgs>>>(StringComparer.OrdinalIgnoreCase);
            _isInitialized = false;
            _maxHandlersPerEvent = 100; // default seguro
        }

        /// <summary>
        /// Inicializa o EventManager.
        /// </summary>
        public bool Initialize(Logger logger = null, int maxHandlersPerEvent = 100)
        {
            lock (_lock)
            {
                if (_isInitialized)
                    return true;

                _logger = logger;
                _maxHandlersPerEvent = maxHandlersPerEvent > 0 ? maxHandlersPerEvent : 100;

                _eventHandlers.Clear();
                _isInitialized = true;

                _logger?.Info($"EventManager initialized (MaxHandlersPerEvent={_maxHandlersPerEvent})");
                return true;
            }
        }

        /// <summary>
        /// Registra um handler para um evento.
        /// </summary>
        public bool Subscribe(string eventName, EventHandler<RLFEventArgs> handler)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(eventName) || handler == null)
                return false;

            lock (_lock)
            {
                if (!_eventHandlers.TryGetValue(eventName, out var handlers))
                {
                    handlers = new List<EventHandler<RLFEventArgs>>();
                    _eventHandlers[eventName] = handlers;
                }

                // Proteção contra duplicata
                if (handlers.Contains(handler))
                {
                    _logger?.Debug($"EventManager: Handler duplicado ignorado ({eventName})");
                    return false;
                }

                // Proteção contra abuso
                if (handlers.Count >= _maxHandlersPerEvent)
                {
                    _logger?.Warning(
                        $"EventManager: Limite de handlers atingido ({eventName}, Max={_maxHandlersPerEvent})"
                    );
                    return false;
                }

                handlers.Add(handler);
                return true;
            }
        }

        /// <summary>
        /// Remove um handler de um evento.
        /// </summary>
        public bool Unsubscribe(string eventName, EventHandler<RLFEventArgs> handler)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(eventName) || handler == null)
                return false;

            lock (_lock)
            {
                if (!_eventHandlers.TryGetValue(eventName, out var handlers))
                    return false;

                bool removed = handlers.Remove(handler);

                if (handlers.Count == 0)
                    _eventHandlers.Remove(eventName);

                return removed;
            }
        }

        /// <summary>
        /// Dispara um evento de forma segura.
        /// </summary>
        public bool Raise(string eventName, RLFEventArgs args)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(eventName) || args == null)
                return false;

            List<EventHandler<RLFEventArgs>> handlersCopy;

            lock (_lock)
            {
                if (!_eventHandlers.TryGetValue(eventName, out var handlers) || handlers.Count == 0)
                    return false;

                // Cópia defensiva
                handlersCopy = new List<EventHandler<RLFEventArgs>>(handlers);
            }

            bool anyExecuted = false;

            foreach (var handler in handlersCopy)
            {
                try
                {
                    handler?.Invoke(this, args);
                    anyExecuted = true;

                    if (args.IsCancellable && args.IsCancelled)
                        break;
                }
                catch (Exception ex)
                {
                    _logger?.Warning(
                        $"EventManager: Handler falhou ({eventName})",
                        ex
                    );
                }
            }

            return anyExecuted;
        }

        /// <summary>
        /// Remove todos os handlers de um evento.
        /// </summary>
        public bool ClearEvent(string eventName)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(eventName))
                return false;

            lock (_lock)
            {
                return _eventHandlers.Remove(eventName);
            }
        }

        /// <summary>
        /// Remove todos os handlers de todos os eventos.
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                if (!_isInitialized)
                    return;

                _eventHandlers.Clear();
            }
        }

        /// <summary>
        /// Retorna quantidade de handlers de um evento.
        /// </summary>
        public int GetHandlerCount(string eventName)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(eventName))
                return 0;

            lock (_lock)
            {
                return _eventHandlers.TryGetValue(eventName, out var handlers)
                    ? handlers.Count
                    : 0;
            }
        }
    }
}
