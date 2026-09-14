using System;

namespace SmartX.API.Models
{
    public class TelemetryResponse
    {
        public string Status { get; set; } = "SUCCESS";
        public string VisualState { get; set; } = "GREEN_NORMAL";
        public string MacAddress { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
    }
}
