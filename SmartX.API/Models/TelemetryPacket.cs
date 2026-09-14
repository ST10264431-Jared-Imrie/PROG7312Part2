using System;

namespace SmartX.API.Models
{
    /// <summary>
    /// Generic telemetry packet designed to eliminate boxing/unboxing overhead
    /// when transmitting scalar value types across mesh network nodes.
    /// </summary>
    /// <typeparam name="T">Payload value type restricted to struct (unmanaged/value types).</typeparam>
    public class TelemetryPacket<T> where T : struct
    {
        public string MacAddress { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public T PayloadValue { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public TelemetryPacket()
        {
        }

        public TelemetryPacket(string macAddress, string location, string category, T payloadValue, DateTime? timestamp = null)
        {
            MacAddress = macAddress;
            Location = location;
            Category = category;
            PayloadValue = payloadValue;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] MAC: {MacAddress} | Loc: {Location} | Cat: {Category} | Val: {PayloadValue}";
        }
    }
}
