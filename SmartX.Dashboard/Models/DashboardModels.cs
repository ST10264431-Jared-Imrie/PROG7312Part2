using System;
using System.Collections.Generic;

namespace SmartX.Dashboard.Models
{
    public class LiveDeviceDto
    {
        public string MacAddress { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double LastReading { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; }
        public string Status { get; set; } = "Normal";
        public string RelayState { get; set; } = "CLOSED";
        public string PumpState { get; set; } = "OFF";
        public string ValveState { get; set; } = "NORMAL";
        public int TelemetryCount { get; set; }
        public double PowerDrawWatts { get; set; }
        public string FirmwareVersion { get; set; } = string.Empty;

        // Display Helpers
        public string DisplayValue => $"{LastReading:F1} {Unit}";
        public string StatusColor => Status switch
        {
            "Critical" => "#EF4444",
            "Warning" => "#F59E0B",
            "Disconnected" => "#64748B",
            _ => "#10B981"
        };
        public string RelayColor => RelayState == "CLOSED" ? "#10B981" : "#EF4444";
        public string PumpColor => PumpState == "ON" ? "#3B82F6" : "#64748B";
    }

    public class DeviceCommandDto
    {
        public string CommandId { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string TargetDeviceName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string Parameter { get; set; } = string.Empty;
        public string PreviousState { get; set; } = string.Empty;
        public string ResultState { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string Operator { get; set; } = string.Empty;
        public bool IsUndone { get; set; }

        public string FormattedDescription => $"[{IssuedAt:HH:mm:ss}] {TargetDeviceName}: {ActionType} -> {Parameter} (Prev: {PreviousState})";
    }

    public class PredictiveInsightDto
    {
        public string InsightId { get; set; } = string.Empty;
        public string TargetMac { get; set; } = string.Empty;
        public string TargetDeviceName { get; set; } = string.Empty;
        public string SuggestedAction { get; set; } = string.Empty;
        public string ActionParameter { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public string Severity { get; set; } = "HIGH";
        public string PatternCorrelation { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }

        public string ConfidencePercentage => $"{ConfidenceScore * 100:F0}%";
        public string SeverityColor => Severity switch
        {
            "CRITICAL" => "#EF4444",
            "HIGH" => "#F97316",
            "MEDIUM" => "#F59E0B",
            _ => "#3B82F6"
        };
    }

    public class QueueStatusDto
    {
        public int StandardFifoQueueCount { get; set; }
        public int PriorityAlertQueueCount { get; set; }
        public int CommandUndoStackCount { get; set; }
        public int RegisteredDevicesCount { get; set; }
        public int HistoricalTimelineCount { get; set; }
        public int ActiveErrorCount { get; set; }
        public List<string> ActiveErrorList { get; set; } = new();
        public List<string> RecentCommands { get; set; } = new();
        public long TotalPacketsProcessed { get; set; }
        public long TotalPriorityBypasses { get; set; }
        public DateTime LastProcessedTimestamp { get; set; }
    }

    public class HistoricalLogDto
    {
        public string LogId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string MacAddress { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ProcessingQueue { get; set; } = string.Empty;

        public string QueueBadgeColor => ProcessingQueue.Contains("Priority", StringComparison.OrdinalIgnoreCase) ? "#EF4444" : "#3B82F6";
        public string FormattedLog => $"[{Timestamp:HH:mm:ss.fff}] {DeviceName}: {Value:F1} {Unit} [{ProcessingQueue}]";
    }
}
