using System;

namespace SmartX.API.Models
{
    /// <summary>
    /// Represents a high-priority critical alert packet that bypasses standard FIFO ingestion.
    /// Handled via PriorityQueue&lt;TelemetryAlert, int&gt; where lowest integer denotes highest priority.
    /// Priority 1: Critical Emergency (Power spike, critical moisture drop &lt; 20%)
    /// Priority 2: High Warning
    /// Priority 3: Elevated Nominal
    /// </summary>
    public class TelemetryAlert
    {
        public string AlertId { get; set; } = Guid.NewGuid().ToString("N");
        public string MacAddress { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string AlertType { get; set; } = "CRITICAL_THRESHOLD"; // POWER_SPIKE, MOISTURE_CRITICAL, VOLTAGE_DROP, DISCONNECT
        public int PriorityLevel { get; set; } = 1; // 1 = Highest
        public double TriggerValue { get; set; }
        public double ThresholdLimit { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
        public bool BypassedStandardQueue { get; set; } = true;

        public TelemetryAlert()
        {
        }

        public TelemetryAlert(string macAddress, string deviceName, string alertType, int priority, double triggerVal, double threshold, string unit, string message)
        {
            MacAddress = macAddress;
            DeviceName = deviceName;
            AlertType = alertType;
            PriorityLevel = priority;
            TriggerValue = triggerVal;
            ThresholdLimit = threshold;
            Unit = unit;
            Message = message;
            DetectedAt = DateTime.UtcNow;
            BypassedStandardQueue = true;
        }

        public override string ToString()
        {
            return $"[PRIORITY {PriorityLevel} ALERT] {AlertType} on {DeviceName} ({MacAddress}): {TriggerValue} {Unit} (Limit: {ThresholdLimit}) - {Message}";
        }
    }
}
