using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RLF.Core.Performance
{
    /// <summary>
    /// Coletor de métricas de performance com suporte a média, P95 e P99.
    /// GTA-SAFE: Usa array circular para evitar alocações.
    /// </summary>
    public sealed class PerformanceMetrics
    {
        private readonly string _name;
        private readonly double[] _samples;
        private readonly int _capacity;

        private int _index;
        private int _count;
        private long _totalCalls;

        private double _min;
        private double _max;
        private double _sum;

        // Para cálculo de percentis (ordenação lazy)
        private readonly double[] _sortBuffer;
        private bool _sortDirty;

        public string Name => _name;
        public long TotalCalls => _totalCalls;
        public int SampleCount => _count;

        public double Min => _count > 0 ? _min : 0;
        public double Max => _count > 0 ? _max : 0;
        public double Average => _count > 0 ? _sum / _count : 0;

        /// <summary>
        /// Cria um coletor de métricas.
        /// </summary>
        /// <param name="name">Nome identificador</param>
        /// <param name="sampleCapacity">Quantidade de amostras para percentis (padrão: 100)</param>
        public PerformanceMetrics(string name, int sampleCapacity = 100)
        {
            _name = name ?? "Unknown";
            _capacity = Math.Max(10, sampleCapacity);
            _samples = new double[_capacity];
            _sortBuffer = new double[_capacity];

            Reset();
        }

        /// <summary>
        /// Registra uma amostra de tempo em milissegundos.
        /// </summary>
        public void Record(double milliseconds)
        {
            _totalCalls++;
            _sum += milliseconds;

            if (_count == 0)
            {
                _min = milliseconds;
                _max = milliseconds;
            }
            else
            {
                if (milliseconds < _min) _min = milliseconds;
                if (milliseconds > _max) _max = milliseconds;
            }

            // Array circular
            _samples[_index] = milliseconds;
            _index = (_index + 1) % _capacity;

            if (_count < _capacity)
                _count++;

            _sortDirty = true;
        }

        /// <summary>
        /// Registra tempo usando Stopwatch (mais preciso).
        /// </summary>
        public void Record(Stopwatch sw)
        {
            Record(sw.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// Obtém o percentil especificado (ex: 95 para P95).
        /// </summary>
        public double GetPercentile(int percentile)
        {
            if (_count == 0)
                return 0;

            percentile = Math.Max(0, Math.Min(100, percentile));

            EnsureSorted();

            int index = (int)Math.Ceiling(percentile / 100.0 * _count) - 1;
            index = Math.Max(0, Math.Min(_count - 1, index));

            return _sortBuffer[index];
        }

        public double P50 => GetPercentile(50);
        public double P95 => GetPercentile(95);
        public double P99 => GetPercentile(99);

        /// <summary>
        /// Reseta todas as métricas.
        /// </summary>
        public void Reset()
        {
            _index = 0;
            _count = 0;
            _totalCalls = 0;
            _min = 0;
            _max = 0;
            _sum = 0;
            _sortDirty = true;

            Array.Clear(_samples, 0, _samples.Length);
        }

        private void EnsureSorted()
        {
            if (!_sortDirty)
                return;

            Array.Copy(_samples, _sortBuffer, _count);
            Array.Sort(_sortBuffer, 0, _count);
            _sortDirty = false;
        }

        /// <summary>
        /// Retorna um resumo formatado das métricas.
        /// </summary>
        public string GetSummary()
        {
            if (_count == 0)
                return $"[{_name}] Sem dados";

            return $"[{_name}] Calls={_totalCalls} | " +
                   $"Avg={Average:F3}ms | P95={P95:F3}ms | P99={P99:F3}ms | " +
                   $"Min={Min:F3}ms | Max={Max:F3}ms";
        }
    }
}