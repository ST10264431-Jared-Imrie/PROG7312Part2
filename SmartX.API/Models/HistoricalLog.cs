using System;

namespace SmartX.API.Models
{
    /// <summary>
    /// Represents a historical sensor reading stored in SortedDictionary&lt;DateTime, HistoricalLog&gt;.
    /// Kept in strictly ascending chronological order for high-performance timeline rendering.
    /// </summary>
    public class HistoricalLog
    {
        public string LogId { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string MacAddress { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Status { get; set; } = "Normal";
        public string ProcessingQueue { get; set; } = "StandardFIFO"; // "StandardFIFO" or "PriorityAlertQueue"

        public HistoricalLog()
        {
        }

        public HistoricalLog(DateTime timestamp, string macAddress, string deviceName, string category, double value, string unit, string status, string queue = "StandardFIFO")
        {
            Timestamp = timestamp;
            MacAddress = macAddress;
            DeviceName = deviceName;
            Category = category;
            Value = value;
            Unit = unit;
            Status = status;
            ProcessingQueue = queue;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {DeviceName} ({MacAddress}): {Value} {Unit} [{Status}] via {ProcessingQueue}";
        }
    }
}
