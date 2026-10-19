using System;

namespace SmartX.API.Models
{
    /// <summary>
    /// Represents a live, registered IoT node in the Smart-X mesh network.
    /// Indexed in Dictionary&lt;string, LiveDevice&gt; by MAC Address for O(1) instantaneous lookups.
    /// </summary>
    public class LiveDevice
    {
        public string MacAddress { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Category { get; set; } = "Environmental"; // Environmental, Power, Actuator, Hydroponics
        public string Location { get; set; } = "Sector-7 / Substation-B";
        public double LastReading { get; set; } = 0.0;
        public string Unit { get; set; } = "°C";
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Normal"; // Normal, Warning, Critical, Disconnected
        public string RelayState { get; set; } = "CLOSED"; // OPEN, CLOSED
        public string PumpState { get; set; } = "OFF"; // ON, OFF
        public string ValveState { get; set; } = "NORMAL"; // NORMAL, SHUTDOWN
        public int TelemetryCount { get; set; } = 0;
        public double PowerDrawWatts { get; set; } = 12.5;
        public string FirmwareVersion { get; set; } = "v2.4.1-esp32";

        public LiveDevice()
        {
        }

        public LiveDevice(string macAddress, string nodeId, string deviceName, string category, string location, double initialReading, string unit)
        {
            MacAddress = macAddress;
            NodeId = nodeId;
            DeviceName = deviceName;
            Category = category;
            Location = location;
            LastReading = initialReading;
            Unit = unit;
            LastSeen = DateTime.UtcNow;
            Status = "Normal";
        }

        public void UpdateTelemetry(double value, string visualState)
        {
            LastReading = value;
            LastSeen = DateTime.UtcNow;
            TelemetryCount++;

            if (visualState == "RED_ALERT")
            {
                Status = "Critical";
            }
            else if (value > 65.0)
            {
                Status = "Warning";
            }
            else
            {
                Status = "Normal";
            }
        }
    }
}
