using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RLF.Core.IO
{
    /// <summary>
    /// Tipo de operação de arquivo.
    /// </summary>
    public enum FileOperationType
    {
        WriteText,
        WriteBytes,
        AppendText
    }

    /// <summary>
    /// Operação de arquivo pendente.
    /// </summary>
    internal sealed class PendingFileOperation
    {
        public FileOperationType Type;
        public string FilePath;
        public string TextContent;
        public byte[] ByteContent;
        public Encoding Encoding;
        public Action<FileOperationResult> Callback;
        public DateTime QueuedAt;
    }

    /// <summary>
    /// Fila assíncrona para operações de arquivo.
    /// Processa escritas em background sem bloquear o game loop.
    /// </summary>
    public sealed class AsyncFileQueue : IDisposable
    {
        private readonly Queue<PendingFileOperation> _queue;
        private readonly object _lock;
        private readonly Thread _workerThread;
        private readonly AutoResetEvent _signal;
        private readonly int _maxQueueSize;

        private volatile bool _running;
        private int _totalQueued;
        private int _totalProcessed;
        private int _totalFailed;
        private int _totalDropped;

        public int QueuedCount
        {
            get { lock (_lock) return _queue.Count; }
        }

        public int TotalQueued => _totalQueued;
        public int TotalProcessed => _totalProcessed;
        public int TotalFailed => _totalFailed;
        public int TotalDropped => _totalDropped;
        public bool IsRunning => _running;

        /// <summary>
        /// Cria uma fila de operações de arquivo.
        /// </summary>
        /// <param name="maxQueueSize">Tamanho máximo da fila</param>
        public AsyncFileQueue(int maxQueueSize = 100)
        {
            _maxQueueSize = Math.Max(10, maxQueueSize);
            _queue = new Queue<PendingFileOperation>(_maxQueueSize);
            _lock = new object();
            _signal = new AutoResetEvent(false);
            _running = true;

            _workerThread = new Thread(WorkerLoop)
            {
                Name = "RLF_FileQueue",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _workerThread.Start();
        }

        /// <summary>
        /// Enfileira escrita de texto.
        /// </summary>
        public bool QueueWriteText(string filePath, string content, Encoding encoding = null, Action<FileOperationResult> callback = null)
        {
            return Enqueue(new PendingFileOperation
            {
                Type = FileOperationType.WriteText,
                FilePath = filePath,
                TextContent = content,
                Encoding = encoding ?? Encoding.UTF8,
                Callback = callback,
                QueuedAt = DateTime.Now
            });
        }

        /// <summary>
        /// Enfileira escrita de bytes.
        /// </summary>
        public bool QueueWriteBytes(string filePath, byte[] data, Action<FileOperationResult> callback = null)
        {
            return Enqueue(new PendingFileOperation
            {
                Type = FileOperationType.WriteBytes,
                FilePath = filePath,
                ByteContent = data,
                Callback = callback,
                QueuedAt = DateTime.Now
            });
        }

        /// <summary>
        /// Enfileira append de texto.
        /// </summary>
        public bool QueueAppendText(string filePath, string content, Encoding encoding = null, Action<FileOperationResult> callback = null)
        {
            return Enqueue(new PendingFileOperation
            {
                Type = FileOperationType.AppendText,
                FilePath = filePath,
                TextContent = content,
                Encoding = encoding ?? Encoding.UTF8,
                Callback = callback,
                QueuedAt = DateTime.Now
            });
        }

        private bool Enqueue(PendingFileOperation op)
        {
            lock (_lock)
            {
                if (_queue.Count >= _maxQueueSize)
                {
                    _totalDropped++;
                    return false;
                }

                _queue.Enqueue(op);
                _totalQueued++;
            }

            _signal.Set();
            return true;
        }

        private void WorkerLoop()
        {
            while (_running)
            {
                PendingFileOperation op = null;

                lock (_lock)
                {
                    if (_queue.Count > 0)
                    {
                        op = _queue.Dequeue();
                    }
                }

                if (op != null)
                {
                    ProcessOperation(op);
                }
                else
                {
                    _signal.WaitOne(100);
                }
            }

            // Processa restante ao encerrar
            FlushRemaining();
        }

        private void ProcessOperation(PendingFileOperation op)
        {
            FileOperationResult result;

            try
            {
                switch (op.Type)
                {
                    case FileOperationType.WriteText:
                        result = SafeFileWriter.WriteAllText(op.FilePath, op.TextContent, op.Encoding);
                        break;

                    case FileOperationType.WriteBytes:
                        result = SafeFileWriter.WriteAllBytes(op.FilePath, op.ByteContent);
                        break;

                    case FileOperationType.AppendText:
                        result = SafeFileWriter.AppendText(op.FilePath, op.TextContent, op.Encoding);
                        break;

                    default:
                        result = FileOperationResult.Fail(op.FilePath, FileOperationStatus.Unknown, "Tipo desconhecido");
                        break;
                }
            }
            catch (Exception ex)
            {
                result = FileOperationResult.FromException(op.FilePath, ex);
            }

            if (result.IsSuccess)
                _totalProcessed++;
            else
                _totalFailed++;

            // Callback (cuidado: pode ser chamado de outra thread)
            try
            {
                op.Callback?.Invoke(result);
            }
            catch { }
        }

        private void FlushRemaining()
        {
            while (true)
            {
                PendingFileOperation op;

                lock (_lock)
                {
                    if (_queue.Count == 0)
                        break;

                    op = _queue.Dequeue();
                }

                ProcessOperation(op);
            }
        }

        /// <summary>
        /// Aguarda todas as operações pendentes.
        /// </summary>
        public void WaitForCompletion(int timeoutMs = 5000)
        {
            DateTime start = DateTime.Now;

            while (QueuedCount > 0)
            {
                if ((DateTime.Now - start).TotalMilliseconds > timeoutMs)
                    break;

                Thread.Sleep(10);
            }
        }

        public string GetStats()
        {
            return $"[AsyncFileQueue] " +
                   $"Queued={QueuedCount}/{_maxQueueSize} | " +
                   $"Total={_totalQueued} | " +
                   $"Processed={_totalProcessed} | " +
                   $"Failed={_totalFailed} | " +
                   $"Dropped={_totalDropped}";
        }

        public void Dispose()
        {
            if (!_running)
                return;

            _running = false;
            _signal.Set();

            // Aguarda thread terminar
            if (_workerThread.IsAlive)
            {
                _workerThread.Join(3000);
            }

            _signal.Dispose();
        }
    }
}