using System;
using System.Collections.Generic;
using System.IO;
using RLF.Core.Pooling;
using LogLevel = RLF.Core.Logging.LogLevel;

namespace RLF.Core.Loggin
{
    /// <summary>
    /// Buffer circular de logs em memória.
    /// Acumula logs e faz flush em lote para reduzir I/O.
    /// </summary>
    public sealed class LogBuffer
    {
        private readonly LogEntry[] _buffer;
        private readonly object _lock;
        private readonly int _capacity;
        private readonly ObjectPool<LogEntry> _entryPool;

        private int _head;
        private int _count;
        private int _totalLogs;
        private int _totalFlushes;
        private int _droppedLogs;

        public int Count
        {
            get { lock (_lock) return _count; }
        }

        public int Capacity => _capacity;
        public int TotalLogs => _totalLogs;
        public int TotalFlushes => _totalFlushes;
        public int DroppedLogs => _droppedLogs;
        public bool IsFull => _count >= _capacity;

        /// <summary>
        /// Cria um buffer de logs.
        /// </summary>
        /// <param name="capacity">Capacidade máxima</param>
        public LogBuffer(int capacity = 500)
        {
            _capacity = Math.Max(10, capacity);
            _buffer = new LogEntry[_capacity];
            _lock = new object();
            _head = 0;
            _count = 0;

            _entryPool = new ObjectPool<LogEntry>(
                maxSize: _capacity,
                preWarm: _capacity / 4
            );
        }

        /// <summary>
        /// Adiciona uma entrada ao buffer.
        /// </summary>
        public void Add(LogLevel level, string category, string message, Exception ex = null)
        {
            var entry = _entryPool.Get();
            entry.Set(level, category, message, ex);

            lock (_lock)
            {
                _totalLogs++;

                if (_count >= _capacity)
                {
                    var oldEntry = _buffer[_head];
                    if (oldEntry != null)
                    {
                        _entryPool.Return(oldEntry);
                    }
                    _droppedLogs++;
                }
                else
                {
                    _count++;
                }

                int index = (_head + _count - 1) % _capacity;
                _buffer[index] = entry;
            }
        }

        /// <summary>
        /// Faz flush de todas as entradas para um StreamWriter.
        /// </summary>
        public int FlushTo(StreamWriter writer)
        {
            if (writer == null)
                return 0;

            List<LogEntry> toFlush;

            lock (_lock)
            {
                if (_count == 0)
                    return 0;

                toFlush = new List<LogEntry>(_count);

                for (int i = 0; i < _count; i++)
                {
                    int index = (_head + i) % _capacity;
                    if (_buffer[index] != null)
                    {
                        toFlush.Add(_buffer[index]);
                        _buffer[index] = null;
                    }
                }

                _head = 0;
                _count = 0;
                _totalFlushes++;
            }

            foreach (var entry in toFlush)
            {
                writer.WriteLine(entry.Format());
                _entryPool.Return(entry);
            }

            return toFlush.Count;
        }

        /// <summary>
        /// Faz flush para uma lista de strings.
        /// </summary>
        public List<string> FlushToStrings()
        {
            var result = new List<string>();

            lock (_lock)
            {
                if (_count == 0)
                    return result;

                for (int i = 0; i < _count; i++)
                {
                    int index = (_head + i) % _capacity;
                    if (_buffer[index] != null)
                    {
                        result.Add(_buffer[index].Format());
                        _entryPool.Return(_buffer[index]);
                        _buffer[index] = null;
                    }
                }

                _head = 0;
                _count = 0;
                _totalFlushes++;
            }

            return result;
        }

        /// <summary>
        /// Limpa o buffer sem flush.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                for (int i = 0; i < _count; i++)
                {
                    int index = (_head + i) % _capacity;
                    if (_buffer[index] != null)
                    {
                        _entryPool.Return(_buffer[index]);
                        _buffer[index] = null;
                    }
                }

                _head = 0;
                _count = 0;
            }
        }

        public string GetStats()
        {
            return $"[LogBuffer] " +
                   $"Size={Count}/{_capacity} | " +
                   $"Total={_totalLogs} | " +
                   $"Flushes={_totalFlushes} | " +
                   $"Dropped={_droppedLogs}";
        }
    }
}