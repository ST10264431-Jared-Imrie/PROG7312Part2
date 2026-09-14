using System;
using System.Collections.Generic;

namespace SmartX.API.Models
{
    /// <summary>
    /// Ingestion buffer for batch processing telemetry streams.
    /// Flattens jagged raw sensor matrices into a high-performance memory structure.
    /// </summary>
    public class IngestionBuffer
    {
        private readonly List<double> _buffer;
        private readonly object _lockObject = new();

        public IReadOnlyList<double> Buffer => _buffer.AsReadOnly();
        public int Count => _buffer.Count;

        public IngestionBuffer(int initialCapacity = 1000)
        {
            _buffer = new List<double>(initialCapacity);
        }

        /// <summary>
        /// Iterates through a jagged array of raw telemetry batches and loads all values 
        /// into an optimized continuous memory list.
        /// </summary>
        /// <param name="rawBatches">Jagged 2D array containing raw batch samples.</param>
        public void ProcessRawBatch(double[][] rawBatches)
        {
            if (rawBatches == null || rawBatches.Length == 0)
                return;

            int totalElements = 0;
            for (int i = 0; i < rawBatches.Length; i++)
            {
                if (rawBatches[i] != null)
                {
                    totalElements += rawBatches[i].Length;
                }
            }

            lock (_lockObject)
            {
                int requiredCapacity = _buffer.Count + totalElements;
                if (_buffer.Capacity < requiredCapacity)
                {
                    _buffer.Capacity = Math.Max(_buffer.Capacity * 2, requiredCapacity);
                }

                for (int i = 0; i < rawBatches.Length; i++)
                {
                    double[] batch = rawBatches[i];
                    if (batch == null) continue;

                    for (int j = 0; j < batch.Length; j++)
                    {
                        _buffer.Add(batch[j]);
                    }
                }
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                _buffer.Clear();
            }
        }
    }
}
